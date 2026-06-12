using System.Data.Common;

namespace CSBestpPactice.Infrastructure.Data.Factories;

public interface IDbConnectionFactory
{
    DbConnection CreateConnection();
}
