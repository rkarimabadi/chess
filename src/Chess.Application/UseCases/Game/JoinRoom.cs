using Chess.Application.Common;
using Chess.Application.Common.Authorization;
using Chess.Application.DTOs;
using Chess.Application.Ports;
using Chess.Domain.Entities;
using Chess.Domain.ValueObjects;

namespace Chess.Application.UseCases.Game;

public sealed class JoinRoom : UseCaseBase<JoinRoomRequest, JoinRoomResponse>
{
    private readonly IPermissionChecker _permissions;

    public JoinRoom(IUnitOfWork uow, IPermissionChecker permissions) : base(uow)
    {
        _permissions = permissions;
    }

    public override async Task<JoinRoomResponse> ExecuteAsync(JoinRoomRequest request, CancellationToken ct = default)
    {
        if (!_permissions.IsUser(request.UserId))
            throw new UnauthorizedAccessException("User not authorized");

        var user = await UoW.Users.GetByIdAsync(request.UserId);
        if (user is null || user.Status != UserStatus.Active)
            throw new InvalidOperationException("User not found or inactive");

        var room = await UoW.Rooms.GetByIdAsync(request.RoomId);
        if (room is null)
            throw new InvalidOperationException("Room not found");

        if (room.Status != RoomStatus.Waiting)
            throw new InvalidOperationException("Room is not available");

        if (room.HostId == request.UserId)
            throw new InvalidOperationException("Cannot join your own room");

        // Check if blocked
        if (await UoW.UserBlocks.IsBlockedAsync(room.HostId, request.UserId))
            throw new InvalidOperationException("Cannot join this room");

        room.Join(request.UserId);
        UoW.Rooms.Update(room);
        await UoW.SaveChangesAsync(ct);

        var host = await UoW.Users.GetByIdAsync(room.HostId);
        return new JoinRoomResponse(room.Id, host?.Username ?? "Unknown");
    }
}
