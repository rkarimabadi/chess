using Chess.Domain.Common;

namespace Chess.Domain.Entities;

public sealed class StaffAuditLog : Entity
{
    public Guid ActorStaffId { get; private set; }
    public string ActionType { get; private set; } = string.Empty;
    public string TargetType { get; private set; } = string.Empty;
    public Guid TargetId { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public string? DetailsJson { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    private StaffAuditLog() { }

    public static StaffAuditLog Create(Guid staffId, string action, string targetType, Guid targetId, string reason, string? details = null)
    {
        return new StaffAuditLog { Id = Guid.NewGuid(), ActorStaffId = staffId, ActionType = action, TargetType = targetType, TargetId = targetId, Reason = reason, DetailsJson = details };
    }
}
