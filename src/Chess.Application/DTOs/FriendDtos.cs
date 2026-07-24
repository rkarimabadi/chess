namespace Chess.Application.DTOs;

public sealed record FriendDto(Guid Id, string Username, int Rating, bool IsOnline);
public sealed record FriendshipDto(Guid Id, string RequesterUsername, string Status, DateTime CreatedAt);
public sealed record BlockDto(Guid Id, string Username);
public sealed record SendFriendRequestRequest(Guid UserId, Guid TargetUserId);
public sealed record RespondFriendRequestRequest(bool Accept);
public sealed record BlockUserRequest(Guid UserId, Guid TargetUserId);
