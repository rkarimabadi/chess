namespace Chess.Application.DTOs;

public sealed record SubmitReportRequest(Guid ReporterId, Guid TargetUserId, string Reason, Guid? GameId, string? Note);
public sealed record SubmitReportResponse(Guid ReportId);

public sealed record ReportDto
{
    public Guid Id { get; init; }
    public string ReporterUsername { get; init; } = string.Empty;
    public string TargetUsername { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public string? Note { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public Guid? GameId { get; init; }
}

public sealed record ReportListItemDto
{
    public Guid Id { get; init; }
    public string TargetUsername { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

public sealed record ResolveReportRequest(string Action, string Note);
