using Chess.Application.DTOs;

namespace Chess.Web.Hubs;

public interface IStaffHub
{
    Task DashboardUpdated(DashboardDto stats);
    Task NewReportSubmitted(ReportDto report);
    Task GameAborted(Guid gameId);
    Task UserBanned(Guid userId, string reason);
    Task JoinStaffGroup();
    Task SubscribeDashboard();
    Task SubscribeReportQueue();
}
