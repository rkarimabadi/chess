using Chess.Application.Common;
using Chess.Application.Common.Authorization;
using Chess.Application.DTOs;
using Chess.Application.Ports;
using Chess.Domain.Entities;
using Chess.Domain.ValueObjects;

namespace Chess.Application.UseCases.Staff;

public sealed class ListReports : UseCaseBase<ListReportsRequest, PagedResult<ReportListItemDto>>
{
    private readonly IPermissionChecker _permissions;
    private const int PageSize = 20;

    public ListReports(IUnitOfWork uow, IPermissionChecker permissions) : base(uow)
    {
        _permissions = permissions;
    }

    public override async Task<PagedResult<ReportListItemDto>> ExecuteAsync(ListReportsRequest request, CancellationToken ct = default)
    {
        if (!_permissions.IsStaff(request.StaffId))
            throw new UnauthorizedAccessException("Staff access required");

        IReadOnlyList<PlayerReport> reports;
        if (string.IsNullOrEmpty(request.Status))
            reports = await UoW.Reports.GetOpenReportsAsync(request.Page, PageSize);
        else if (Enum.TryParse<ReportStatus>(request.Status, true, out var statusFilter))
            reports = await UoW.Reports.GetByStatusAsync(statusFilter, request.Page, PageSize);
        else
            reports = await UoW.Reports.GetOpenReportsAsync(request.Page, PageSize);

        var items = new List<ReportListItemDto>();
        foreach (var report in reports)
        {
            var target = await UoW.Users.GetByIdAsync(report.TargetUserId);
            items.Add(new ReportListItemDto
            {
                Id = report.Id,
                TargetUsername = target?.Username ?? "Unknown",
                Reason = report.Reason.ToString(),
                Status = report.Status.ToString(),
                CreatedAt = report.CreatedAt
            });
        }

        return new PagedResult<ReportListItemDto>(items, items.Count, request.Page, PageSize);
    }
}
