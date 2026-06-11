using System.Data;
using System.Data.Common;

namespace BestPractice;

/// <summary>
/// 同期版 DB セッション（DataTable 戻り値版）。
/// SELECT 結果を型付きオブジェクトではなく DataTable / DataRow で受け取る。
/// </summary>
public sealed class DbSessionSync : IDisposable
{
    private readonly DbConnection connection;
    private DbTransaction? transaction;
    private bool disposed;

    public DbSessionSync(DbConnection connection)
    {
        this.connection = connection;
    }

    public void Open() => connection.Open();

    // ── クエリ系 ────────────────────────────────────────────────────────────

    public DataTable QueryDataTable(
        string sql,
        Action<DbCommand>? parameters = null)
    {
        using DbCommand cmd = BuildCommand(sql, parameters);
        using DbDataReader reader = cmd.ExecuteReader();

        var table = new DataTable();
        table.Load(reader);
        return table;
    }

    public DataRow? QueryDataRow(
        string sql,
        Action<DbCommand>? parameters = null)
    {
        var table = QueryDataTable(sql, parameters);
        return table.Rows.Count > 0 ? table.Rows[0] : null;
    }

    // COUNT(*) など単一値を返す SELECT 向け。
    // ExecuteScalar の戻り値は object? かつ DBNull の可能性があるため変換処理が必要。
    public T? ExecuteScalar<T>(
        string sql,
        Action<DbCommand>? parameters = null)
    {
        using DbCommand cmd = BuildCommand(sql, parameters);
        object? result = cmd.ExecuteScalar();
        return result is null or DBNull ? default : (T)Convert.ChangeType(result, typeof(T));
    }

    // ── 更新系 ────────────────────────────────────────────────────────────

    // INSERT / UPDATE / DELETE → 影響行数を返す
    public int Execute(
        string sql,
        Action<DbCommand>? parameters = null)
    {
        using DbCommand cmd = BuildCommand(sql, parameters);
        return cmd.ExecuteNonQuery();
    }

    // ── トランザクション ──────────────────────────────────────────────────

    public void BeginTransaction(
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
    {
        transaction = connection.BeginTransaction(isolationLevel);
    }

    public void Commit()
    {
        EnsureTransaction();
        transaction!.Commit();
    }

    public void Rollback()
    {
        EnsureTransaction();
        transaction!.Rollback();
    }

    // try/catch/rollback の定型を隠蔽したヘルパー。
    // 例外が発生した場合は自動で Rollback し、例外を再スローする。
    public void ExecuteInTransaction(
        Action work,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
    {
        BeginTransaction(isolationLevel);
        try
        {
            work();
            Commit();
        }
        catch
        {
            Rollback();
            throw;
        }
    }

    // ── 内部ヘルパー ───────────────────────────────────────────────────────

    private DbCommand BuildCommand(string sql, Action<DbCommand>? parameters)
    {
        DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = transaction;
        parameters?.Invoke(cmd);
        return cmd;
    }

    private void EnsureTransaction()
    {
        if (transaction is null)
            throw new InvalidOperationException("Transaction has not been started. Call BeginTransaction first.");
    }

    // ── Dispose ───────────────────────────────────────────────────────────

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        transaction?.Dispose();
        connection.Dispose();
    }
}
