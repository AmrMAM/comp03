using AMWatch.Domain.Entities;
using AMWatch.Domain.Interfaces;
using AMWatch.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AMWatch.Infrastructure.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly ApplicationDbContext _context;

    public CategoryRepository(ApplicationDbContext context) => _context = context;

    public async Task<IEnumerable<Category>> GetAllAsync() => await _context.Categories.ToListAsync();

    public async Task AddAsync(Category category)
    {
        await _context.Categories.AddAsync(category);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid categoryId)
    {
        var entity = await _context.Categories.FindAsync(categoryId);
        if (entity is null) return;
        _context.Categories.Remove(entity);
        await _context.SaveChangesAsync();
    }
}
