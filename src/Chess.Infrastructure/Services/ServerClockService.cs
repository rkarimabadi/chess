using Chess.Application.Services;
using Chess.Domain.ValueObjects;

namespace Chess.Infrastructure.Services;

public sealed class ServerClockService : IClockService
{
    public ClockState Tick(LiveGameState state, PieceColor clockedSide, TimeSpan elapsed)
    {
        var remainingMs = clockedSide == PieceColor.White
            ? state.WhiteTimeMs : state.BlackTimeMs;

        remainingMs -= (long)elapsed.TotalMilliseconds;

        if (remainingMs <= 0)
        {
            remainingMs = 0;
            return new ClockState(
                clockedSide == PieceColor.White ? remainingMs : state.WhiteTimeMs,
                clockedSide == PieceColor.Black ? remainingMs : state.BlackTimeMs)
            {
                FlaggedSide = clockedSide
            };
        }

        return new ClockState(
            clockedSide == PieceColor.White ? remainingMs : state.WhiteTimeMs,
            clockedSide == PieceColor.Black ? remainingMs : state.BlackTimeMs);
    }

    public bool IsFlagged(ClockState clock, PieceColor side) =>
        side == PieceColor.White ? clock.WhiteTimeMs <= 0 : clock.BlackTimeMs <= 0;

    public long ApplyIncrement(LiveGameState state, PieceColor side) =>
        state.Variant == "Untimed" ? 0 : side == PieceColor.White ? state.WhiteTimeMs + state.IncrementMs : state.BlackTimeMs + state.IncrementMs;
}
