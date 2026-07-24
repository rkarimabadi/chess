using Chess.Domain.ValueObjects;

namespace Chess.Application.Services;

public interface IIdleAbandonTimer
{
    void ResetTimer(Guid gameId, PieceColor sideToMove);
    Task<bool> HasExceededIdleLimitAsync(Guid gameId);
}
