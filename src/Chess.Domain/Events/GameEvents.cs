using Chess.Domain.Common;
using Chess.Domain.ValueObjects;

namespace Chess.Domain.Events;

public record GameCreatedEvent(Guid GameId, Guid WhiteId, Guid BlackId, bool IsRated) : IDomainEvent
{
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
public record GameStartedEvent(Guid GameId) : IDomainEvent
{
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
public record MoveAcceptedEvent(Guid GameId, MoveRecord Move, PieceColor Turn) : IDomainEvent
{
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
public record MoveRejectedEvent(Guid GameId, string Reason) : IDomainEvent
{
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
public record CheckDetectedEvent(Guid GameId, PieceColor CheckedSide) : IDomainEvent
{
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
public record CheckmateEvent(Guid GameId, PieceColor Winner) : IDomainEvent
{
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
public record StalemateEvent(Guid GameId) : IDomainEvent
{
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
public record GameFinishedEvent(Guid GameId, GameResult Result, ResultReason Reason, int? WhiteRatingDelta, int? BlackRatingDelta) : IDomainEvent
{
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
public record ClockFlaggedEvent(Guid GameId, PieceColor FlaggedSide) : IDomainEvent
{
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
public record DrawOfferedEvent(Guid GameId, Guid OfferedById) : IDomainEvent
{
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
public record DrawRespondedEvent(Guid GameId, bool Accepted, Guid RespondedById) : IDomainEvent
{
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
public record PlayerResignedEvent(Guid GameId, Guid ResignedById) : IDomainEvent
{
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
public record PlayerDisconnectedEvent(Guid GameId, PieceColor Side) : IDomainEvent
{
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
public record PlayerReconnectedEvent(Guid GameId, PieceColor Side) : IDomainEvent
{
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
public record PresetMessageSentEvent(Guid GameId, Guid SenderId, string MessageKey) : IDomainEvent
{
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
public record RematchOfferedEvent(Guid GameId, Guid OfferedById) : IDomainEvent
{
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
public record RematchAcceptedEvent(Guid OldGameId, Guid NewGameId) : IDomainEvent
{
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
