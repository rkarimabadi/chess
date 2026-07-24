using Chess.Application.Common;
using Chess.Application.Common.Authorization;
using Chess.Application.DTOs;
using Chess.Application.Ports;
using Chess.Application.Services;
using Chess.Domain.Entities;

namespace Chess.Application.UseCases.Staff;

public sealed class GetStaffDashboard : UseCaseBase<Guid, DashboardDto>
{
    private readonly IPermissionChecker _permissions;
    private readonly IGameStateManager _stateManager;
    private readonly IMatchmakingService _matchmaking;

    public GetStaffDashboard(IUnitOfWork uow, IPermissionChecker permissions, IGameStateManager stateManager, IMatchmakingService matchmaking) : base(uow)
    {
        _permissions = permissions;
        _stateManager = stateManager;
        _matchmaking = matchmaking;
    }

    public override async Task<DashboardDto> ExecuteAsync(Guid staffId, CancellationToken ct = default)
    {
        if (!_permissions.IsStaff(staffId))
            throw new UnauthorizedAccessException("Staff access required");

        var activeGames = _stateManager.GetActiveCount();
        var onlineUsers = await UoW.Games.GetActivePlayerCountAsync();
        var openReports = await UoW.Reports.GetOpenReportsAsync(1, 1);
        var queueLength = _matchmaking.GetQueueLength();
        var recentBans = await UoW.Sanctions.GetRecentBansCountAsync(7);

        return new DashboardDto
        {
            OnlineUsers = onlineUsers,
            ActiveGames = activeGames,
            QueueLength = queueLength,
            OpenReports = openReports.Count,
            RecentBans = recentBans
        };
    }
}
