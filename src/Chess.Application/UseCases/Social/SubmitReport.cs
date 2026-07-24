using Chess.Application.Common;
using Chess.Application.Common.Authorization;
using Chess.Application.DTOs;
using Chess.Application.Ports;
using Chess.Domain.Entities;
using Chess.Domain.ValueObjects;

namespace Chess.Application.UseCases.Social;

public sealed class SubmitReport : UseCaseBase<SubmitReportRequest, SubmitReportResponse>
{
    private readonly IPermissionChecker _permissions;

    public SubmitReport(IUnitOfWork uow, IPermissionChecker permissions) : base(uow)
    {
        _permissions = permissions;
    }

    public override async Task<SubmitReportResponse> ExecuteAsync(SubmitReportRequest request, CancellationToken ct = default)
    {
        if (!_permissions.IsUser(request.ReporterId))
            throw new UnauthorizedAccessException("User not authorized");

        var reporter = await UoW.Users.GetByIdAsync(request.ReporterId);
        if (reporter is null || reporter.Status != UserStatus.Active)
            throw new InvalidOperationException("Reporter not found or inactive");

        var target = await UoW.Users.GetByIdAsync(request.TargetUserId);
        if (target is null)
            throw new InvalidOperationException("Target user not found");

        if (request.ReporterId == request.TargetUserId)
            throw new InvalidOperationException("Cannot report yourself");

        // Parse reason
        if (!Enum.TryParse<ReportReason>(request.Reason, true, out var reason))
            throw new InvalidOperationException("Invalid report reason");

        var report = PlayerReport.Create(
            request.ReporterId,
            request.TargetUserId,
            reason,
            request.GameId,
            request.Note);

        await UoW.Reports.AddAsync(report);
        await UoW.SaveChangesAsync(ct);

        return new SubmitReportResponse(report.Id);
    }
}
