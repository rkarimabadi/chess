using Chess.Application.Common;
using Chess.Application.Common.Authorization;
using Chess.Application.DTOs;
using Chess.Application.Ports;
using Chess.Domain.Entities;
using Chess.Domain.ValueObjects;

namespace Chess.Application.UseCases.Game;

public sealed class CreateRoom : UseCaseBase<CreateRoomRequest, CreateRoomResponse>
{
    private readonly IPermissionChecker _permissions;

    public CreateRoom(IUnitOfWork uow, IPermissionChecker permissions) : base(uow)
    {
        _permissions = permissions;
    }

    public override async Task<CreateRoomResponse> ExecuteAsync(CreateRoomRequest request, CancellationToken ct = default)
    {
        if (!_permissions.IsUser(request.UserId))
            throw new UnauthorizedAccessException("User not authorized");

        var user = await UoW.Users.GetByIdAsync(request.UserId);
        if (user is null || user.Status != UserStatus.Active)
            throw new InvalidOperationException("User not found or inactive");

        var timeControl = ParseTimeControl(request.TimeControl);
        var room = Room.Create(request.UserId, timeControl.Base, timeControl.Inc, request.IsRated);

        await UoW.Rooms.AddAsync(room);
        await UoW.SaveChangesAsync(ct);

        return new CreateRoomResponse(room.Id);
    }

    private static (int Base, int Inc) ParseTimeControl(string timeControl)
    {
        if (timeControl.Contains('+'))
        {
            var parts = timeControl.Split('+');
            return (int.Parse(parts[0]), int.Parse(parts[1]));
        }
        return timeControl.ToLowerInvariant() switch
        {
            "bullet" => (60, 0),
            "blitz" => (180, 2),
            "rapid" => (600, 5),
            "classic" => (3600, 0),
            "untimed" => (0, 0),
            _ => (300, 0)
        };
    }
}
