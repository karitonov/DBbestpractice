using System.Data;
using System.Data.Common;

namespace CSBestpPactice.Infrastructure.Data.Sessions;

public sealed class DbSession : IDbSession
{
    #region Fields & Constructor

    private readonly DbConnection _connection;
    private DbTransaction? _transaction;
    private bool _disposed;

    public DbSession(DbConnection connection)
    {
        _connection = connection;
    }

    #endregion

    #region Open

    public void Open() => _connection.Open();
    public Task OpenAsync() => _connection.OpenAsync();

    #endregion

    #region Query（標準）

    public IReadOnlyList<T> Query<T>(
        string sql,
        Func<DbDataReader, T> map,
        Action<DbCommand>? parameters = null)
    {
        using DbCommand cmd = BuildCommand(sql, parameters);
        using DbDataReader reader = cmd.ExecuteReader();

        var results = new List<T>();
        while (reader.Read())
            results.Add(map(reader));

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

    #endregion

    #region Query（DataTable）

    public DataTable QueryDataTable(
        string sql,
        Action<DbCommand>? parameters = null)
    {
        using DbCommand cmd = BuildCommand(sql, parameters);
        using DbDataReader reader = cmd.ExecuteReader();
        return BuildDataTable(reader);
    }

    public DataRow? QueryDataRow(
        string sql,
        Action<DbCommand>? parameters = null)
    {
        var table = QueryDataTable(sql, parameters);
        return table.Rows.Count > 0 ? table.Rows[0] : null;
    }

    public async Task<DataTable> QueryDataTableAsync(
        string sql,
        Action<DbCommand>? parameters = null)
    {
        using DbCommand cmd = BuildCommand(sql, parameters);
        await using DbDataReader reader = await cmd.ExecuteReaderAsync();
        return BuildDataTable(reader);
    }

    public async Task<DataRow?> QueryDataRowAsync(
        string sql,
        Action<DbCommand>? parameters = null)
    {
        var table = await QueryDataTableAsync(sql, parameters);
        return table.Rows.Count > 0 ? table.Rows[0] : null;
    }

    #endregion

    #region ExecuteScalar

    public T? ExecuteScalar<T>(
        string sql,
        Action<DbCommand>? parameters = null)
    {
        using DbCommand cmd = BuildCommand(sql, parameters);
        object? result = cmd.ExecuteScalar();
        return result is null or DBNull ? default : (T)Convert.ChangeType(result, typeof(T));
    }

    public async Task<T?> ExecuteScalarAsync<T>(
        string sql,
        Action<DbCommand>? parameters = null)
    {
        using DbCommand cmd = BuildCommand(sql, parameters);
        object? result = await cmd.ExecuteScalarAsync();
        return result is null or DBNull ? default : (T)Convert.ChangeType(result, typeof(T));
    }

    #endregion

    #region Execute

    public int Execute(
        string sql,
        Action<DbCommand>? parameters = null)
    {
        using DbCommand cmd = BuildCommand(sql, parameters);
        return cmd.ExecuteNonQuery();
    }

    public async Task<int> ExecuteAsync(
        string sql,
        Action<DbCommand>? parameters = null)
    {
        using DbCommand cmd = BuildCommand(sql, parameters);
        return await cmd.ExecuteNonQueryAsync();
    }

    #endregion

    #region Transaction

    public void BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
    {
        if (_connection.State != System.Data.ConnectionState.Open)
            _connection.Open();
        _transaction = _connection.BeginTransaction(isolationLevel);
    }

    public async Task BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
    {
        if (_connection.State != System.Data.ConnectionState.Open)
            await _connection.OpenAsync();
        _transaction = await _connection.BeginTransactionAsync(isolationLevel);
    }

    public void Commit()
    {
        EnsureTransaction();
        _transaction!.Commit();
        _transaction.Dispose();
        _transaction = null;
    }

    public async Task CommitAsync()
    {
        EnsureTransaction();
        await _transaction!.CommitAsync();
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public void Rollback()
    {
        EnsureTransaction();
        _transaction!.Rollback();
        _transaction.Dispose();
        _transaction = null;
    }

    public async Task RollbackAsync()
    {
        EnsureTransaction();
        await _transaction!.RollbackAsync();
        await _transaction.DisposeAsync();
        _transaction = null;
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

    #endregion

    #region Private

    // すべての列を object 型で定義することで、SQLite の動的型付けによる
    // BLOB/TEXT 混在でも DataTable への格納が失敗しないようにする
    private static DataTable BuildDataTable(DbDataReader reader)
    {
        var table = new DataTable();
        for (int i = 0; i < reader.FieldCount; i++)
            table.Columns.Add(reader.GetName(i), typeof(object));

        while (reader.Read())
        {
            var row = table.NewRow();
            for (int i = 0; i < reader.FieldCount; i++)
                row[i] = reader.IsDBNull(i) ? DBNull.Value : reader.GetValue(i);
            table.Rows.Add(row);
        }
        return table;
    }

    private DbCommand BuildCommand(string sql, Action<DbCommand>? parameters)
    {
        if (_connection.State != System.Data.ConnectionState.Open)
            _connection.Open();

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
            throw new InvalidOperationException("BeginTransaction(Async) を先に呼んでください。");
        }
    }

    #endregion

    #region Dispose

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

    #endregion
}
