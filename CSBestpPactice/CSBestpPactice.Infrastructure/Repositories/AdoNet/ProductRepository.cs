using CSBestpPactice.Domain.Entities;
using CSBestpPactice.Domain.Repositories;
using CSBestpPactice.Infrastructure.Data;
using CSBestpPactice.Infrastructure.Data.Sessions;
using System.Data.Common;

namespace CSBestpPactice.Infrastructure.Repositories.AdoNet;

internal sealed class ProductRepository : IProductRepository
{
    private readonly IDbSession _session;

    public ProductRepository(IDbSession session)
    {
        _session = session;
    }

    public IReadOnlyList<Product> GetAll()
    {
        var sql = "SELECT Id, Name, Description, UnitPrice, IsFeatured FROM Products";
        return _session.Query(sql, Map);
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync()
    {
        var sql = "SELECT Id, Name, Description, UnitPrice, IsFeatured FROM Products";
        return await _session.QueryAsync(sql, Map);
    }

    public Product? GetById(Guid id)
    {
        var sql = "SELECT Id, Name, Description, UnitPrice, IsFeatured FROM Products WHERE Id = @Id";
        return _session.QuerySingleOrDefault(sql, Map, DbParam.Of(("@Id", id.ToString())));
    }

    public async Task<Product?> GetByIdAsync(Guid id)
    {
        var sql = "SELECT Id, Name, Description, UnitPrice, IsFeatured FROM Products WHERE Id = @Id";
        return await _session.QuerySingleOrDefaultAsync(sql, Map, DbParam.Of(("@Id", id.ToString())));
    }

    public void Add(Product entity)
    {
        var sql = "INSERT INTO Products (Id, Name, Description, UnitPrice, IsFeatured) VALUES (@Id, @Name, @Description, @UnitPrice, @IsFeatured)";
        _session.Execute(sql, DbParam.Of(
            ("@Id",          entity.Id.ToString()),
            ("@Name",        entity.Name),
            ("@Description", entity.Description),
            ("@UnitPrice",   entity.UnitPrice),
            ("@IsFeatured",  entity.IsFeatured)));
    }

    public async Task AddAsync(Product entity)
    {
        var sql = "INSERT INTO Products (Id, Name, Description, UnitPrice, IsFeatured) VALUES (@Id, @Name, @Description, @UnitPrice, @IsFeatured)";
        await _session.ExecuteAsync(sql, DbParam.Of(
            ("@Id",          entity.Id.ToString()),
            ("@Name",        entity.Name),
            ("@Description", entity.Description),
            ("@UnitPrice",   entity.UnitPrice),
            ("@IsFeatured",  entity.IsFeatured)));
    }

    public void Update(Product entity)
    {
        var sql = "UPDATE Products SET Name = @Name, Description = @Description, UnitPrice = @UnitPrice, IsFeatured = @IsFeatured WHERE Id = @Id";
        _session.Execute(sql, DbParam.Of(
            ("@Name",        entity.Name),
            ("@Description", entity.Description),
            ("@UnitPrice",   entity.UnitPrice),
            ("@IsFeatured",  entity.IsFeatured),
            ("@Id",          entity.Id.ToString())));
    }

    public async Task UpdateAsync(Product entity)
    {
        var sql = "UPDATE Products SET Name = @Name, Description = @Description, UnitPrice = @UnitPrice, IsFeatured = @IsFeatured WHERE Id = @Id";
        await _session.ExecuteAsync(sql, DbParam.Of(
            ("@Name",        entity.Name),
            ("@Description", entity.Description),
            ("@UnitPrice",   entity.UnitPrice),
            ("@IsFeatured",  entity.IsFeatured),
            ("@Id",          entity.Id.ToString())));
    }

    public void Delete(Guid id)
    {
        var sql = "DELETE FROM Products WHERE Id = @Id";
        _session.Execute(sql, DbParam.Of(("@Id", id.ToString())));
    }

    public async Task DeleteAsync(Guid id)
    {
        var sql = "DELETE FROM Products WHERE Id = @Id";
        await _session.ExecuteAsync(sql, DbParam.Of(("@Id", id.ToString())));
    }

    public IReadOnlyList<Product> GetFeaturedProducts()
    {
        var sql = "SELECT Id, Name, Description, UnitPrice, IsFeatured FROM Products WHERE IsFeatured = 1";
        return _session.Query(sql, Map);
    }

    public async Task<IReadOnlyList<Product>> GetFeaturedProductsAsync()
    {
        var sql = "SELECT Id, Name, Description, UnitPrice, IsFeatured FROM Products WHERE IsFeatured = 1";
        return await _session.QueryAsync(sql, Map);
    }

    private static Product Map(DbDataReader reader) => new Product
    {
        Id          = Guid.Parse(reader.GetString(reader.GetOrdinal("Id"))),
        Name        = reader.GetString(reader.GetOrdinal("Name")),
        Description = reader.IsDBNull(reader.GetOrdinal("Description"))
                          ? null
                          : reader.GetString(reader.GetOrdinal("Description")),
        UnitPrice   = reader.GetDecimal(reader.GetOrdinal("UnitPrice")),
        IsFeatured  = reader.GetBoolean(reader.GetOrdinal("IsFeatured")),
    };
}
