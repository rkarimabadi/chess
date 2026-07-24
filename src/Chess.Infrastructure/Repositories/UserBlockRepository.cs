using Chess.Application.Ports;
using Chess.Domain.Entities;
using Chess.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Chess.Infrastructure.Repositories;

public sealed class UserBlockRepository : IUserBlockRepository
{
    private readonly ChessDbContext _db;

    public UserBlockRepository(ChessDbContext db) => _db = db;

    public async Task<UserBlock?> GetAsync(Guid blockerId, Guid blockedUserId) =>
        await _db.UserBlocks.FirstOrDefaultAsync(b =>
            b.BlockerId == blockerId && b.BlockedUserId == blockedUserId);

    public async Task<bool> IsBlockedAsync(Guid userId1, Guid userId2) =>
        await _db.UserBlocks.AnyAsync(b =>
            (b.BlockerId == userId1 && b.BlockedUserId == userId2) ||
            (b.BlockerId == userId2 && b.BlockedUserId == userId1));

    public async Task<IReadOnlyList<UserBlock>> GetBlockedByAsync(Guid userId) =>
        await _db.UserBlocks
            .Where(b => b.BlockerId == userId)
            .ToListAsync();

    public async Task AddAsync(UserBlock block) =>
        await _db.UserBlocks.AddAsync(block);

    public void Remove(UserBlock block) =>
        _db.UserBlocks.Remove(block);
}
