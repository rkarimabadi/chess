using Chess.Domain.Common;

namespace Chess.Domain.Entities;

public sealed class StaffNote : Entity
{
    public Guid UserId { get; private set; }
    public Guid AuthorStaffId { get; private set; }
    public string Body { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    private StaffNote() { }

    public static StaffNote Create(Guid userId, Guid staffId, string body)
    {
        return new StaffNote { Id = Guid.NewGuid(), UserId = userId, AuthorStaffId = staffId, Body = body };
    }
}
