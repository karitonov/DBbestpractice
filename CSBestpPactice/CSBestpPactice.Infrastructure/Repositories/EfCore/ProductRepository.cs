using CSBestpPactice.Domain.Entities;
using CSBestpPactice.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CSBestpPactice.Infrastructure.Repositories.EfCore;

internal sealed class ProductRepository : IProductRepository, IProductRepositoryAsync
{
    private readonly AppDbContext _context;

    public ProductRepository(AppDbContext context)
    {
        _context = context;
    }

    public IReadOnlyList<Product> GetAll()
    {
        return _context.Products.ToList();
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync()
    {
        return await _context.Products.ToListAsync();
    }

    public Product? GetById(Guid id)
    {
        return _context.Products.FirstOrDefault(p => p.Id == id);
    }

    public async Task<Product?> GetByIdAsync(Guid id)
    {
        return await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
    }

    public void Add(Product entity)
    {
        _context.Products.Add(entity);
        _context.SaveChanges();
    }

    public async Task AddAsync(Product entity)
    {
        _context.Products.Add(entity);
        await _context.SaveChangesAsync();
    }

    public void Update(Product entity)
    {
        _context.Products.Update(entity);
        _context.SaveChanges();
    }

    public async Task UpdateAsync(Product entity)
    {
        _context.Products.Update(entity);
        await _context.SaveChangesAsync();
    }

    public void Delete(Guid id)
    {
        var product = _context.Products.FirstOrDefault(p => p.Id == id);
        if (product != null)
        {
            _context.Products.Remove(product);
            _context.SaveChanges();
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product != null)
        {
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
        }
    }
    public IReadOnlyList<Product> GetFeaturedProducts()
    {
        return _context.Products.Where(p => p.IsFeatured).ToList();
    }

    public async Task<IReadOnlyList<Product>> GetFeaturedProductsAsync()
    {
        return await _context.Products.Where(p => p.IsFeatured).ToListAsync();
    }
}
