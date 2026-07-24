namespace Chess.Application.Services;

public interface IMatchmakingService
{
    Task<Guid?> TryMatchAsync(Guid userId, int rating, string timeControl, bool isRated);
    Task CancelAsync(Guid userId);
    int GetQueueLength(string? timeControl = null);
}

public sealed record MatchResult(Guid OpponentUserId, string RoomId)
{
    public static MatchResult Found(Guid opponentUserId, string roomId) =>
        new(opponentUserId, roomId);
}

public sealed record MatchTicket(Guid UserId, int Rating, string TimeControl, bool IsRated, DateTime QueuedAt);
