using System.Data;

namespace CSBestpPactice.Service;

public interface IProductTableService
{
    #region 同期
    DataTable GetAll();
    DataTable GetById(Guid id);
    DataTable GetFeatured();
    int Update(DataTable table);
    #endregion

    #region 非同期
    Task<DataTable> GetAllAsync();
    Task<DataTable> GetByIdAsync(Guid id);
    Task<DataTable> GetFeaturedAsync();
    Task<int> UpdateAsync(DataTable table);
    #endregion
}
