using Chess.Application.Common;
using Chess.Application.DTOs;
using Chess.Application.Ports;
using Chess.Domain.Entities;

namespace Chess.Application.UseCases.User;

public sealed class GetUserProfile : UseCaseBase<Guid, UserDto>
{
    public GetUserProfile(IUnitOfWork uow) : base(uow) { }

    public override async Task<UserDto> ExecuteAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await UoW.Users.GetByIdAsync(userId);
        if (user is null)
            throw new InvalidOperationException("User not found");

        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Rating = user.Rating,
            Role = user.Role.ToString(),
            Status = user.Status.ToString(),
            GamesPlayed = user.GamesPlayed,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        };
    }
}
