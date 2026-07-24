using Chess.Application.Ports;
using Chess.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Chess.Infrastructure.Services;

public sealed class SecurityAuditLogger
{
    private readonly IAuditRepository _auditRepository;
    private readonly ILogger<SecurityAuditLogger> _logger;

    public SecurityAuditLogger(IAuditRepository auditRepository, ILogger<SecurityAuditLogger> logger)
    {
        _auditRepository = auditRepository;
        _logger = logger;
    }

    public async Task LogAsync(Guid actorStaffId, string actionType, string targetType, Guid targetId, string reason, string? detailsJson = null)
    {
        var log = StaffAuditLog.Create(actorStaffId, actionType, targetType, targetId, reason, detailsJson);
        await _auditRepository.AddAsync(log);

        _logger.LogInformation(
            "Staff audit: Actor={ActorId}, Action={Action}, Target={TargetType}:{TargetId}, Reason={Reason}",
            actorStaffId, actionType, targetType, targetId, reason);
    }

    public async Task LogBanAsync(Guid staffId, Guid userId, string reason)
    {
        await LogAsync(staffId, "Ban", "User", userId, reason);
    }

    public async Task LogUnbanAsync(Guid staffId, Guid userId, string reason)
    {
        await LogAsync(staffId, "Unban", "User", userId, reason);
    }

    public async Task LogRoleChangeAsync(Guid staffId, Guid userId, string reason)
    {
        await LogAsync(staffId, "AssignRole", "User", userId, reason);
    }

    public async Task LogReportResolutionAsync(Guid staffId, Guid reportId, string reason)
    {
        await LogAsync(staffId, "ResolveReport", "Report", reportId, reason);
    }

    public async Task LogForceFinishAsync(Guid staffId, Guid gameId, string reason)
    {
        await LogAsync(staffId, "ForceFinish", "Game", gameId, reason);
    }
}
