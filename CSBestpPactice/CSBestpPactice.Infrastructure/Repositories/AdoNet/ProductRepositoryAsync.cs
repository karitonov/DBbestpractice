using CSBestpPactice.Domain.Entities;
using CSBestpPactice.Domain.Repositories;
using CSBestpPactice.Infrastructure.Data;
using CSBestpPactice.Infrastructure.Data.Sessions;
using System.Data.Common;

namespace CSBestpPactice.Infrastructure.Repositories.AdoNet;

internal sealed class ProductRepositoryAsync : IProductRepositoryAsync
{
    private readonly IDbSession _session;

    public ProductRepositoryAsync(IDbSession session)
    {
        _session = session;
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync()
    {
        var sql = "SELECT Id, Name, Description, UnitPrice, IsFeatured FROM Products";
        return await _session.QueryAsync(sql, Map);
    }

    public async Task<Product?> GetByIdAsync(Guid id)
    {
        var sql = "SELECT Id, Name, Description, UnitPrice, IsFeatured FROM Products WHERE Id = @Id";
        var parameters = DbParam.Of(("@Id", id.ToString()));
        return await _session.QuerySingleOrDefaultAsync(sql, Map, parameters);
    }

    public async Task AddAsync(Product entity)
    {
        var sql = "INSERT INTO Products (Id, Name, Description, UnitPrice, IsFeatured) VALUES (@Id, @Name, @Description, @UnitPrice, @IsFeatured)";
        var parameters = DbParam.Of(
            ("@Id", entity.Id.ToString()),
            ("@Name", entity.Name),
            ("@Description", entity.Description),
            ("@UnitPrice", entity.UnitPrice),
            ("@IsFeatured", entity.IsFeatured)
        );
        await _session.ExecuteAsync(sql, parameters);
    }

    public async Task UpdateAsync(Product entity)
    {
        var sql = "UPDATE Products SET Name = @Name, Description = @Description, UnitPrice = @UnitPrice, IsFeatured = @IsFeatured WHERE Id = @Id";
        var parameters = DbParam.Of(
            ("@Id", entity.Id.ToString()),
            ("@Name", entity.Name),
            ("@Description", entity.Description),
            ("@UnitPrice", entity.UnitPrice),
            ("@IsFeatured", entity.IsFeatured)
        );
        await _session.ExecuteAsync(sql, parameters);
    }

    public async Task DeleteAsync(Guid id)
    {
        var sql = "DELETE FROM Products WHERE Id = @Id";
        var parameters = DbParam.Of(("@Id", id.ToString()));
        await _session.ExecuteAsync(sql, parameters);
    }

    public async Task<IReadOnlyList<Product>> GetFeaturedProductsAsync()
    {
        var sql = "SELECT Id, Name, Description, UnitPrice, IsFeatured FROM Products WHERE IsFeatured = @IsFeatured";
        var parameters = DbParam.Of(("@IsFeatured", true));
        return await _session.QueryAsync(sql, Map, parameters);
    }

    private static Product Map(DbDataReader reader) => new Product
    {
        Id = Guid.Parse(reader.GetString(reader.GetOrdinal("Id"))),
        Name = reader.GetString(reader.GetOrdinal("Name")),
        Description = reader.IsDBNull(reader.GetOrdinal("Description"))
                      ? null
                      : reader.GetString(reader.GetOrdinal("Description")),
        UnitPrice = reader.GetDecimal(reader.GetOrdinal("UnitPrice")),
        IsFeatured = reader.GetBoolean(reader.GetOrdinal("IsFeatured")),
    };
}
