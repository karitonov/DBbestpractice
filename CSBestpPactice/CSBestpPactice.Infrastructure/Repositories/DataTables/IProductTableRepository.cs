using System.Data;

namespace CSBestpPactice.Infrastructure.Repositories.DataTables;

public interface IProductTableRepository
{
    DataTable GetAll();
    DataTable GetById(Guid id);
    DataTable GetFeaturedProducts();

    Task<DataTable> GetAllAsync();
    Task<DataTable> GetByIdAsync(Guid id);
    Task<DataTable> GetFeaturedProductsAsync();
}
