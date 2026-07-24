using Chess.Domain.Common;

namespace Chess.Domain.Entities;

public sealed class UserBlock : Entity
{
    public Guid BlockerId { get; private set; }
    public Guid BlockedUserId { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    private UserBlock() { }

    public static UserBlock Create(Guid blockerId, Guid blockedUserId)
    {
        return new UserBlock { Id = Guid.NewGuid(), BlockerId = blockerId, BlockedUserId = blockedUserId };
    }
}
