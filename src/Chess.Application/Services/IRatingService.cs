using Chess.Domain.ValueObjects;

namespace Chess.Application.Services;

public sealed record RatingResult
{
    public int WhiteOldRating { get; init; }
    public int WhiteNewRating { get; init; }
    public int WhiteDelta { get; init; }
    public int BlackOldRating { get; init; }
    public int BlackNewRating { get; init; }
    public int BlackDelta { get; init; }
    public int K { get; init; }

    public static RatingResult NoChange => new();
}

public interface IRatingService
{
    RatingResult Calculate(int whiteRating, int blackRating, GameResult result, bool isRated);
}
