using Chess.Domain.ValueObjects;

namespace Chess.Application.Services;

public sealed record ClockState(long WhiteTimeMs, long BlackTimeMs)
{
    public PieceColor? FlaggedSide { get; init; }
}

public interface IClockService
{
    ClockState Tick(LiveGameState state, PieceColor clockedSide, TimeSpan elapsed);
    bool IsFlagged(ClockState clock, PieceColor side);
    long ApplyIncrement(LiveGameState state, PieceColor side);
}
