using Chess.Application.Common;
using Chess.Application.Common.Authorization;
using Chess.Application.DTOs;
using Chess.Application.Ports;
using Chess.Domain.Entities;
using Chess.Domain.ValueObjects;

namespace Chess.Application.UseCases.Social;

public sealed class SendFriendRequest : UseCaseBase<SendFriendRequestRequest, Guid>
{
    private readonly IPermissionChecker _permissions;

    public SendFriendRequest(IUnitOfWork uow, IPermissionChecker permissions) : base(uow)
    {
        _permissions = permissions;
    }

    public override async Task<Guid> ExecuteAsync(SendFriendRequestRequest request, CancellationToken ct = default)
    {
        if (!_permissions.IsUser(request.UserId))
            throw new UnauthorizedAccessException("User not authorized");

        if (request.UserId == request.TargetUserId)
            throw new InvalidOperationException("Cannot send friend request to yourself");

        var user = await UoW.Users.GetByIdAsync(request.UserId);
        if (user is null || user.Status != UserStatus.Active)
            throw new InvalidOperationException("User not found or inactive");

        var target = await UoW.Users.GetByIdAsync(request.TargetUserId);
        if (target is null || target.Status != UserStatus.Active)
            throw new InvalidOperationException("Target user not found or inactive");

        // Check if blocked
        if (await UoW.UserBlocks.IsBlockedAsync(request.TargetUserId, request.UserId))
            throw new InvalidOperationException("Cannot send friend request to this user");

        // Check if friendship already exists
        var existing = await UoW.Friendships.GetBetweenAsync(request.UserId, request.TargetUserId);
        if (existing is not null)
            throw new InvalidOperationException("Friendship already exists");

        var reverseExisting = await UoW.Friendships.GetBetweenAsync(request.TargetUserId, request.UserId);
        if (reverseExisting is not null)
            throw new InvalidOperationException("Friendship request already pending");

        var friendship = Friendship.Create(request.UserId, request.TargetUserId);

        await UoW.Friendships.AddAsync(friendship);
        await UoW.SaveChangesAsync(ct);

        return friendship.Id;
    }
}
