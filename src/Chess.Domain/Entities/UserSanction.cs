using Chess.Domain.Common;
using Chess.Domain.ValueObjects;

namespace Chess.Domain.Entities;

public sealed class UserSanction : Entity
{
    public Guid UserId { get; private set; }
    public SanctionType Type { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public Guid CreatedByStaffId { get; private set; }
    public DateTime StartsAt { get; private set; }
    public DateTime? EndsAt { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public bool IsExpired => EndsAt.HasValue && DateTime.UtcNow > EndsAt.Value;

    private UserSanction() { }

    public static UserSanction Create(Guid userId, SanctionType type, string reason, Guid staffId, int? durationDays = null)
    {
        return new UserSanction { Id = Guid.NewGuid(), UserId = userId, Type = type, Reason = reason, CreatedByStaffId = staffId, StartsAt = DateTime.UtcNow, EndsAt = durationDays.HasValue ? DateTime.UtcNow.AddDays(durationDays.Value) : null };
    }

    public void Deactivate() => IsActive = false;
}
