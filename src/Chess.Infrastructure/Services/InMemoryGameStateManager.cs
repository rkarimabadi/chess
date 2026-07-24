using System.Collections.Concurrent;
using System.Text.Json;
using Chess.Application.Services;
using Chess.Domain.Entities;
using Chess.Domain.ValueObjects;
using Chess.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Chess.Infrastructure.Services;

public sealed class InMemoryGameStateManager : IGameStateManager
{
    private readonly ConcurrentDictionary<Guid, LiveGameState> _states = new();
    private readonly IServiceScopeFactory _scopeFactory;

    public InMemoryGameStateManager(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public Task<LiveGameState?> GetAsync(Guid gameId)
    {
        _states.TryGetValue(gameId, out var state);
        return Task.FromResult(state);
    }

    public Task UpsertAsync(Guid gameId, LiveGameState state)
    {
        state.GameId = gameId;
        _states.AddOrUpdate(gameId, state, (_, _) => state);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(Guid gameId)
    {
        _states.TryRemove(gameId, out _);
        return Task.CompletedTask;
    }

    public IEnumerable<LiveGameState> GetAllActive() => _states.Values;

    public int GetActiveCount() => _states.Count;

    public async Task<int> SnapshotAllActiveAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ChessDbContext>();

        foreach (var (gameId, state) in _states)
        {
            var game = await db.Games.FindAsync(gameId);
            if (game is null || game.Status != GameStatus.Active) continue;

            game.SetFen(state.Board.ToFen());
            game.SetHalfmove(state.HalfmoveClock);
            game.SetFullmove(state.FullmoveNumber);
            game.SetTime(state.WhiteTimeMs, state.BlackTimeMs);
            game.ClearDrawOffer();
            game.WhiteConnected = state.WhiteConnected;
            game.BlackConnected = state.BlackConnected;
            game.WhiteDisconnectedAt = state.WhiteDisconnectedAt;
            game.BlackDisconnectedAt = state.BlackDisconnectedAt;
            game.SetPositionHistory(JsonSerializer.Serialize(state.PositionHistory));
        }

        return await db.SaveChangesAsync();
    }
}
