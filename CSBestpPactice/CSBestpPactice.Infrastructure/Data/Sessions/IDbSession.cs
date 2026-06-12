using System.Data;
using System.Data.Common;

namespace CSBestpPactice.Infrastructure.Data.Sessions;

public interface IDbSession : IAsyncDisposable, IDisposable
{
    #region Open

    void Open();
    Task OpenAsync();

    #endregion

    #region Query（標準）

    IReadOnlyList<T> Query<T>(string sql, Func<DbDataReader, T> map, Action<DbCommand>? parameters = null);
    T? QuerySingleOrDefault<T>(string sql, Func<DbDataReader, T> map, Action<DbCommand>? parameters = null);
    Task<IReadOnlyList<T>> QueryAsync<T>(string sql, Func<DbDataReader, T> map, Action<DbCommand>? parameters = null);
    Task<T?> QuerySingleOrDefaultAsync<T>(string sql, Func<DbDataReader, T> map, Action<DbCommand>? parameters = null);

    #endregion

    #region Query（DataTable）

    DataTable QueryDataTable(string sql, Action<DbCommand>? parameters = null);
    DataRow? QueryDataRow(string sql, Action<DbCommand>? parameters = null);
    Task<DataTable> QueryDataTableAsync(string sql, Action<DbCommand>? parameters = null);
    Task<DataRow?> QueryDataRowAsync(string sql, Action<DbCommand>? parameters = null);

    #endregion

    #region ExecuteScalar

    T? ExecuteScalar<T>(string sql, Action<DbCommand>? parameters = null);
    Task<T?> ExecuteScalarAsync<T>(string sql, Action<DbCommand>? parameters = null);

    #endregion

    #region Execute

    int Execute(string sql, Action<DbCommand>? parameters = null);
    Task<int> ExecuteAsync(string sql, Action<DbCommand>? parameters = null);

    #endregion

    #region Transaction

    void BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted);
    void Commit();
    void Rollback();
    void ExecuteInTransaction(Action work, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted);

    Task BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted);
    Task CommitAsync();
    Task RollbackAsync();
    Task ExecuteInTransactionAsync(Func<Task> work, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted);

    #endregion
}
