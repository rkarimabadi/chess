using Chess.Application.Common;
using Chess.Application.Common.Authorization;
using Chess.Application.DTOs;
using Chess.Application.Ports;
using Chess.Domain.Entities;

namespace Chess.Application.UseCases.Social;

public sealed class ListFriends : UseCaseBase<Guid, List<FriendDto>>
{
    private readonly IPermissionChecker _permissions;

    public ListFriends(IUnitOfWork uow, IPermissionChecker permissions) : base(uow)
    {
        _permissions = permissions;
    }

    public override async Task<List<FriendDto>> ExecuteAsync(Guid userId, CancellationToken ct = default)
    {
        if (!_permissions.IsUser(userId))
            throw new UnauthorizedAccessException("User not authorized");

        var friendships = await UoW.Friendships.GetFriendsOfAsync(userId);
        var friends = new List<FriendDto>();

        foreach (var friendship in friendships)
        {
            var friendId = friendship.RequesterId == userId ? friendship.AddresseeId : friendship.RequesterId;
            var friend = await UoW.Users.GetByIdAsync(friendId);
            if (friend is not null)
            {
                friends.Add(new FriendDto(
                    friend.Id,
                    friend.Username,
                    friend.Rating,
                    false)); // Online status would come from presence tracker
            }
        }

        return friends;
    }
}
