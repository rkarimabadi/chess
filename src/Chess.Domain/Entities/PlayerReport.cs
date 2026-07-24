using Chess.Domain.Common;
using Chess.Domain.ValueObjects;

namespace Chess.Domain.Entities;

public sealed class PlayerReport : Entity
{
    public Guid ReporterId { get; private set; }
    public Guid TargetUserId { get; private set; }
    public Guid? GameId { get; private set; }
    public ReportReason Reason { get; private set; }
    public string? Note { get; private set; }
    public ReportStatus Status { get; private set; } = ReportStatus.Open;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public Guid? ResolvedByStaffId { get; private set; }
    public string? ResolutionNote { get; private set; }
    public DateTime? ResolvedAt { get; private set; }

    private PlayerReport() { }

    public static PlayerReport Create(Guid reporterId, Guid targetUserId, ReportReason reason, Guid? gameId, string? note)
    {
        return new PlayerReport { Id = Guid.NewGuid(), ReporterId = reporterId, TargetUserId = targetUserId, GameId = gameId, Reason = reason, Note = note };
    }

    public void Resolve(Guid staffId, string? note) { Status = ReportStatus.Resolved; ResolvedByStaffId = staffId; ResolutionNote = note; ResolvedAt = DateTime.UtcNow; }
    public void Reject(Guid staffId, string? note) { Status = ReportStatus.Rejected; ResolvedByStaffId = staffId; ResolutionNote = note; ResolvedAt = DateTime.UtcNow; }
}
