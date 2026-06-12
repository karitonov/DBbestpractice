using CSBestpPactice.Domain.Entities;

namespace CSBestpPactice.Domain.Repositories;

public interface IProductRepository : IRepository<Product>
{
    IReadOnlyList<Product> GetFeaturedProducts();
}
