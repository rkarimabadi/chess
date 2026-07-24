using Chess.Application.Common;
using Chess.Application.DTOs;
using Chess.Application.Ports;
using Chess.Domain.Entities;

namespace Chess.Application.UseCases.Social;

public sealed class GetGameHistory : UseCaseBase<GetGameHistoryRequest, PagedResult<GameListItemDto>>
{
    private const int PageSize = 20;

    public GetGameHistory(IUnitOfWork uow) : base(uow) { }

    public override async Task<PagedResult<GameListItemDto>> ExecuteAsync(GetGameHistoryRequest request, CancellationToken ct = default)
    {
        var user = await UoW.Users.GetByIdAsync(request.UserId);
        if (user is null)
            throw new InvalidOperationException("User not found");

        var games = await UoW.Games.GetUserHistoryAsync(request.UserId, request.Page, PageSize);
        var totalCount = await UoW.Games.GetUserHistoryCountAsync(request.UserId);

        var items = new List<GameListItemDto>();
        foreach (var game in games)
        {
            var opponentId = game.WhitePlayerId == request.UserId ? game.BlackPlayerId : game.WhitePlayerId;
            var opponent = await UoW.Users.GetByIdAsync(opponentId);

            items.Add(new GameListItemDto
            {
                GameId = game.Id,
                OpponentUsername = opponent?.Username ?? "Unknown",
                OpponentRating = opponent?.Rating ?? 1200,
                Result = game.Result.ToString(),
                Variant = game.Variant,
                CreatedAt = game.CreatedAt
            });
        }

        return new PagedResult<GameListItemDto>(items, totalCount, request.Page, PageSize);
    }
}
