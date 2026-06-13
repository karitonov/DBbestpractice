using CSBestpPactice.Domain.Entities;
using CSBestpPactice.Domain.Repositories;
using CSBestpPactice.Infrastructure.Data.Factories;
using Dapper;

namespace CSBestpPactice.Infrastructure.Repositories.Dapper;

internal sealed class ProductRepositoryAsync : IProductRepositoryAsync
{
    private readonly IDbConnectionFactory _factory;

    public ProductRepositoryAsync(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync()
    {
        var sql = "SELECT Id, Name, Description, UnitPrice, IsFeatured FROM Products";
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        return (await conn.QueryAsync<Product>(sql)).ToList();
    }

    public async Task<Product?> GetByIdAsync(Guid id)
    {
        var sql = "SELECT Id, Name, Description, UnitPrice, IsFeatured FROM Products WHERE Id = @Id";
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        return await conn.QuerySingleOrDefaultAsync<Product>(sql, new { Id = id.ToString() });
    }

    public async Task AddAsync(Product entity)
    {
        var sql = "INSERT INTO Products (Id, Name, Description, UnitPrice, IsFeatured) VALUES (@Id, @Name, @Description, @UnitPrice, @IsFeatured)";
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        await conn.ExecuteAsync(
            sql,
            new
            {
                Id = entity.Id.ToString(),
                Name = entity.Name,
                Description = entity.Description,
                UnitPrice = entity.UnitPrice,
                IsFeatured = entity.IsFeatured
            });
    }

    public async Task UpdateAsync(Product entity)
    {
        var sql = "UPDATE Products SET Name = @Name, Description = @Description, UnitPrice = @UnitPrice, IsFeatured = @IsFeatured WHERE Id = @Id";
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        await conn.ExecuteAsync(
            sql,
            new
            {
                Id = entity.Id.ToString(),
                Name = entity.Name,
                Description = entity.Description,
                UnitPrice = entity.UnitPrice,
                IsFeatured = entity.IsFeatured
            });
    }

    public async Task DeleteAsync(Guid id)
    {
        var sql = "DELETE FROM Products WHERE Id = @Id";
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        await conn.ExecuteAsync(sql, new { Id = id.ToString() });
    }

    public async Task<IReadOnlyList<Product>> GetFeaturedProductsAsync()
    {
        var sql = "SELECT Id, Name, Description, UnitPrice, IsFeatured FROM Products WHERE IsFeatured = 1";
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        return (await conn.QueryAsync<Product>(sql)).ToList();
    }
}
