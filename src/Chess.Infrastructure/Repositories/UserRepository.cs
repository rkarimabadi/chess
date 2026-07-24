using Chess.Application.Ports;
using Chess.Domain.Entities;
using Chess.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Chess.Infrastructure.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly ChessDbContext _db;

    public UserRepository(ChessDbContext db) => _db = db;

    public async Task<User?> GetByIdAsync(Guid id) =>
        await _db.Users.FindAsync(id);

    public async Task<User?> GetByUsernameAsync(string username) =>
        await _db.Users.FirstOrDefaultAsync(u => u.Username == username);

    public async Task<User?> GetByEmailAsync(string email) =>
        await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

    public async Task<bool> UsernameExistsAsync(string username) =>
        await _db.Users.AnyAsync(u => u.Username == username);

    public async Task<bool> EmailExistsAsync(string email) =>
        await _db.Users.AnyAsync(u => u.Email == email);

    public async Task<IReadOnlyList<User>> SearchAsync(string query, int page, int pageSize) =>
        await _db.Users
            .Where(u => u.Username.Contains(query) || u.Email.Contains(query))
            .OrderBy(u => u.Username)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

    public async Task AddAsync(User user) =>
        await _db.Users.AddAsync(user);

    public void Update(User user) =>
        _db.Users.Update(user);
}
