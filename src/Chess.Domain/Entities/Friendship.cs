using Chess.Domain.Common;
using Chess.Domain.ValueObjects;

namespace Chess.Domain.Entities;

public sealed class Friendship : Entity
{
    public Guid RequesterId { get; private set; }
    public Guid AddresseeId { get; private set; }
    public FriendshipStatus Status { get; private set; } = FriendshipStatus.Pending;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? RespondedAt { get; private set; }

    private Friendship() { }

    public static Friendship Create(Guid requesterId, Guid addresseeId)
    {
        return new Friendship { Id = Guid.NewGuid(), RequesterId = requesterId, AddresseeId = addresseeId };
    }

    public void Accept() { Status = FriendshipStatus.Accepted; RespondedAt = DateTime.UtcNow; }
    public void Decline() { Status = FriendshipStatus.Declined; RespondedAt = DateTime.UtcNow; }
}
