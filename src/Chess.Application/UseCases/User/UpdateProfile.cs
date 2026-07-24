using Chess.Application.Common;
using Chess.Application.DTOs;
using Chess.Application.Ports;
using Chess.Domain.Entities;

namespace Chess.Application.UseCases.User;

public sealed class UpdateProfile : UseCaseBase<(Guid UserId, UpdateProfileRequest Request), UserDto>
{
    public UpdateProfile(IUnitOfWork uow) : base(uow) { }

    public override async Task<UserDto> ExecuteAsync((Guid UserId, UpdateProfileRequest Request) request, CancellationToken ct = default)
    {
        var user = await UoW.Users.GetByIdAsync(request.UserId);
        if (user is null)
            throw new InvalidOperationException("User not found");

        if (request.Request.DisplayName is not null)
        {
            // In a real implementation, this would update the display name
            // For now, we just return the current user
        }

        UoW.Users.Update(user);
        await UoW.SaveChangesAsync(ct);

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
