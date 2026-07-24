using Chess.Application.Common;
using Chess.Application.Common.Authorization;
using Chess.Application.DTOs;
using Chess.Application.Ports;
using Chess.Domain.Entities;
using Chess.Domain.ValueObjects;

namespace Chess.Application.UseCases.Staff;

public sealed class RemoveSanction : UseCaseBase<(Guid StaffId, Guid SanctionId), SuccessResponse>
{
    private readonly IPermissionChecker _permissions;

    public RemoveSanction(IUnitOfWork uow, IPermissionChecker permissions) : base(uow)
    {
        _permissions = permissions;
    }

    public override async Task<SuccessResponse> ExecuteAsync((Guid StaffId, Guid SanctionId) request, CancellationToken ct = default)
    {
        if (!_permissions.IsStaff(request.StaffId))
            throw new UnauthorizedAccessException("Staff access required");

        var sanction = await UoW.Sanctions.GetByIdAsync(request.SanctionId);
        if (sanction is null)
            throw new InvalidOperationException("Sanction not found");

        if (!sanction.IsActive)
            throw new InvalidOperationException("Sanction is already inactive");

        sanction.Deactivate();
        UoW.Sanctions.Update(sanction);

        // Unban user if this was a ban
        if (sanction.Type is SanctionType.TempBan or SanctionType.PermBan)
        {
            var user = await UoW.Users.GetByIdAsync(sanction.UserId);
            if (user is not null)
            {
                user.Unban();
                UoW.Users.Update(user);
            }
        }

        // Log audit
        var auditLog = StaffAuditLog.Create(
            request.StaffId,
            "RemoveSanction",
            "Sanction",
            request.SanctionId,
            "Sanction removed");

        await UoW.Audit.AddAsync(auditLog);
        await UoW.SaveChangesAsync(ct);

        return new SuccessResponse(true);
    }
}
