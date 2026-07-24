using Chess.Application.Common;
using Chess.Application.Common.Authorization;
using Chess.Application.DTOs;
using Chess.Application.Ports;
using Chess.Domain.Entities;
using Chess.Domain.ValueObjects;

namespace Chess.Application.UseCases.Social;

public sealed class RespondFriendRequest : UseCaseBase<(Guid UserId, Guid FriendshipId, bool Accept), SuccessResponse>
{
    private readonly IPermissionChecker _permissions;

    public RespondFriendRequest(IUnitOfWork uow, IPermissionChecker permissions) : base(uow)
    {
        _permissions = permissions;
    }

    public override async Task<SuccessResponse> ExecuteAsync((Guid UserId, Guid FriendshipId, bool Accept) request, CancellationToken ct = default)
    {
        if (!_permissions.IsUser(request.UserId))
            throw new UnauthorizedAccessException("User not authorized");

        var friendship = await UoW.Friendships.GetByIdAsync(request.FriendshipId);
        if (friendship is null)
            throw new InvalidOperationException("Friendship request not found");

        if (friendship.AddresseeId != request.UserId)
            throw new UnauthorizedAccessException("Not authorized to respond to this request");

        if (friendship.Status != FriendshipStatus.Pending)
            throw new InvalidOperationException("Friendship request is not pending");

        if (request.Accept)
            friendship.Accept();
        else
            friendship.Decline();

        UoW.Friendships.Update(friendship);
        await UoW.SaveChangesAsync(ct);

        return new SuccessResponse(true);
    }
}
