using System.Collections.Concurrent;
using Chess.Application.Services;
using Chess.Domain.ValueObjects;

namespace Chess.Infrastructure.Services;

public sealed class IdleAbandonTimer : IIdleAbandonTimer
{
    private readonly ConcurrentDictionary<Guid, DateTime> _lastMoveTimes = new();
    private readonly TimeSpan _idleLimit = TimeSpan.FromMinutes(5);

    public void ResetTimer(Guid gameId, PieceColor sideToMove)
    {
        _lastMoveTimes.AddOrUpdate(gameId, DateTime.UtcNow, (_, _) => DateTime.UtcNow);
    }

    public Task<bool> HasExceededIdleLimitAsync(Guid gameId)
    {
        return Task.FromResult(HasExceededIdleLimit(gameId));
    }

    public bool HasExceededIdleLimit(Guid gameId)
    {
        if (!_lastMoveTimes.TryGetValue(gameId, out var lastMove))
            return false;

        return DateTime.UtcNow - lastMove > _idleLimit;
    }

    public void RemoveGame(Guid gameId)
    {
        _lastMoveTimes.TryRemove(gameId, out _);
    }

    public TimeSpan GetIdleTime(Guid gameId)
    {
        if (!_lastMoveTimes.TryGetValue(gameId, out var lastMove))
            return TimeSpan.Zero;

        return DateTime.UtcNow - lastMove;
    }
}
