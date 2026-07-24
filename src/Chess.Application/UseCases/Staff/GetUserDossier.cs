using Chess.Application.Common;
using Chess.Application.Common.Authorization;
using Chess.Application.DTOs;
using Chess.Application.Ports;
using Chess.Domain.Entities;

namespace Chess.Application.UseCases.Staff;

public sealed class GetUserDossier : UseCaseBase<(Guid StaffId, Guid UserId), UserDossierDto?>
{
    private readonly IPermissionChecker _permissions;

    public GetUserDossier(IUnitOfWork uow, IPermissionChecker permissions) : base(uow)
    {
        _permissions = permissions;
    }

    public override async Task<UserDossierDto?> ExecuteAsync((Guid StaffId, Guid UserId) request, CancellationToken ct = default)
    {
        if (!_permissions.IsStaff(request.StaffId))
            throw new UnauthorizedAccessException("Staff access required");

        var user = await UoW.Users.GetByIdAsync(request.UserId);
        if (user is null)
            return null;

        var sanctions = await UoW.Sanctions.GetActiveByUserIdAsync(request.UserId);
        var reports = await UoW.Reports.GetByTargetUserIdAsync(request.UserId);
        var recentGames = await UoW.Games.GetUserHistoryAsync(request.UserId, 1, 10);

        var sanctionDtos = sanctions.Select(s => new SanctionDto
        {
            Id = s.Id,
            Username = user.Username,
            Type = s.Type.ToString(),
            Reason = s.Reason,
            StartsAt = s.StartsAt,
            EndsAt = s.EndsAt,
            IsActive = s.IsActive
        }).ToList();

        var reportDtos = reports.Select(r => new ReportListItemDto
        {
            Id = r.Id,
            TargetUsername = user.Username,
            Reason = r.Reason.ToString(),
            Status = r.Status.ToString(),
            CreatedAt = r.CreatedAt
        }).ToList();

        var gameDtos = new List<GameListItemDto>();
        foreach (var g in recentGames)
        {
            var opponentId = g.WhitePlayerId == request.UserId ? g.BlackPlayerId : g.WhitePlayerId;
            var opponent = await UoW.Users.GetByIdAsync(opponentId);

            gameDtos.Add(new GameListItemDto
            {
                GameId = g.Id,
                OpponentUsername = opponent?.Username ?? "ناشناخته",
                OpponentRating = opponent?.Rating ?? 1200,
                Result = g.Result.ToString(),
                Variant = g.Variant,
                CreatedAt = g.CreatedAt
            });
        }

        return new UserDossierDto
        {
            User = new UserDto
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
            },
            Sanctions = sanctionDtos,
            Reports = reportDtos,
            RecentGames = gameDtos
        };
    }
}
