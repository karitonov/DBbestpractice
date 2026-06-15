using CSBestpPactice.Infrastructure.Data;
using CSBestpPactice.Infrastructure.Data.Sessions;
using System.Data;

namespace CSBestpPactice.Infrastructure.Repositories.DataTables;

public sealed class ProductTableRepository : IProductTableRepository
{
    private readonly IDbSession _session;

    public ProductTableRepository(IDbSession session)
    {
        _session = session;
    }

    public DataTable GetAll()
    {
        var sql = "SELECT Id, Name, Description, UnitPrice, IsFeatured FROM Products";
        return Normalize(_session.QueryDataTable(sql));
    }

    public async Task<DataTable> GetAllAsync()
    {
        var sql = "SELECT Id, Name, Description, UnitPrice, IsFeatured FROM Products";
        return Normalize(await _session.QueryDataTableAsync(sql));
    }

    public DataTable GetById(Guid id)
    {
        var sql = "SELECT Id, Name, Description, UnitPrice, IsFeatured FROM Products WHERE Id = @Id";
        return Normalize(_session.QueryDataTable(sql, DbParam.Of(("@Id", id.ToString()))));
    }

    public async Task<DataTable> GetByIdAsync(Guid id)
    {
        var sql = "SELECT Id, Name, Description, UnitPrice, IsFeatured FROM Products WHERE Id = @Id";
        return Normalize(await _session.QueryDataTableAsync(sql, DbParam.Of(("@Id", id.ToString()))));
    }

    public DataTable GetFeaturedProducts()
    {
        var sql = "SELECT Id, Name, Description, UnitPrice, IsFeatured FROM Products WHERE IsFeatured = 1";
        return Normalize(_session.QueryDataTable(sql));
    }

    public async Task<DataTable> GetFeaturedProductsAsync()
    {
        var sql = "SELECT Id, Name, Description, UnitPrice, IsFeatured FROM Products WHERE IsFeatured = 1";
        return Normalize(await _session.QueryDataTableAsync(sql));
    }

    private static DataTable Normalize(DataTable table)
    {
        // Id: BLOB → GUID 文字列
        if (table.Columns.Contains("Id") && table.Columns["Id"]!.DataType == typeof(byte[]))
        {
            var idStr = new DataColumn("IdStr", typeof(string));
            table.Columns.Add(idStr);
            foreach (DataRow row in table.Rows)
                row["IdStr"] = new Guid((byte[])row["Id"]).ToString();

            table.PrimaryKey = [];
            table.Columns.Remove("Id");
            idStr.ColumnName = "Id";
            idStr.SetOrdinal(0);
        }

        // IsFeatured: INTEGER(0/1) → bool
        if (table.Columns.Contains("IsFeatured") && table.Columns["IsFeatured"]!.DataType != typeof(bool))
        {
            int ordinal = table.Columns["IsFeatured"]!.Ordinal;
            var boolCol = new DataColumn("IsFeaturedBool", typeof(bool));
            table.Columns.Add(boolCol);
            foreach (DataRow row in table.Rows)
                row["IsFeaturedBool"] = Convert.ToBoolean(row["IsFeatured"]);

            table.Columns.Remove("IsFeatured");
            boolCol.ColumnName = "IsFeatured";
            boolCol.SetOrdinal(ordinal);
        }

        return table;
    }
}
