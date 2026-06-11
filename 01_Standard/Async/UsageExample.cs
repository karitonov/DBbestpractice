using System.Data;
using System.Data.Common;
using BestPractice;
using Microsoft.Data.Sqlite;
// PostgreSQL の場合: using Npgsql;

namespace BestPractice;

// record Product(Guid Id, string Name, decimal UnitPrice);

static class Examples
{
    static DbConnection CreateConnection()
        => new SqliteConnection("Data Source=sqlite.db");
    // PostgreSQL の場合:
    // => new NpgsqlConnection("Host=localhost;Database=commerce;Username=postgres;Password=pass");


    // ── 複数行 SELECT ──────────────────────────────────────────────────────

    public static async Task<IReadOnlyList<Product>> GetFeaturedProducts()
    {
        await using var session = new DbSessionAsync(CreateConnection());
        await session.OpenAsync();

        return await session.QueryAsync(
            sql: "SELECT Id, Name, UnitPrice FROM Products WHERE IsFeatured = @featured",
            map: r => new Product(r.GetGuid(0), r.GetString(1), r.GetDecimal(2)),
            parameters: DbParam.Of(("@featured", true)));
    }


    // ── 単一行 SELECT ──────────────────────────────────────────────────────

    public static async Task<Product?> FindProduct(Guid id)
    {
        await using var session = new DbSessionAsync(CreateConnection());
        await session.OpenAsync();

        return await session.QuerySingleOrDefaultAsync(
            sql: "SELECT Id, Name, UnitPrice FROM Products WHERE Id = @id",
            map: r => new Product(r.GetGuid(0), r.GetString(1), r.GetDecimal(2)),
            parameters: DbParam.Of(("@id", id)));
    }


    // ── スカラー SELECT（COUNT など） ───────────────────────────────────────

    public static async Task<int> CountFeaturedProducts()
    {
        await using var session = new DbSessionAsync(CreateConnection());
        await session.OpenAsync();

        return await session.ExecuteScalarAsync<int>(
            sql: "SELECT COUNT(*) FROM Products WHERE IsFeatured = @featured",
            parameters: DbParam.Of(("@featured", true))) ?? 0;
    }


    // ── INSERT ─────────────────────────────────────────────────────────────

    public static async Task AddProduct(Product product)
    {
        await using var session = new DbSessionAsync(CreateConnection());
        await session.OpenAsync();

        await session.ExecuteAsync(
            sql: @"INSERT INTO Products (Id, Name, UnitPrice, IsFeatured)
                   VALUES (@id, @name, @price, @featured)",
            parameters: DbParam.Of(
                ("@id",       product.Id),
                ("@name",     product.Name),
                ("@price",    product.UnitPrice),
                ("@featured", false)));
    }


    // ── トランザクション（自動管理） ───────────────────────────────────────
    // 例外が発生した場合は自動で Rollback → 例外を再スロー

    public static async Task PlaceOrder(Guid orderId, Guid productId, int quantity)
    {
        await using var session = new DbSessionAsync(CreateConnection());
        await session.OpenAsync();

        await session.ExecuteInTransactionAsync(async () =>
        {
            await session.ExecuteAsync(
                sql: "INSERT INTO Orders (Id, ProductId, Quantity) VALUES (@id, @pid, @qty)",
                parameters: DbParam.Of(
                    ("@id",  orderId),
                    ("@pid", productId),
                    ("@qty", quantity)));

            await session.ExecuteAsync(
                sql: "UPDATE Stock SET Quantity = Quantity - @qty WHERE ProductId = @pid",
                parameters: DbParam.Of(
                    ("@qty", quantity),
                    ("@pid", productId)));
        });
    }


    // ── トランザクション（手動制御） ───────────────────────────────────────
    // コミット前に結果を確認するなど、途中で判断が必要な場合に使う

    public static async Task PlaceOrderManual(Guid orderId, Guid productId, int quantity)
    {
        await using var session = new DbSessionAsync(CreateConnection());
        await session.OpenAsync();
        await session.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            await session.ExecuteAsync(
                sql: "INSERT INTO Orders (Id, ProductId, Quantity) VALUES (@id, @pid, @qty)",
                parameters: DbParam.Of(
                    ("@id",  orderId),
                    ("@pid", productId),
                    ("@qty", quantity)));

            int remaining = await session.ExecuteScalarAsync<int>(
                sql: "SELECT Quantity FROM Stock WHERE ProductId = @pid",
                parameters: DbParam.Of(("@pid", productId))) ?? 0;

            if (remaining < quantity)
                throw new InvalidOperationException("在庫が不足しています。");

            await session.ExecuteAsync(
                sql: "UPDATE Stock SET Quantity = Quantity - @qty WHERE ProductId = @pid",
                parameters: DbParam.Of(
                    ("@qty", quantity),
                    ("@pid", productId)));

            await session.CommitAsync();
        }
        catch
        {
            await session.RollbackAsync();
            throw;
        }
    }
}
