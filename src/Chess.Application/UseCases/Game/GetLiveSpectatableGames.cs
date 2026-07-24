using Chess.Application.Common;
using Chess.Application.DTOs;
using Chess.Application.Ports;
using Chess.Domain.Entities;

namespace Chess.Application.UseCases.Game;

public sealed class GetLiveSpectatableGames : UseCaseBase<GetLiveSpectatableGamesRequest, PagedResult<SpectatableGameDto>>
{
    private const int PageSize = 20;

    public GetLiveSpectatableGames(IUnitOfWork uow) : base(uow) { }

    public override async Task<PagedResult<SpectatableGameDto>> ExecuteAsync(GetLiveSpectatableGamesRequest request, CancellationToken ct = default)
    {
        var games = await UoW.Games.GetActiveGamesAsync();

        var items = new List<SpectatableGameDto>();
        foreach (var game in games)
        {
            var white = await UoW.Users.GetByIdAsync(game.WhitePlayerId);
            var black = await UoW.Users.GetByIdAsync(game.BlackPlayerId);

            items.Add(new SpectatableGameDto
            {
                GameId = game.Id,
                White = new PlayerDto(game.WhitePlayerId, white?.Username ?? "Unknown", white?.Rating ?? 1200),
                Black = new PlayerDto(game.BlackPlayerId, black?.Username ?? "Unknown", black?.Rating ?? 1200),
                Variant = game.Variant,
                CurrentTurn = game.CurrentFen.Contains(" w ") ? "White" : "Black",
                MoveCount = game.FullmoveNumber,
                StartedAt = game.StartedAt ?? game.CreatedAt
            });
        }

        return new PagedResult<SpectatableGameDto>(items, items.Count, request.Page, PageSize);
    }
}
