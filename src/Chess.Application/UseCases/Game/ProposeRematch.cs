using Chess.Application.Common;
using Chess.Application.DTOs;
using Chess.Application.Ports;
using Chess.Domain.Entities;
using Chess.Domain.ValueObjects;

namespace Chess.Application.UseCases.Game;

public sealed class ProposeRematch : UseCaseBase<(Guid UserId, Guid GameId), ProposeRematchResponse>
{
    public ProposeRematch(IUnitOfWork uow) : base(uow) { }

    public override async Task<ProposeRematchResponse> ExecuteAsync((Guid UserId, Guid GameId) request, CancellationToken ct = default)
    {
        var game = await UoW.Games.GetByIdAsync(request.GameId);
        if (game is null)
            throw new InvalidOperationException("Game not found");

        if (game.Status != GameStatus.Finished)
            throw new InvalidOperationException("Game is not finished");

        if (game.WhitePlayerId != request.UserId && game.BlackPlayerId != request.UserId)
            throw new UnauthorizedAccessException("Not a player in this game");

        // Generate a rematch token
        var token = Guid.NewGuid().ToString("N")[..8];
        return new ProposeRematchResponse(token);
    }
}
