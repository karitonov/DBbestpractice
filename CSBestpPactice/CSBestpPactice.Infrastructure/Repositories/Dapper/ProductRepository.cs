using CSBestpPactice.Domain.Entities;
using CSBestpPactice.Domain.Repositories;
using CSBestpPactice.Infrastructure.Data.Factories;
using Dapper;
using System.Data.Common;

namespace CSBestpPactice.Infrastructure.Repositories.Dapper;

public sealed class ProductRepository : IProductRepository
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
        var sql = "SELECT Id, Name, Description, UnitPrice, IsFeatured FROM Products";
        using var conn = OpenConnection();
        return conn.Query<Product>(sql).ToList();
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync()
    {
        var sql = "SELECT Id, Name, Description, UnitPrice, IsFeatured FROM Products";
        await using var conn = await OpenConnectionAsync();
        return (await conn.QueryAsync<Product>(sql)).ToList();
    }

    public Product? GetById(Guid id)
    {
        var sql = "SELECT Id, Name, Description, UnitPrice, IsFeatured FROM Products WHERE Id = @Id";
        using var conn = OpenConnection();
        return conn.QuerySingleOrDefault<Product>(sql, new { Id = id });
    }

    public async Task<Product?> GetByIdAsync(Guid id)
    {
        var sql = "SELECT Id, Name, Description, UnitPrice, IsFeatured FROM Products WHERE Id = @Id";
        await using var conn = await OpenConnectionAsync();
        return await conn.QuerySingleOrDefaultAsync<Product>(sql, new { Id = id });
    }

    public void Add(Product entity)
    {
        var sql = "INSERT INTO Products (Id, Name, Description, UnitPrice, IsFeatured) VALUES (@Id, @Name, @Description, @UnitPrice, @IsFeatured)";
        using var conn = OpenConnection();
        conn.Execute(sql, new
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            UnitPrice = entity.UnitPrice,
            IsFeatured = entity.IsFeatured
        });
    }

    public async Task AddAsync(Product entity)
    {
        var sql = "INSERT INTO Products (Id, Name, Description, UnitPrice, IsFeatured) VALUES (@Id, @Name, @Description, @UnitPrice, @IsFeatured)";
        await using var conn = await OpenConnectionAsync();
        await conn.ExecuteAsync(sql, new
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            UnitPrice = entity.UnitPrice,
            IsFeatured = entity.IsFeatured
        });
    }

    public void Update(Product entity)
    {
        var sql = "UPDATE Products SET Name = @Name, Description = @Description, UnitPrice = @UnitPrice, IsFeatured = @IsFeatured WHERE Id = @Id";
        using var conn = OpenConnection();
        conn.Execute(sql, new
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            UnitPrice = entity.UnitPrice,
            IsFeatured = entity.IsFeatured
        });
    }

    public async Task UpdateAsync(Product entity)
    {
        var sql = "UPDATE Products SET Name = @Name, Description = @Description, UnitPrice = @UnitPrice, IsFeatured = @IsFeatured WHERE Id = @Id";
        await using var conn = await OpenConnectionAsync();
        await conn.ExecuteAsync(sql, new
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            UnitPrice = entity.UnitPrice,
            IsFeatured = entity.IsFeatured
        });
    }

    // Dapper自体はトランザクション管理機構を持たないため、IDbTransactionを生成して
    // 各Executeに明示的に渡すことで複数文を1トランザクションにまとめる。
    public int UpdateMany(IEnumerable<Product> entities)
    {
        var sql = "UPDATE Products SET Name = @Name, Description = @Description, UnitPrice = @UnitPrice, IsFeatured = @IsFeatured WHERE Id = @Id";
        using var conn = OpenConnection();
        using var transaction = conn.BeginTransaction();
        try
        {
            var count = 0;
            foreach (var entity in entities)
            {
                count += conn.Execute(sql, new
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    Description = entity.Description,
                    UnitPrice = entity.UnitPrice,
                    IsFeatured = entity.IsFeatured
                }, transaction: transaction);
            }
            transaction.Commit();
            return count;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<int> UpdateManyAsync(IEnumerable<Product> entities)
    {
        var sql = "UPDATE Products SET Name = @Name, Description = @Description, UnitPrice = @UnitPrice, IsFeatured = @IsFeatured WHERE Id = @Id";
        await using var conn = await OpenConnectionAsync();
        await using var transaction = await conn.BeginTransactionAsync();
        try
        {
            var count = 0;
            foreach (var entity in entities)
            {
                count += await conn.ExecuteAsync(sql, new
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    Description = entity.Description,
                    UnitPrice = entity.UnitPrice,
                    IsFeatured = entity.IsFeatured
                }, transaction: transaction);
            }
            await transaction.CommitAsync();
            return count;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public void Delete(Guid id)
    {
        var sql = "DELETE FROM Products WHERE Id = @Id";
        using var conn = OpenConnection();
        conn.Execute(sql, new { Id = id });
    }

    public async Task DeleteAsync(Guid id)
    {
        var sql = "DELETE FROM Products WHERE Id = @Id";
        await using var conn = await OpenConnectionAsync();
        await conn.ExecuteAsync(sql, new { Id = id });
    }

    public IReadOnlyList<Product> GetFeaturedProducts()
    {
        var sql = "SELECT Id, Name, Description, UnitPrice, IsFeatured FROM Products WHERE IsFeatured = 1";
        using var conn = OpenConnection();
        return conn.Query<Product>(sql).ToList();
    }

    public async Task<IReadOnlyList<Product>> GetFeaturedProductsAsync()
    {
        var sql = "SELECT Id, Name, Description, UnitPrice, IsFeatured FROM Products WHERE IsFeatured = 1";
        await using var conn = await OpenConnectionAsync();
        return (await conn.QueryAsync<Product>(sql)).ToList();
    }
}
