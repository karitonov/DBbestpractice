using Microsoft.Data.Sqlite;
using System.Data.Common;

namespace CSBestpPactice.Infrastructure.Data.Factories;

internal sealed class SqliteConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqliteConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public DbConnection CreateConnection() =>
        new SqliteConnection(_connectionString);
}
