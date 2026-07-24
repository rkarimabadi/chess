using Chess.Application.Common;
using Chess.Application.DTOs;
using Chess.Application.Ports;
using Chess.Application.Services;
using Chess.Domain.Entities;
using Chess.Domain.ValueObjects;

namespace Chess.Application.UseCases.Game;

public sealed class OfferDraw : UseCaseBase<(Guid UserId, Guid GameId), SuccessResponse>
{
    private readonly IGameStateManager _stateManager;

    public OfferDraw(IUnitOfWork uow, IGameStateManager stateManager) : base(uow)
    {
        _stateManager = stateManager;
    }

    public override async Task<SuccessResponse> ExecuteAsync((Guid UserId, Guid GameId) request, CancellationToken ct = default)
    {
        var game = await UoW.Games.GetByIdAsync(request.GameId);
        if (game is null)
            throw new InvalidOperationException("Game not found");

        if (game.Status != GameStatus.Active)
            throw new InvalidOperationException("Game is not active");

        if (game.WhitePlayerId != request.UserId && game.BlackPlayerId != request.UserId)
            throw new UnauthorizedAccessException("Not a player in this game");

        if (game.DrawOfferPending)
            throw new InvalidOperationException("Draw offer already pending");

        var state = await _stateManager.GetAsync(request.GameId);
        if (state is null)
            throw new InvalidOperationException("Game state not found");

        state.DrawOfferPending = true;
        await _stateManager.UpsertAsync(request.GameId, state);

        return new SuccessResponse(true);
    }
}
