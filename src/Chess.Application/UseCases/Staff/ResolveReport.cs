using Chess.Application.Common;
using Chess.Application.Common.Authorization;
using Chess.Application.DTOs;
using Chess.Application.Ports;
using Chess.Domain.Entities;
using Chess.Domain.ValueObjects;

namespace Chess.Application.UseCases.Staff;

public sealed class ResolveReport : UseCaseBase<(Guid StaffId, Guid ReportId, ResolveReportRequest Request), SuccessResponse>
{
    private readonly IPermissionChecker _permissions;

    public ResolveReport(IUnitOfWork uow, IPermissionChecker permissions) : base(uow)
    {
        _permissions = permissions;
    }

    public override async Task<SuccessResponse> ExecuteAsync((Guid StaffId, Guid ReportId, ResolveReportRequest Request) request, CancellationToken ct = default)
    {
        if (!_permissions.IsStaff(request.StaffId))
            throw new UnauthorizedAccessException("Staff access required");

        var report = await UoW.Reports.GetByIdAsync(request.ReportId);
        if (report is null)
            throw new InvalidOperationException("Report not found");

        if (report.Status != ReportStatus.Open && report.Status != ReportStatus.InReview)
            throw new InvalidOperationException("Report cannot be resolved");

        // Parse action
        if (!Enum.TryParse<ReportStatus>(request.Request.Action, true, out var status))
            throw new InvalidOperationException("Invalid action");

        report.Resolve(request.StaffId, request.Request.Note);
        if (status == ReportStatus.Rejected)
            report.Reject(request.StaffId, request.Request.Note);

        UoW.Reports.Update(report);

        // Log audit
        var auditLog = StaffAuditLog.Create(
            request.StaffId,
            "ResolveReport",
            "Report",
            request.ReportId,
            request.Request.Note ?? "Report resolved");

        await UoW.Audit.AddAsync(auditLog);
        await UoW.SaveChangesAsync(ct);

        return new SuccessResponse(true);
    }
}
