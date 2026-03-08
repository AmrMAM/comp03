using AMWatch.Domain.Entities;
using AMWatch.Domain.Interfaces;
using AMWatch.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AMWatch.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context) => _context = context;

    public Task<User?> GetByEmailAsync(string email) => _context.Users.FirstOrDefaultAsync(x => x.Email == email);

    public Task<User?> GetByIdAsync(Guid id) => _context.Users.FindAsync(id).AsTask();

    public async Task AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
    }
}
