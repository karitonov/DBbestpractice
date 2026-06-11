using System.Data;
using System.Data.Common;

using Microsoft.Data.Sqlite;
// PostgreSQL の場合: using Npgsql;

namespace BestPractice;

static class Examples
{
    static DbConnection CreateConnection()
        => new SqliteConnection("Data Source=sqlite.db");
    // PostgreSQL の場合:
    // => new NpgsqlConnection("Host=localhost;Database=commerce;Username=postgres;Password=pass");


    // ── 複数行 SELECT → DataTable ──────────────────────────────────────────

    public static DataTable GetFeaturedProducts()
    {
        using var session = new DbSessionSync(CreateConnection());
        session.Open();

        return session.QueryDataTable(
            sql: "SELECT Id, Name, UnitPrice FROM Products WHERE IsFeatured = @featured",
            parameters: DbParam.Of(("@featured", true)));
    }


    // ── 単一行 SELECT → DataRow? ───────────────────────────────────────────

    public static DataRow? FindProduct(Guid id)
    {
        using var session = new DbSessionSync(CreateConnection());
        session.Open();

        return session.QueryDataRow(
            sql: "SELECT Id, Name, UnitPrice FROM Products WHERE Id = @id",
            parameters: DbParam.Of(("@id", id)));
    }


    // ── DataTable の各種操作 ───────────────────────────────────────────────

    public static void DataTableOperations()
    {
        using var session = new DbSessionSync(CreateConnection());
        session.Open();

        DataTable table = session.QueryDataTable(
            sql: "SELECT Id, Name, UnitPrice, IsFeatured FROM Products");

        // 行数・列数の確認
        int rowCount = table.Rows.Count;
        int colCount = table.Columns.Count;

        // 列名の一覧（DB の列名がそのまま使える）
        foreach (DataColumn col in table.Columns)
            Console.WriteLine($"{col.ColumnName}: {col.DataType.Name}");

        // 行のイテレーション（列名でアクセス）
        foreach (DataRow row in table.Rows)
        {
            // DataRow の値は object 型なので明示的にキャストする
            var name      = (string)row["Name"];
            var unitPrice = (decimal)row["UnitPrice"];
            Console.WriteLine($"{name}: {unitPrice:C}");
        }

        // DBNull のチェック（NULL を許容する列は IsNull() で確認する）
        foreach (DataRow row in table.Rows)
        {
            var description = row.IsNull("Description")
                ? "(説明なし)"
                : (string)row["Description"];
        }

        // 条件フィルタリング（DataTable.Select）
        DataRow[] featuredRows = table.Select("IsFeatured = true");

        // DataView でソート
        DataView view = table.DefaultView;
        view.Sort = "UnitPrice DESC";
        foreach (DataRowView rowView in view)
            Console.WriteLine(rowView["Name"]);
    }


    // ── DataGrid バインドとロジック処理の両用 ─────────────────────────────
    // 1 回のクエリで DataTable を取得し、DataGrid バインドとロジック処理を両立する

    public static void GetFeaturedProductsForGrid()
    {
        using var session = new DbSessionSync(CreateConnection());
        session.Open();

        DataTable table = session.QueryDataTable(
            sql: "SELECT Id, Name, UnitPrice FROM Products WHERE IsFeatured = @featured",
            parameters: DbParam.Of(("@featured", true)));

        // ① DataGrid へのバインド（DB アクセスなし。DataTable がそのままソースになる）
        DataView gridSource = table.DefaultView;
        // WinForms: dataGridView.DataSource = gridSource;
        // WPF:      dataGrid.ItemsSource   = gridSource;

        // ② 型付きリストに変換してロジック処理（DB アクセスなし。メモリ上の変換のみ）
        IReadOnlyList<ProductRow> products = table.ToList(row => new ProductRow(
            Id:        (Guid)row["Id"],
            Name:      (string)row["Name"],
            UnitPrice: (decimal)row["UnitPrice"]));

        decimal totalValue   = products.Sum(p => p.UnitPrice);
        decimal averagePrice = products.Average(p => p.UnitPrice);
        Console.WriteLine($"合計: {totalValue:C}  平均: {averagePrice:C}  件数: {products.Count}");
    }


    // ── スカラー SELECT（COUNT など）──────────────────────────────────────
    // スカラーは DataTable に関係なく同じ実装

    public static int CountFeaturedProducts()
    {
        using var session = new DbSessionSync(CreateConnection());
        session.Open();

        return session.ExecuteScalar<int>(
            sql: "SELECT COUNT(*) FROM Products WHERE IsFeatured = @featured",
            parameters: DbParam.Of(("@featured", true))) ?? 0;
    }


    // ── INSERT ─────────────────────────────────────────────────────────────

    public static void AddProduct(Guid id, string name, decimal unitPrice)
    {
        using var session = new DbSessionSync(CreateConnection());
        session.Open();

        session.Execute(
            sql: @"INSERT INTO Products (Id, Name, UnitPrice, IsFeatured)
                   VALUES (@id, @name, @price, @featured)",
            parameters: DbParam.Of(
                ("@id",       id),
                ("@name",     name),
                ("@price",    unitPrice),
                ("@featured", false)));
    }


    // ── トランザクション（自動管理） ───────────────────────────────────────
    // 例外が発生した場合は自動で Rollback → 例外を再スロー

    public static void PlaceOrder(Guid orderId, Guid productId, int quantity)
    {
        using var session = new DbSessionSync(CreateConnection());
        session.Open();

        session.ExecuteInTransaction(() =>
        {
            session.Execute(
                sql: "INSERT INTO Orders (Id, ProductId, Quantity) VALUES (@id, @pid, @qty)",
                parameters: DbParam.Of(
                    ("@id",  orderId),
                    ("@pid", productId),
                    ("@qty", quantity)));

            session.Execute(
                sql: "UPDATE Stock SET Quantity = Quantity - @qty WHERE ProductId = @pid",
                parameters: DbParam.Of(
                    ("@qty", quantity),
                    ("@pid", productId)));
        });
    }


    // ── トランザクション（手動制御） ───────────────────────────────────────
    // コミット前に結果を確認するなど、途中で判断が必要な場合に使う

    public static void PlaceOrderManual(Guid orderId, Guid productId, int quantity)
    {
        using var session = new DbSessionSync(CreateConnection());
        session.Open();
        session.BeginTransaction(IsolationLevel.ReadCommitted);
        try
        {
            session.Execute(
                sql: "INSERT INTO Orders (Id, ProductId, Quantity) VALUES (@id, @pid, @qty)",
                parameters: DbParam.Of(
                    ("@id",  orderId),
                    ("@pid", productId),
                    ("@qty", quantity)));

            int remaining = session.ExecuteScalar<int>(
                sql: "SELECT Quantity FROM Stock WHERE ProductId = @pid",
                parameters: DbParam.Of(("@pid", productId))) ?? 0;

            if (remaining < quantity)
                throw new InvalidOperationException("在庫が不足しています。");

            session.Execute(
                sql: "UPDATE Stock SET Quantity = Quantity - @qty WHERE ProductId = @pid",
                parameters: DbParam.Of(
                    ("@qty", quantity),
                    ("@pid", productId)));

            session.Commit();
        }
        catch
        {
            session.Rollback();
            throw;
        }
    }
}
