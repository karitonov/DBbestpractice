using System.Data;
using System.Data.Common;

namespace CSBestpPactice.Infrastructure.Data.Sessions;

internal sealed class DbSessionAsync : IAsyncDisposable, IDisposable
{
    private readonly DbConnection _connection;
    private DbTransaction? _transaction;
    private bool _disposed;

    public DbSessionAsync(DbConnection connection)
    {
        this._connection = connection;
    }

    public Task OpenAsync() => _connection.OpenAsync();

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

    public async Task<T?> ExecuteScalarAsync<T>(
        string sql,
        Action<DbCommand>? parameters = null)
    {
        using DbCommand cmd = BuildCommand(sql, parameters);
        object? result = await cmd.ExecuteScalarAsync();
        return result is null or DBNull ? default : (T)Convert.ChangeType(result, typeof(T));
    }

    public async Task<int> ExecuteAsync(
        string sql,
        Action<DbCommand>? parameters = null)
    {
        using DbCommand cmd = BuildCommand(sql, parameters);
        return await cmd.ExecuteNonQueryAsync();
    }

    public async Task BeginTransactionAsync(
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
    {
        _transaction = await _connection.BeginTransactionAsync(isolationLevel);
    }

    public Task CommitAsync()
    {
        EnsureTransaction();
        return _transaction!.CommitAsync();
    }

    public Task RollbackAsync()
    {
        EnsureTransaction();
        return _transaction!.RollbackAsync();
    }

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

    private DbCommand BuildCommand(string sql, Action<DbCommand>? parameters)
    {
        DbCommand cmd = _connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = _transaction;
        parameters?.Invoke(cmd);
        return cmd;
    }

    private void EnsureTransaction()
    {
        if (_transaction is null)
        {
            throw new InvalidOperationException("BeginTransactionAsync を先に呼んでください。");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        if (_transaction is not null) await _transaction.DisposeAsync();
        await _connection.DisposeAsync();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _transaction?.Dispose();
        _connection.Dispose();
    }
}
