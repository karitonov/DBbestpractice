namespace CSBestpPactice.Domain.Repositories;

public interface IRepository<T> where T : class
{
    IReadOnlyList<T> GetAll();
    T? GetById(Guid id);
    void Add(T entity);
    void Update(T entity);
    void Delete(Guid id);
}
