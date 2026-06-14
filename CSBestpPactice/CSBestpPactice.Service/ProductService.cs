using CSBestpPactice.Domain.Entities;
using CSBestpPactice.Domain.Repositories;

namespace CSBestpPactice.Service;

internal sealed class ProductService : IProductService
{
    private readonly IProductRepository _repository;

    public ProductService(IProductRepository repository)
    {
        _repository = repository;
    }

    public IReadOnlyList<Product> GetAll()
    {
        return _repository.GetAll();
    }

    public Product? GetById(Guid id)
    {
        return _repository.GetById(id);
    }

    public void Register(Product product)
    {
        _repository.Add(product);
    }

    public void Update(Product product)
    {
        _repository.Update(product);
    }

    public void Delete(Guid id)
    {
        _repository.Delete(id);
    }

    public IReadOnlyList<Product> GetFeatured()
    {
        return _repository.GetFeaturedProducts();
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<Product?> GetByIdAsync(Guid id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<IReadOnlyList<Product>> GetFeaturedAsync()
    {
        return await _repository.GetFeaturedProductsAsync();
    }

    public async Task RegisterAsync(Product product)
    {
        await _repository.AddAsync(product);
    }

    public async Task UpdateAsync(Product product)
    {
        await _repository.UpdateAsync(product);
    }

    public async Task DeleteAsync(Guid id)
    {
        await _repository.DeleteAsync(id);
    }
}
