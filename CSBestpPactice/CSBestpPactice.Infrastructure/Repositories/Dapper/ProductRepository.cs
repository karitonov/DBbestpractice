using CSBestpPactice.Domain.Entities;
using CSBestpPactice.Domain.Repositories;
using CSBestpPactice.Infrastructure.Data.Factories;
using Dapper;
using System.Data.Common;

namespace CSBestpPactice.Infrastructure.Repositories.Dapper;

internal sealed class ProductRepository : IProductRepository, IProductRepositoryAsync
{
    private readonly IDbConnectionFactory _factory;

    public ProductRepository(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    private DbConnection OpenConnection()
    {
        var conn = _factory.CreateConnection();
        conn.Open();
        return conn;
    }

    private async Task<DbConnection> OpenConnectionAsync()
    {
        var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        return conn;
    }

    public IReadOnlyList<Product> GetAll()
    {
        using var conn = OpenConnection();
        return conn.Query<Product>(
            "SELECT Id, Name, Description, UnitPrice, IsFeatured FROM Products").ToList();
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync()
    {
        await using var conn = await OpenConnectionAsync();
        return (await conn.QueryAsync<Product>(
            "SELECT Id, Name, Description, UnitPrice, IsFeatured FROM Products")).ToList();
    }

    public Product? GetById(Guid id)
    {
        using var conn = OpenConnection();
        return conn.QuerySingleOrDefault<Product>(
            "SELECT Id, Name, Description, UnitPrice, IsFeatured FROM Products WHERE Id = @Id",
            new { Id = id.ToString() });
    }

    public async Task<Product?> GetByIdAsync(Guid id)
    {
        await using var conn = await OpenConnectionAsync();
        return await conn.QuerySingleOrDefaultAsync<Product>(
            "SELECT Id, Name, Description, UnitPrice, IsFeatured FROM Products WHERE Id = @Id",
            new { Id = id.ToString() });
    }

    public void Add(Product entity)
    {
        using var conn = OpenConnection();
        conn.Execute(
            "INSERT INTO Products (Id, Name, Description, UnitPrice, IsFeatured) VALUES (@Id, @Name, @Description, @UnitPrice, @IsFeatured)",
            new { Id = entity.Id.ToString(), entity.Name, entity.Description, entity.UnitPrice, entity.IsFeatured });
    }

    public async Task AddAsync(Product entity)
    {
        await using var conn = await OpenConnectionAsync();
        await conn.ExecuteAsync(
            "INSERT INTO Products (Id, Name, Description, UnitPrice, IsFeatured) VALUES (@Id, @Name, @Description, @UnitPrice, @IsFeatured)",
            new { Id = entity.Id.ToString(), entity.Name, entity.Description, entity.UnitPrice, entity.IsFeatured });
    }

    public void Update(Product entity)
    {
        using var conn = OpenConnection();
        conn.Execute(
            "UPDATE Products SET Name = @Name, Description = @Description, UnitPrice = @UnitPrice, IsFeatured = @IsFeatured WHERE Id = @Id",
            new { Id = entity.Id.ToString(), entity.Name, entity.Description, entity.UnitPrice, entity.IsFeatured });
    }

    public async Task UpdateAsync(Product entity)
    {
        await using var conn = await OpenConnectionAsync();
        await conn.ExecuteAsync(
            "UPDATE Products SET Name = @Name, Description = @Description, UnitPrice = @UnitPrice, IsFeatured = @IsFeatured WHERE Id = @Id",
            new { Id = entity.Id.ToString(), entity.Name, entity.Description, entity.UnitPrice, entity.IsFeatured });
    }

    public void Delete(Guid id)
    {
        using var conn = OpenConnection();
        conn.Execute("DELETE FROM Products WHERE Id = @Id", new { Id = id.ToString() });
    }

    public async Task DeleteAsync(Guid id)
    {
        await using var conn = await OpenConnectionAsync();
        await conn.ExecuteAsync("DELETE FROM Products WHERE Id = @Id", new { Id = id.ToString() });
    }

    public IReadOnlyList<Product> GetFeaturedProducts()
    {
        using var conn = OpenConnection();
        return conn.Query<Product>(
            "SELECT Id, Name, Description, UnitPrice, IsFeatured FROM Products WHERE IsFeatured = 1").ToList();
    }

    public async Task<IReadOnlyList<Product>> GetFeaturedProductsAsync()
    {
        await using var conn = await OpenConnectionAsync();
        return (await conn.QueryAsync<Product>(
            "SELECT Id, Name, Description, UnitPrice, IsFeatured FROM Products WHERE IsFeatured = 1")).ToList();
    }
}
