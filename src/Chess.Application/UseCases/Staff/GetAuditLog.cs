using Chess.Application.Common;
using Chess.Application.Common.Authorization;
using Chess.Application.DTOs;
using Chess.Application.Ports;
using Chess.Domain.Entities;

namespace Chess.Application.UseCases.Staff;

public sealed class GetAuditLog : UseCaseBase<(Guid AdminId, AuditLogFilter Filter), PagedResult<AuditLogDto>>
{
    private readonly IPermissionChecker _permissions;
    private const int PageSize = 50;

    public GetAuditLog(IUnitOfWork uow, IPermissionChecker permissions) : base(uow)
    {
        _permissions = permissions;
    }

    public override async Task<PagedResult<AuditLogDto>> ExecuteAsync((Guid AdminId, AuditLogFilter Filter) request, CancellationToken ct = default)
    {
        if (!_permissions.CanViewFullAudit(request.AdminId))
            throw new UnauthorizedAccessException("Full audit access required");

        var logs = await UoW.Audit.GetFilteredAsync(
            request.Filter.StaffId,
            request.Filter.ActionType,
            request.Filter.From,
            request.Filter.To ?? DateTime.UtcNow,
            request.Filter.Page,
            PageSize);

        var items = new List<AuditLogDto>();
        foreach (var log in logs)
        {
            var actor = await UoW.Users.GetByIdAsync(log.ActorStaffId);
            items.Add(new AuditLogDto
            {
                Id = log.Id,
                ActorUsername = actor?.Username ?? "Unknown",
                ActionType = log.ActionType,
                TargetType = log.TargetType,
                TargetId = log.TargetId,
                Reason = log.Reason,
                DetailsJson = log.DetailsJson,
                CreatedAt = log.CreatedAt
            });
        }

        return new PagedResult<AuditLogDto>(items, items.Count, request.Filter.Page, PageSize);
    }
}
