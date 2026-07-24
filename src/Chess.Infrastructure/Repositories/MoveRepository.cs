using Chess.Application.Ports;
using Chess.Domain.Entities;
using Chess.Domain.ValueObjects;
using Chess.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Chess.Infrastructure.Repositories;

public sealed class MoveRepository : IMoveRepository
{
    private readonly ChessDbContext _db;

    public MoveRepository(ChessDbContext db) => _db = db;

    public async Task<IReadOnlyList<MoveRecord>> GetByGameIdAsync(Guid gameId) =>
        await _db.MoveRecords
            .Where(m => m.GameId == gameId)
            .OrderBy(m => m.MoveNumber)
            .ToListAsync();

    public async Task AddAsync(MoveRecord move) =>
        await _db.MoveRecords.AddAsync(move);
}
