using Chess.Application.Common;
using Chess.Application.Common.Authorization;
using Chess.Application.DTOs;
using Chess.Application.Ports;
using Chess.Domain.Entities;
using Chess.Domain.ValueObjects;

namespace Chess.Application.UseCases.Social;

public sealed class BlockUser : UseCaseBase<BlockUserRequest, SuccessResponse>
{
    private readonly IPermissionChecker _permissions;

    public BlockUser(IUnitOfWork uow, IPermissionChecker permissions) : base(uow)
    {
        _permissions = permissions;
    }

    public override async Task<SuccessResponse> ExecuteAsync(BlockUserRequest request, CancellationToken ct = default)
    {
        if (!_permissions.IsUser(request.UserId))
            throw new UnauthorizedAccessException("User not authorized");

        if (request.UserId == request.TargetUserId)
            throw new InvalidOperationException("Cannot block yourself");

        var user = await UoW.Users.GetByIdAsync(request.UserId);
        if (user is null || user.Status != UserStatus.Active)
            throw new InvalidOperationException("User not found or inactive");

        var target = await UoW.Users.GetByIdAsync(request.TargetUserId);
        if (target is null)
            throw new InvalidOperationException("Target user not found");

        // Check if already blocked
        if (await UoW.UserBlocks.IsBlockedAsync(request.UserId, request.TargetUserId))
            throw new InvalidOperationException("User already blocked");

        // Remove existing friendship if any
        var friendship = await UoW.Friendships.GetBetweenAsync(request.UserId, request.TargetUserId);
        if (friendship is not null)
        {
            friendship.Decline();
            UoW.Friendships.Update(friendship);
        }

        var reverseFriendship = await UoW.Friendships.GetBetweenAsync(request.TargetUserId, request.UserId);
        if (reverseFriendship is not null)
        {
            reverseFriendship.Decline();
            UoW.Friendships.Update(reverseFriendship);
        }

        var block = UserBlock.Create(request.UserId, request.TargetUserId);

        await UoW.UserBlocks.AddAsync(block);
        await UoW.SaveChangesAsync(ct);

        return new SuccessResponse(true);
    }
}
