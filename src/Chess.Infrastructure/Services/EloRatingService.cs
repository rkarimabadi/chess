using Chess.Application.Services;
using Chess.Domain.ValueObjects;

namespace Chess.Infrastructure.Services;

public sealed class EloRatingService : IRatingService
{
    private const int DefaultK = 20;
    private const int StartRating = 1200;

    public RatingResult Calculate(int whiteRating, int blackRating, GameResult result, bool isRated)
    {
        if (!isRated) return RatingResult.NoChange;

        var (whiteScore, blackScore) = result switch
        {
            GameResult.WhiteWins => (1.0, 0.0),
            GameResult.BlackWins => (0.0, 1.0),
            GameResult.Draw => (0.5, 0.5),
            _ => (0.0, 0.0)
        };

        var expectedWhite = 1.0 / (1.0 + Math.Pow(10, (blackRating - whiteRating) / 400.0));
        var expectedBlack = 1.0 - expectedWhite;

        var whiteDelta = (int)Math.Round(DefaultK * (whiteScore - expectedWhite));
        var blackDelta = -whiteDelta;

        return new RatingResult
        {
            WhiteOldRating = whiteRating,
            WhiteNewRating = whiteRating + whiteDelta,
            WhiteDelta = whiteDelta,
            BlackOldRating = blackRating,
            BlackNewRating = blackRating + blackDelta,
            BlackDelta = blackDelta,
            K = DefaultK
        };
    }
}
