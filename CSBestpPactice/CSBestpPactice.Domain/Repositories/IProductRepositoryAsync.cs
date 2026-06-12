using CSBestpPactice.Domain.Entities;

namespace CSBestpPactice.Domain.Repositories;

public interface IProductRepositoryAsync : IRepositoryAsync<Product>
{
    Task<IReadOnlyList<Product>> GetFeaturedProductsAsync();
}
