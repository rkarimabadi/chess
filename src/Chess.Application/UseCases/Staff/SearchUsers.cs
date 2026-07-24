using Chess.Application.Common;
using Chess.Application.Common.Authorization;
using Chess.Application.DTOs;
using Chess.Application.Ports;

namespace Chess.Application.UseCases.Staff;

public sealed class SearchUsers : UseCaseBase<string, List<UserDto>>
{
    private readonly IPermissionChecker _permissions;

    public SearchUsers(IUnitOfWork uow, IPermissionChecker permissions) : base(uow)
    {
        _permissions = permissions;
    }

    public override async Task<List<UserDto>> ExecuteAsync(string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            throw new ArgumentException("Search query must be at least 2 characters");

        var users = await UoW.Users.SearchAsync(query, 1, 20);

        return users.Select(u => new UserDto
        {
            Id = u.Id,
            Username = u.Username,
            Email = u.Email,
            Rating = u.Rating,
            Role = u.Role.ToString(),
            Status = u.Status.ToString(),
            GamesPlayed = u.GamesPlayed,
            CreatedAt = u.CreatedAt,
            LastLoginAt = u.LastLoginAt
        }).ToList();
    }
}
