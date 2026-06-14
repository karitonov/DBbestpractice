using CSBestpPactice.Domain.Entities;

namespace CSBestpPactice.Service
{
    public interface IProductService
    {
        #region 同期
        IReadOnlyList<Product> GetAll();
        Product? GetById(Guid id);
        IReadOnlyList<Product> GetFeatured();
        void Register(Product product);
        void Update(Product product);
        void Delete(Guid id);
        #endregion

        #region 非同期
        Task<IReadOnlyList<Product>> GetAllAsync();
        Task<Product?> GetByIdAsync(Guid id);
        Task<IReadOnlyList<Product>> GetFeaturedAsync();
        Task RegisterAsync(Product product);
        Task UpdateAsync(Product product);
        Task DeleteAsync(Guid id);
        #endregion
    }
}
