using Chess.Application.Ports;
using Chess.Domain.Entities;
using Chess.Domain.ValueObjects;
using Chess.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Chess.Infrastructure.Repositories;

public sealed class SanctionRepository : ISanctionRepository
{
    private readonly ChessDbContext _db;

    public SanctionRepository(ChessDbContext db) => _db = db;

    public async Task<IReadOnlyList<UserSanction>> GetActiveByUserIdAsync(Guid userId) =>
        await _db.UserSanctions
            .Where(s => s.UserId == userId && s.IsActive)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

    public async Task<UserSanction?> GetByIdAsync(Guid id) =>
        await _db.UserSanctions.FindAsync(id);

    public async Task AddAsync(UserSanction sanction) =>
        await _db.UserSanctions.AddAsync(sanction);

    public void Update(UserSanction sanction) =>
        _db.UserSanctions.Update(sanction);

    public async Task<int> ExpireStaleBansAsync()
    {
        var now = DateTime.UtcNow;
        var stale = await _db.UserSanctions
            .Where(s => s.IsActive && s.EndsAt != null && s.EndsAt < now)
            .ToListAsync();

        foreach (var sanction in stale)
            sanction.Deactivate();

        return await _db.SaveChangesAsync();
    }

    public async Task<int> GetRecentBansCountAsync(int days)
    {
        var since = DateTime.UtcNow.AddDays(-days);
        return await _db.UserSanctions
            .CountAsync(s => (s.Type == SanctionType.TempBan || s.Type == SanctionType.PermBan)
                            && s.CreatedAt >= since);
    }
}
