namespace Chess.Application.DTOs;

public sealed record ApplySanctionRequest(Guid StaffId, Guid UserId, string Type, string Reason, int? DurationDays);
public sealed record ApplySanctionResponse(Guid SanctionId, DateTime? EndsAt);

public sealed record SanctionDto
{
    public Guid Id { get; init; }
    public string Username { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public DateTime StartsAt { get; init; }
    public DateTime? EndsAt { get; init; }
    public bool IsActive { get; init; }
}
