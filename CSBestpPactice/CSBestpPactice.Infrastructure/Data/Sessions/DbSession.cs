using System.Data;
using System.Data.Common;

namespace CSBestpPactice.Infrastructure.Data.Sessions;

internal sealed class DbSession : IDisposable
{
    private readonly DbConnection _connection;
    private DbTransaction? _transaction;
    private bool _disposed;

    public DbSession(DbConnection connection)
    {
        _connection = connection;
    }

    public void Open() => _connection.Open();

    public IReadOnlyList<T> Query<T>(
        string sql,
        Func<DbDataReader, T> map,
        Action<DbCommand>? parameters = null)
    {
        using DbCommand cmd = BuildCommand(sql, parameters);
        using DbDataReader reader = cmd.ExecuteReader();

        var results = new List<T>();
        while (reader.Read())
        {
            results.Add(map(reader));
        }

        return results;
    }

    public T? QuerySingleOrDefault<T>(
        string sql,
        Func<DbDataReader, T> map,
        Action<DbCommand>? parameters = null)
    {
        using DbCommand cmd = BuildCommand(sql, parameters);
        using DbDataReader reader = cmd.ExecuteReader();

        return reader.Read() ? map(reader) : default;
    }

    public T? ExecuteScalar<T>(
        string sql,
        Action<DbCommand>? parameters = null)
    {
        using DbCommand cmd = BuildCommand(sql, parameters);
        object? result = cmd.ExecuteScalar();

        return result is null or DBNull ? default : (T)Convert.ChangeType(result, typeof(T));
    }

    public int Execute(
        string sql,
        Action<DbCommand>? parameters = null)
    {
        using DbCommand cmd = BuildCommand(sql, parameters);
        return cmd.ExecuteNonQuery();
    }

    public void BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted) =>
        _transaction = _connection.BeginTransaction(isolationLevel);

    public void Commit()
    {
        EnsureTransaction();
        _transaction!.Commit();
    }

    public void Rollback()
    {
        EnsureTransaction();
        _transaction!.Rollback();
    }

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
            throw new InvalidOperationException("BeginTransaction を先に呼んでください。");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _transaction?.Dispose();
        _connection.Dispose();
    }
}
