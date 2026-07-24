using Chess.Application.Ports;
using Chess.Domain.Entities;
using Chess.Domain.ValueObjects;
using Chess.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Chess.Infrastructure.Repositories;

public sealed class FriendshipRepository : IFriendshipRepository
{
    private readonly ChessDbContext _db;

    public FriendshipRepository(ChessDbContext db) => _db = db;

    public async Task<Friendship?> GetByIdAsync(Guid id) =>
        await _db.Friendships.FindAsync(id);

    public async Task<Friendship?> GetBetweenAsync(Guid requesterId, Guid addresseeId) =>
        await _db.Friendships.FirstOrDefaultAsync(f =>
            (f.RequesterId == requesterId && f.AddresseeId == addresseeId) ||
            (f.RequesterId == addresseeId && f.AddresseeId == requesterId));

    public async Task<IReadOnlyList<Friendship>> GetFriendsOfAsync(Guid userId) =>
        await _db.Friendships
            .Where(f => (f.RequesterId == userId || f.AddresseeId == userId) && f.Status == FriendshipStatus.Accepted)
            .ToListAsync();

    public async Task<IReadOnlyList<Friendship>> GetPendingRequestsForAsync(Guid userId) =>
        await _db.Friendships
            .Where(f => f.AddresseeId == userId && f.Status == FriendshipStatus.Pending)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();

    public async Task AddAsync(Friendship friendship) =>
        await _db.Friendships.AddAsync(friendship);

    public void Update(Friendship friendship) =>
        _db.Friendships.Update(friendship);
}
