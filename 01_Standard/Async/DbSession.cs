using System.Data;
using System.Data.Common;

namespace BestPractice;

/// <summary>
/// 非同期版 DB セッション。接続とトランザクションを1つの Unit of Work として管理する。
/// </summary>
/// <remarks>
/// IDbConnection ではなく DbConnection を使う理由:
/// IDbConnection には OpenAsync / BeginTransactionAsync などの非同期メソッドが定義されていない。
/// DbConnection（抽象基底クラス）を使うことで、SQLite・PostgreSQL どちらのプロバイダーでも
/// 同一コードで非同期 DB アクセスが可能になる。
/// </remarks>
public sealed class DbSessionAsync : IAsyncDisposable, IDisposable
{
    private readonly DbConnection connection;
    private DbTransaction? transaction;
    private bool disposed;

    public DbSessionAsync(DbConnection connection)
    {
        this.connection = connection;
    }

    public Task OpenAsync() => connection.OpenAsync();

    // ── クエリ系 ────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<T>> QueryAsync<T>(
        string sql,
        Func<DbDataReader, T> map,
        Action<DbCommand>? parameters = null)
    {
        using DbCommand cmd = BuildCommand(sql, parameters);
        await using DbDataReader reader = await cmd.ExecuteReaderAsync();

        var results = new List<T>();
        while (await reader.ReadAsync())
            results.Add(map(reader));

        return results;
    }

    public async Task<T?> QuerySingleOrDefaultAsync<T>(
        string sql,
        Func<DbDataReader, T> map,
        Action<DbCommand>? parameters = null)
    {
        using DbCommand cmd = BuildCommand(sql, parameters);
        await using DbDataReader reader = await cmd.ExecuteReaderAsync();

        return await reader.ReadAsync() ? map(reader) : default;
    }

    // COUNT(*) など単一値を返す SELECT 向け。
    // ExecuteScalar の戻り値は object? かつ DBNull の可能性があるため変換処理が必要。
    public async Task<T?> ExecuteScalarAsync<T>(
        string sql,
        Action<DbCommand>? parameters = null)
    {
        using DbCommand cmd = BuildCommand(sql, parameters);
        object? result = await cmd.ExecuteScalarAsync();
        return result is null or DBNull ? default : (T)Convert.ChangeType(result, typeof(T));
    }

    // ── 更新系 ────────────────────────────────────────────────────────────

    // INSERT / UPDATE / DELETE → 影響行数を返す
    public async Task<int> ExecuteAsync(
        string sql,
        Action<DbCommand>? parameters = null)
    {
        using DbCommand cmd = BuildCommand(sql, parameters);
        return await cmd.ExecuteNonQueryAsync();
    }

    // ── トランザクション ──────────────────────────────────────────────────

    public async Task BeginTransactionAsync(
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
    {
        transaction = await connection.BeginTransactionAsync(isolationLevel);
    }

    public Task CommitAsync()
    {
        EnsureTransaction();
        return transaction!.CommitAsync();
    }

    public Task RollbackAsync()
    {
        EnsureTransaction();
        return transaction!.RollbackAsync();
    }

    // try/catch/rollback の定型を隠蔽したヘルパー。
    // 例外が発生した場合は自動で Rollback し、例外を再スローする。
    public async Task ExecuteInTransactionAsync(
        Func<Task> work,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
    {
        await BeginTransactionAsync(isolationLevel);
        try
        {
            await work();
            await CommitAsync();
        }
        catch
        {
            await RollbackAsync();
            throw;
        }
    }

    // ── 内部ヘルパー ───────────────────────────────────────────────────────

    private DbCommand BuildCommand(string sql, Action<DbCommand>? parameters)
    {
        DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = transaction; // null のときはトランザクションなし
        parameters?.Invoke(cmd);
        return cmd;
    }

    private void EnsureTransaction()
    {
        if (transaction is null)
            throw new InvalidOperationException("Transaction has not been started. Call BeginTransactionAsync first.");
    }

    // ── Dispose ───────────────────────────────────────────────────────────

    // IAsyncDisposable と IDisposable の両方を実装する。
    // await using で呼ばれた場合は DisposeAsync、using で呼ばれた場合は Dispose が使われる。
    public async ValueTask DisposeAsync()
    {
        if (disposed) return;
        disposed = true;
        if (transaction is not null) await transaction.DisposeAsync();
        await connection.DisposeAsync();
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        transaction?.Dispose();
        connection.Dispose();
    }
}
