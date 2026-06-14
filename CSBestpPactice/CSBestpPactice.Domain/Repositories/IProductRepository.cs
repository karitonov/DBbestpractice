using CSBestpPactice.Domain.Entities;

namespace CSBestpPactice.Domain.Repositories;

public interface IProductRepository : IRepository<Product>, IRepositoryAsync<Product>
{
    IReadOnlyList<Product> GetFeaturedProducts();
    Task<IReadOnlyList<Product>> GetFeaturedProductsAsync();
}
