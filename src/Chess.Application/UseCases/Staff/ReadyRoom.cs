using Chess.Application.Common;
using Chess.Application.DTOs;
using Chess.Application.Ports;
using Chess.Domain.Entities;
using Chess.Domain.ValueObjects;

namespace Chess.Application.UseCases.Staff;

public sealed class ReadyRoom : UseCaseBase<(Guid UserId, Guid RoomId), SuccessResponse>
{
    public ReadyRoom(IUnitOfWork uow) : base(uow) { }

    public override async Task<SuccessResponse> ExecuteAsync((Guid UserId, Guid RoomId) request, CancellationToken ct = default)
    {
        var room = await UoW.Rooms.GetByIdAsync(request.RoomId);
        if (room is null)
            throw new InvalidOperationException("Room not found");

        if (room.Status != RoomStatus.Waiting)
            throw new InvalidOperationException("Room is not in waiting state");

        if (room.HostId != request.UserId && room.GuestId != request.UserId)
            throw new UnauthorizedAccessException("Not a participant in this room");

        if (room.HostId == request.UserId)
            room.ReadyHost();
        else
            room.ReadyGuest();

        UoW.Rooms.Update(room);

        // If both players are ready, start the game
        if (room.HostReady && room.GuestReady)
        {
            var game = Chess.Domain.Entities.Game.Create(
                room.HostId,
                room.GuestId!.Value,
                room.BaseTimeSeconds,
                room.IncrementSeconds,
                room.IsRated);

            await UoW.Games.AddAsync(game);
            room.Close();
        }

        await UoW.SaveChangesAsync(ct);
        return new SuccessResponse(true);
    }
}
