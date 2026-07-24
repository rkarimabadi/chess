using Chess.Domain.Common;

namespace Chess.Domain.Entities;

public sealed class RatingChange : Entity
{
    public Guid PlayerId { get; private set; }
    public Guid GameId { get; private set; }
    public int OldRating { get; private set; }
    public int NewRating { get; private set; }
    public int K { get; private set; }
    public int Delta => NewRating - OldRating;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    private RatingChange() { }

    public static RatingChange Create(Guid playerId, Guid gameId, int oldRating, int newRating, int k = 20)
    {
        return new RatingChange { Id = Guid.NewGuid(), PlayerId = playerId, GameId = gameId, OldRating = oldRating, NewRating = newRating, K = k };
    }
}
