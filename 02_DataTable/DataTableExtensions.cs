using System.Data;

namespace BestPractice;

// サンプル用の型付き行モデル（実際のアプリでは Domain/ViewModel に置く）
public record ProductRow(Guid Id, string Name, decimal UnitPrice);

public static class DataTableExtensions
{
    public static IReadOnlyList<T> ToList<T>(
        this DataTable table,
        Func<DataRow, T> map)
    {
        var results = new List<T>(table.Rows.Count);
        foreach (DataRow row in table.Rows)
            results.Add(map(row));
        return results;
    }
}
