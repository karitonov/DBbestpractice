using System.Data;

namespace CSBestpPactice.Infrastructure.Repositories.DataTables;

public interface IProductTableRepository
{
    DataTable GetAll();
    DataTable GetById(Guid id);
    DataTable GetFeaturedProducts();
    int Update(DataTable table);

    Task<DataTable> GetAllAsync();
    Task<DataTable> GetByIdAsync(Guid id);
    Task<DataTable> GetFeaturedProductsAsync();
    Task<int> UpdateAsync(DataTable table);
}
