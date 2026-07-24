using Chess.Application.Ports;
using Chess.Domain.Entities;
using Chess.Domain.ValueObjects;
using Chess.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Chess.Infrastructure.Repositories;

public sealed class RoomRepository : IRoomRepository
{
    private readonly ChessDbContext _db;

    public RoomRepository(ChessDbContext db) => _db = db;

    public async Task<Room?> GetByIdAsync(Guid id) =>
        await _db.Rooms.FindAsync(id);

    public async Task<IReadOnlyList<Room>> GetOpenRoomsAsync(int page, int pageSize) =>
        await _db.Rooms
            .Where(r => r.Status == RoomStatus.Waiting && r.ExpiresAt > DateTime.UtcNow && r.GuestId == null)
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

    public async Task AddAsync(Room room) =>
        await _db.Rooms.AddAsync(room);

    public async Task<int> CleanupExpiredAsync()
    {
        var now = DateTime.UtcNow;
        var expired = await _db.Rooms
            .Where(r => r.ExpiresAt != null && r.ExpiresAt < now)
            .ToListAsync();

        _db.Rooms.RemoveRange(expired);
        return await _db.SaveChangesAsync();
    }

    public void Update(Room room) =>
        _db.Rooms.Update(room);
}
