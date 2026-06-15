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
        if (table.Rows.Count == 0) return table;

        // Id: BLOB → GUID 文字列（値ベースで判定。列型は常に object）
        if (table.Rows[0]["Id"] is byte[])
        {
            var idStr = new DataColumn("IdStr", typeof(string));
            table.Columns.Add(idStr);
            foreach (DataRow row in table.Rows)
                row["IdStr"] = row["Id"] is byte[] b ? new Guid(b).ToString() : row["Id"];

            table.PrimaryKey = [];
            table.Columns.Remove("Id");
            idStr.ColumnName = "Id";
            idStr.SetOrdinal(0);
        }

        // IsFeatured: INTEGER(0/1) → bool（値ベースで判定）
        if (table.Rows[0]["IsFeatured"] is not bool)
        {
            int ordinal = table.Columns["IsFeatured"]!.Ordinal;
            var boolCol = new DataColumn("IsFeaturedBool", typeof(bool)) { DefaultValue = false };
            table.Columns.Add(boolCol);
            foreach (DataRow row in table.Rows)
                row["IsFeaturedBool"] = Convert.ToBoolean(row["IsFeatured"]);

            table.Columns.Remove("IsFeatured");
            boolCol.ColumnName = "IsFeatured";
            boolCol.SetOrdinal(ordinal);
        }

        table.AcceptChanges();
        return table;
    }

    #region 同期UPDATE
    public int Update(DataTable table)
    {
        var changed = table.GetChanges();
        if (changed is null) return 0;

        int count = 0;
        _session.ExecuteInTransaction(() =>
        {
            foreach (DataRow row in changed.Rows)
            {
                count += row.RowState switch
                {
                    DataRowState.Added => InsertRow(row),
                    DataRowState.Modified => UpdateRow(row),
                    DataRowState.Deleted => DeleteRow(row),
                    _ => 0,
                };
            }
        });

        table.AcceptChanges();
        return count;
    }

    private int InsertRow(DataRow row)
    {
        var id = row["Id"] is DBNull or "" ? Guid.NewGuid().ToString() : row["Id"];
        var sql =
            """
            INSERT INTO Products (Id, Name, Description, UnitPrice, IsFeatured)
            VALUES (@Id, @Name, @Description, @UnitPrice, @IsFeatured)
            """;

        return _session.Execute(sql, DbParam.Of(
            ("@Id",          id),
            ("@Name",        row["Name"]),
            ("@Description", row["Description"]),
            ("@UnitPrice",   row["UnitPrice"]),
            ("@IsFeatured",  row["IsFeatured"])
        ));
    }

    private int UpdateRow(DataRow row)
    {
        var sql = 
            """
            UPDATE Products
            SET Name = @Name, Description = @Description,
                UnitPrice = @UnitPrice, IsFeatured = @IsFeatured
            WHERE Id = @Id
            """;
        return _session.Execute(sql, DbParam.Of(
            ("@Id", row["Id", DataRowVersion.Original]),
            ("@Name", row["Name"]),
            ("@Description", row["Description"]),
            ("@UnitPrice", row["UnitPrice"]),
            ("@IsFeatured", row["IsFeatured"])
        ));
    }

    private int DeleteRow(DataRow row)
    {
        var sql = "DELETE FROM Products WHERE Id = @Id";
        return _session.Execute(sql, DbParam.Of(
            ("@Id", row["Id", DataRowVersion.Original])
        ));
    }
    #endregion

    #region 非同期UPDATE
    public async Task<int> UpdateAsync(DataTable table)
    {
        var changed = table.GetChanges();
        if (changed is null) return 0;

        int count = 0;
        await _session.ExecuteInTransactionAsync(async () =>
        {
            foreach (DataRow row in changed.Rows)
            {
                count += row.RowState switch
                {
                    DataRowState.Added => await InsertRowAsync(row),
                    DataRowState.Modified => await UpdateRowAsync(row),
                    DataRowState.Deleted => await DeleteRowAsync(row),
                    _ => 0,
                };
            }
        });

        table.AcceptChanges();
        return count;
    }

    private async Task<int> InsertRowAsync(DataRow row)
    {
        var id = row["Id"] is DBNull or "" ? Guid.NewGuid().ToString() : row["Id"];
        var sql =
            """
            INSERT INTO Products (Id, Name, Description, UnitPrice, IsFeatured)
            VALUES (@Id, @Name, @Description, @UnitPrice, @IsFeatured)
            """;
        return await _session.ExecuteAsync(sql, DbParam.Of(
            ("@Id",          id),
            ("@Name",        row["Name"]),
            ("@Description", row["Description"]),
            ("@UnitPrice",   row["UnitPrice"]),
            ("@IsFeatured",  row["IsFeatured"])
        ));
    }

    private async Task<int> UpdateRowAsync(DataRow row)
    {
        var sql = 
            """
            UPDATE Products
            SET Name = @Name, Description = @Description,
                UnitPrice = @UnitPrice, IsFeatured = @IsFeatured
            WHERE Id = @Id
            """;
        return await _session.ExecuteAsync(sql, DbParam.Of(
            ("@Id", row["Id", DataRowVersion.Original]),
            ("@Name", row["Name"]),
            ("@Description", row["Description"]),
            ("@UnitPrice", row["UnitPrice"]),
            ("@IsFeatured", row["IsFeatured"])
        ));
    }

    private async Task<int> DeleteRowAsync(DataRow row)
    {
        var sql = "DELETE FROM Products WHERE Id = @Id";
        return await _session.ExecuteAsync(sql, DbParam.Of(
            ("@Id", row["Id", DataRowVersion.Original])
        ));
    }
    #endregion
}
