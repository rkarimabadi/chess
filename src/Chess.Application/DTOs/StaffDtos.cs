namespace Chess.Application.DTOs;

public sealed record DashboardDto
{
    public int OnlineUsers { get; init; }
    public int ActiveGames { get; init; }
    public int QueueLength { get; init; }
    public int OpenReports { get; init; }
    public int RecentBans { get; init; }
}

public sealed record UserDossierDto
{
    public UserDto User { get; init; } = new();
    public List<SanctionDto> Sanctions { get; init; } = new();
    public List<ReportListItemDto> Reports { get; init; } = new();
    public List<GameListItemDto> RecentGames { get; init; } = new();
}

public sealed record UserDto
{
    public Guid Id { get; init; }
    public string Username { get; init; } = string.Empty;
    public string? Email { get; init; }
    public int Rating { get; init; }
    public string Role { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int GamesPlayed { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? LastLoginAt { get; init; }
}

public sealed record AuditLogDto
{
    public Guid Id { get; init; }
    public string ActorUsername { get; init; } = string.Empty;
    public string ActionType { get; init; } = string.Empty;
    public string TargetType { get; init; } = string.Empty;
    public Guid TargetId { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string? DetailsJson { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed record AuditLogFilter(Guid? StaffId, string? ActionType, DateTime? From, DateTime? To, int Page = 1);

public sealed record AssignRoleRequest(Guid UserId, string Role);
public sealed record ForceFinishRequest(string Reason);
public sealed record ListReportsRequest(Guid StaffId, string? Status, int Page);
public sealed record StaffNoteRequest(string Body);
