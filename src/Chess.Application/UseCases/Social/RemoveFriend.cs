using Chess.Application.Common;
using Chess.Application.Common.Authorization;
using Chess.Application.DTOs;
using Chess.Application.Ports;
using Chess.Domain.Entities;
using Chess.Domain.ValueObjects;

namespace Chess.Application.UseCases.Social;

public sealed class RemoveFriend : UseCaseBase<(Guid UserId, Guid FriendId), SuccessResponse>
{
    private readonly IPermissionChecker _permissions;

    public RemoveFriend(IUnitOfWork uow, IPermissionChecker permissions) : base(uow)
    {
        _permissions = permissions;
    }

    public override async Task<SuccessResponse> ExecuteAsync((Guid UserId, Guid FriendId) request, CancellationToken ct = default)
    {
        if (!_permissions.IsUser(request.UserId))
            throw new UnauthorizedAccessException("User not authorized");

        var friendship = await UoW.Friendships.GetByIdAsync(request.FriendId);
        if (friendship is null)
            throw new InvalidOperationException("Friendship not found");

        if (friendship.RequesterId != request.UserId && friendship.AddresseeId != request.UserId)
            throw new UnauthorizedAccessException("Not a participant in this friendship");

        if (friendship.Status != FriendshipStatus.Accepted)
            throw new InvalidOperationException("Friendship is not active");

        friendship.Decline();
        UoW.Friendships.Update(friendship);
        await UoW.SaveChangesAsync(ct);

        return new SuccessResponse(true);
    }
}
