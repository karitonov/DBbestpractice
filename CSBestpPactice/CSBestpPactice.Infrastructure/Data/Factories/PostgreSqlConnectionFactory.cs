using Npgsql;
using System.Data.Common;

namespace CSBestpPactice.Infrastructure.Data.Factories;

internal sealed class PostgreSqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public PostgreSqlConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public DbConnection CreateConnection() =>
        new NpgsqlConnection(_connectionString);
}
