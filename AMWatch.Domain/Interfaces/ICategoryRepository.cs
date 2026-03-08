using AMWatch.Domain.Entities;

namespace AMWatch.Domain.Interfaces;

public interface ICategoryRepository
{
    Task<IEnumerable<Category>> GetAllAsync();
    Task AddAsync(Category category);
    Task DeleteAsync(Guid categoryId);
}
