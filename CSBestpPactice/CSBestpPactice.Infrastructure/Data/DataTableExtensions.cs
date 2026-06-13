using System.Data;

namespace CSBestpPactice.Infrastructure.Data;

internal static class DataTableExtensions
{
    public static IReadOnlyList<T> ToList<T>(this DataTable dataTable, Func<DataRow, T> map)
    {
        var list = new List<T>(dataTable.Rows.Count);
        foreach (DataRow row in dataTable.Rows)
        {
            list.Add(map(row));
        }
        return list;
    }

    public static T? FirstOrDefault<T>(this DataTable dataTable, Func<DataRow, T> map)
    {
        return dataTable.Rows.Count > 0 ? map(dataTable.Rows[0]) : default;
    }
}
