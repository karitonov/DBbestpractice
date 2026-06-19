using Dapper;
using System.Data;

namespace CSBestpPactice.Infrastructure.Repositories.Dapper;

public sealed class GuidTypeHandler : SqlMapper.TypeHandler<Guid>
{
    public override Guid Parse(object value)
        => value is byte[] bytes ? new Guid(bytes) : Guid.Parse(value.ToString()!);

    public override void SetValue(IDbDataParameter parameter, Guid value)
        => parameter.Value = value.ToString();
}

public sealed class DecimalTypeHandler : SqlMapper.TypeHandler<decimal>
{
    public override decimal Parse(object value)
        => Convert.ToDecimal(value);

    public override void SetValue(IDbDataParameter parameter, decimal value)
        => parameter.Value = value;
}
