using System.Data.Common;

namespace CSBestpPactice.Infrastructure.Data;

internal static class DbParam
{
    public static Action<DbCommand> Of(params (string Name, object? Value)[] parameters)
    {
        return cmd =>
        {
            foreach (var (name, value) in parameters)
            {
                var param = cmd.CreateParameter();
                param.ParameterName = name;
                param.Value = value ?? DBNull.Value;
                cmd.Parameters.Add(param);
            }
        };
    }
}
