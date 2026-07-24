using Chess.Application.Ports;
using Chess.Domain.Entities;
using Chess.Domain.ValueObjects;
using Chess.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Chess.Infrastructure.Repositories;

public sealed class GameRepository : IGameRepository
{
    private readonly ChessDbContext _db;

    public GameRepository(ChessDbContext db) => _db = db;

    public async Task<Game?> GetByIdAsync(Guid id) =>
        await _db.Games.FindAsync(id);

    public async Task AddAsync(Game game) =>
        await _db.Games.AddAsync(game);

    public async Task<IReadOnlyList<Game>> GetUserHistoryAsync(Guid userId, int page, int pageSize) =>
        await _db.Games
            .Where(g => (g.WhitePlayerId == userId || g.BlackPlayerId == userId) && g.Status == GameStatus.Finished)
            .OrderByDescending(g => g.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

    public async Task<int> GetUserHistoryCountAsync(Guid userId) =>
        await _db.Games
            .CountAsync(g => (g.WhitePlayerId == userId || g.BlackPlayerId == userId) && g.Status == GameStatus.Finished);

    public async Task<IReadOnlyList<Game>> GetActiveGamesAsync() =>
        await _db.Games
            .Where(g => g.Status == GameStatus.Active)
            .ToListAsync();

    public async Task<IReadOnlyList<Game>> GetUserActiveGamesAsync(Guid userId) =>
        await _db.Games
            .Where(g => g.Status == GameStatus.Active && (g.WhitePlayerId == userId || g.BlackPlayerId == userId))
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();

    public async Task<int> GetActivePlayerCountAsync() =>
        await _db.Games
            .Where(g => g.Status == GameStatus.Active)
            .SelectMany(g => new[] { g.WhitePlayerId, g.BlackPlayerId })
            .Distinct()
            .CountAsync();

    public async Task<IReadOnlyList<Game>> GetSpectatableGamesAsync(int page, int pageSize) =>
        await _db.Games
            .Where(g => g.Status == GameStatus.Active && g.IsRated)
            .OrderByDescending(g => g.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

    public void Update(Game game) =>
        _db.Games.Update(game);
}
