using System.Security.Claims;
using Chess.Application.Ports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Chess.Web.Hubs;

[Authorize(Policy = "StaffOnly")]
public class StaffHub : Hub<IStaffHub>
{
    private readonly IUnitOfWork _uow;

    public StaffHub(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "staff");
        await base.OnConnectedAsync();
    }

    public async Task JoinStaffGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "staff");
    }

    public async Task SubscribeDashboard()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "staff-dashboard");
    }

    public async Task SubscribeReportQueue()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "staff-reports");
    }
}
