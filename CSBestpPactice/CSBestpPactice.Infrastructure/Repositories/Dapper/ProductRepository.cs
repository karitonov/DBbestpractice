using CSBestpPactice.Domain.Entities;
using CSBestpPactice.Domain.Repositories;
using CSBestpPactice.Infrastructure.Data.Factories;
using Dapper;

namespace CSBestpPactice.Infrastructure.Repositories.Dapper;

internal sealed class ProductRepository : IProductRepository
{
    private readonly IDbConnectionFactory _factory;

    public ProductRepository(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public IReadOnlyList<Product> GetAll()
    {
        var sql = "SELECT Id, Name, Description, UnitPrice, IsFeatured FROM Products";
        using var conn = _factory.CreateConnection();
        conn.Open();
        return conn.Query<Product>(sql).ToList();
    }

    public Product? GetById(Guid id)
    {
        var sql = "SELECT Id, Name, Description, UnitPrice, IsFeatured FROM Products WHERE Id = @Id";
        using var conn = _factory.CreateConnection();
        conn.Open();
        return conn.QuerySingleOrDefault<Product>(sql, new { Id = id.ToString() });
    }

    public void Add(Product entity)
    {
        var sql = "INSERT INTO Products (Id, Name, Description, UnitPrice, IsFeatured) VALUES (@Id, @Name, @Description, @UnitPrice, @IsFeatured)";
        using var conn = _factory.CreateConnection();
        conn.Open();
        conn.Execute(
            sql, 
            new {
                Id = entity.Id.ToString(),
                Name = entity.Name,
                Description = entity.Description,
                UnitPrice = entity.UnitPrice,
                IsFeatured = entity.IsFeatured
            });
    }

    public void Update(Product entity)
    {
        var sql = "UPDATE Products SET Name = @Name, Description = @Description, UnitPrice = @UnitPrice, IsFeatured = @IsFeatured WHERE Id = @Id";
        using var conn = _factory.CreateConnection();
        conn.Open();
        conn.Execute(
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

    public void Delete(Guid id)
    {
        var sql = "DELETE FROM Products WHERE Id = @Id";
        using var conn = _factory.CreateConnection();
        conn.Open();
        conn.Execute(sql, new { Id = id.ToString() });
    }

    public IReadOnlyList<Product> GetFeaturedProducts()
    {
        var sql = "SELECT Id, Name, Description, UnitPrice, IsFeatured FROM Products WHERE IsFeatured = @IsFeatured";
        using var conn = _factory.CreateConnection();
        conn.Open();
        return conn.Query<Product>(sql, new { IsFeatured = true }).ToList();
    }
}
