using Chess.Application.Common;
using Chess.Application.Common.Authorization;
using Chess.Application.DTOs;
using Chess.Application.Ports;
using Chess.Application.Services;
using Chess.Domain.Entities;
using Chess.Domain.ValueObjects;

namespace Chess.Application.UseCases.Game;

public sealed class JoinQueue : UseCaseBase<JoinQueueRequest, JoinQueueResponse>
{
    private readonly IPermissionChecker _permissions;
    private readonly IMatchmakingService _matchmaking;

    public JoinQueue(IUnitOfWork uow, IPermissionChecker permissions, IMatchmakingService matchmaking) : base(uow)
    {
        _permissions = permissions;
        _matchmaking = matchmaking;
    }

    public override async Task<JoinQueueResponse> ExecuteAsync(JoinQueueRequest request, CancellationToken ct = default)
    {
        if (!_permissions.IsUser(request.UserId))
            throw new UnauthorizedAccessException("User not authorized");

        var user = await UoW.Users.GetByIdAsync(request.UserId);
        if (user is null || user.Status != UserStatus.Active)
            throw new InvalidOperationException("User not found or inactive");

        // Try to find a match immediately
        var match = await _matchmaking.TryMatchAsync(request.UserId, user.Rating, request.TimeControl, request.IsRated);

        if (match is not null)
        {
            // Match found, create room
            var room = Room.Create(
                match.Value,
                300,
                0,
                request.IsRated);
            room.Join(request.UserId);

            await UoW.Rooms.AddAsync(room);
            await UoW.SaveChangesAsync(ct);

            return new JoinQueueResponse(room.Id.ToString(), 0);
        }

        // No match found, estimate wait time
        return new JoinQueueResponse(request.UserId.ToString(), 30);
    }
}
