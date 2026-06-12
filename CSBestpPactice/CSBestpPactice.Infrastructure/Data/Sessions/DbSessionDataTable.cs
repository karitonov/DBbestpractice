using System.Data;
using System.Data.Common;

namespace CSBestpPactice.Infrastructure.Data.Sessions;

internal sealed class DbSessionDataTable : IDisposable
{
    private readonly DbConnection _connection;
    private DbTransaction? _transaction;
    private bool _disposed;

    public DbSessionDataTable(DbConnection connection)
    {
        _connection = connection;
    }

    public void Open() => _connection.Open();

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

    public void BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        => _transaction = _connection.BeginTransaction(isolationLevel);

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
