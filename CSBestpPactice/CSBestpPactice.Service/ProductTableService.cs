using CSBestpPactice.Domain.Repositories;
using System.Data;

namespace CSBestpPactice.Service;

public sealed class ProductTableService : IProductTableService
{
    private readonly IProductTableRepository _repository;

    public ProductTableService(IProductTableRepository repository)
    {
        _repository = repository;
    }

    #region 同期
    public DataTable GetAll()
    {
        return _repository.GetAll();
    }

    public DataTable GetById(Guid id)
    {
        return _repository.GetById(id);
    }

    public DataTable GetFeatured()
    {
        return _repository.GetFeaturedProducts();
    }

    public int Update(DataTable table)
    {
        return _repository.Update(table);
    }

    #endregion

    #region 非同期
    public async Task<DataTable> GetAllAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<DataTable> GetByIdAsync(Guid id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<DataTable> GetFeaturedAsync()
    {
        return await _repository.GetFeaturedProductsAsync();
    }

    public async Task<int> UpdateAsync(DataTable table)
    {
        return await _repository.UpdateAsync(table);
    }
    #endregion
}
