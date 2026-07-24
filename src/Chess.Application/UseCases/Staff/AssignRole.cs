using Chess.Application.Common;
using Chess.Application.Common.Authorization;
using Chess.Application.DTOs;
using Chess.Application.Ports;
using Chess.Domain.Entities;
using Chess.Domain.ValueObjects;

namespace Chess.Application.UseCases.Staff;

public sealed class AssignRole : UseCaseBase<(Guid AdminId, AssignRoleRequest Request), SuccessResponse>
{
    private readonly IPermissionChecker _permissions;

    public AssignRole(IUnitOfWork uow, IPermissionChecker permissions) : base(uow)
    {
        _permissions = permissions;
    }

    public override async Task<SuccessResponse> ExecuteAsync((Guid AdminId, AssignRoleRequest Request) request, CancellationToken ct = default)
    {
        if (!_permissions.CanManageRoles(request.AdminId))
            throw new UnauthorizedAccessException("Role management permission required");

        if (request.AdminId == request.Request.UserId)
            throw new InvalidOperationException("Cannot change your own role");

        var user = await UoW.Users.GetByIdAsync(request.Request.UserId);
        if (user is null)
            throw new InvalidOperationException("User not found");

        // Parse role
        if (!Enum.TryParse<UserRole>(request.Request.Role, true, out var role))
            throw new InvalidOperationException("Invalid role");

        var oldRole = user.Role;
        user.SetRole(role);
        UoW.Users.Update(user);

        // Log audit
        var auditLog = StaffAuditLog.Create(
            request.AdminId,
            "AssignRole",
            "User",
            request.Request.UserId,
            $"Role changed from {oldRole} to {role}");

        await UoW.Audit.AddAsync(auditLog);
        await UoW.SaveChangesAsync(ct);

        return new SuccessResponse(true);
    }
}
