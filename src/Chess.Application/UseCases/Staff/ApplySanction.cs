using Chess.Application.Common;
using Chess.Application.Common.Authorization;
using Chess.Application.DTOs;
using Chess.Application.Ports;
using Chess.Domain.Entities;
using Chess.Domain.ValueObjects;

namespace Chess.Application.UseCases.Staff;

public sealed class ApplySanction : UseCaseBase<ApplySanctionRequest, ApplySanctionResponse>
{
    private readonly IPermissionChecker _permissions;

    public ApplySanction(IUnitOfWork uow, IPermissionChecker permissions) : base(uow)
    {
        _permissions = permissions;
    }

    public override async Task<ApplySanctionResponse> ExecuteAsync(ApplySanctionRequest request, CancellationToken ct = default)
    {
        if (!_permissions.CanBan(request.StaffId))
            throw new UnauthorizedAccessException("Ban permission required");

        if (request.StaffId == request.UserId)
            throw new InvalidOperationException("Cannot sanction yourself");

        var user = await UoW.Users.GetByIdAsync(request.UserId);
        if (user is null)
            throw new InvalidOperationException("User not found");

        // Check moderator limits
        if (!_permissions.IsAdmin(request.StaffId))
        {
            // Moderators cannot issue perm bans (ARCH-07)
            if (request.Type.Equals("PermBan", StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("Only admins can issue permanent bans");

            // Moderators cannot ban for more than 30 days (DEC-28)
            if (request.DurationDays > 30)
                throw new InvalidOperationException("Moderators cannot ban for more than 30 days");
        }

        // Parse sanction type
        if (!Enum.TryParse<SanctionType>(request.Type, true, out var sanctionType))
            throw new InvalidOperationException("Invalid sanction type");

        var sanction = UserSanction.Create(
            request.UserId,
            sanctionType,
            request.Reason,
            request.StaffId,
            request.DurationDays);

        // Apply side effects
        switch (sanctionType)
        {
            case SanctionType.PermBan:
            case SanctionType.TempBan:
                user.Ban();
                break;
            case SanctionType.MutePresets:
                user.MutePresetsUntil(sanction.EndsAt ?? DateTime.UtcNow.AddDays(7));
                break;
        }

        UoW.Users.Update(user);
        await UoW.Sanctions.AddAsync(sanction);

        // Log audit
        var auditLog = StaffAuditLog.Create(
            request.StaffId,
            $"Apply{request.Type}",
            "User",
            request.UserId,
            request.Reason);

        await UoW.Audit.AddAsync(auditLog);
        await UoW.SaveChangesAsync(ct);

        return new ApplySanctionResponse(sanction.Id, sanction.EndsAt);
    }
}
