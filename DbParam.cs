using System.Data.Common;

namespace BestPractice;

/// <summary>
/// 非同期・同期の両 DbSession で共用するパラメーター生成ヘルパー。
/// </summary>
public static class DbParam
{
    // 文字列結合による SQL 組み立てを避けるため、パラメーターは必ずこれ経由で渡す。
    // value に null を渡すと DBNull.Value に変換する（DB の NULL 表現と対応）。
    public static Action<DbCommand> Of(params (string Name, object? Value)[] parameters)
        => cmd =>
        {
            foreach (var (name, value) in parameters)
            {
                DbParameter p = cmd.CreateParameter();
                p.ParameterName = name;
                p.Value = value ?? DBNull.Value;
                cmd.Parameters.Add(p);
            }
        };
}
