using Chess.Application.Ports;
using Chess.Domain.Entities;
using Chess.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Chess.Infrastructure.Repositories;

public sealed class RatingRepository : IRatingRepository
{
    private readonly ChessDbContext _db;

    public RatingRepository(ChessDbContext db) => _db = db;

    public async Task<IReadOnlyList<RatingChange>> GetByPlayerIdAsync(Guid playerId, int page, int pageSize) =>
        await _db.RatingChanges
            .Where(r => r.PlayerId == playerId)
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

    public async Task AddAsync(RatingChange ratingChange) =>
        await _db.RatingChanges.AddAsync(ratingChange);
}
