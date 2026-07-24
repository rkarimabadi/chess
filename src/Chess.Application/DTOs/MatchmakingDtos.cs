namespace Chess.Application.DTOs;

public sealed record JoinQueueRequest(Guid UserId, string TimeControl, bool IsRated);
public sealed record JoinQueueResponse(string QueueId, int EstimatedWaitSeconds);
public sealed record MatchFoundDto(string RoomId, Guid OpponentId, string TimeControl);
