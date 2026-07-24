using Chess.Application.Common;
using Chess.Application.Ports;
using Chess.Domain.Entities;
using Chess.Domain.ValueObjects;

namespace Chess.Application.UseCases.Game;

public sealed class AcceptRematch : UseCaseBase<(Guid UserId, Guid GameId, string RematchToken), Guid>
{
    public AcceptRematch(IUnitOfWork uow) : base(uow) { }

    public override async Task<Guid> ExecuteAsync((Guid UserId, Guid GameId, string RematchToken) request, CancellationToken ct = default)
    {
        var oldGame = await UoW.Games.GetByIdAsync(request.GameId);
        if (oldGame is null)
            throw new InvalidOperationException("Game not found");

        if (oldGame.Status != GameStatus.Finished)
            throw new InvalidOperationException("Game is not finished");

        if (oldGame.WhitePlayerId != request.UserId && oldGame.BlackPlayerId != request.UserId)
            throw new UnauthorizedAccessException("Not a player in this game");

        // Swap colors for rematch
        var newGame = Chess.Domain.Entities.Game.Create(
            oldGame.BlackPlayerId,
            oldGame.WhitePlayerId,
            oldGame.BaseTimeSeconds,
            oldGame.IncrementSeconds,
            oldGame.IsRated);

        await UoW.Games.AddAsync(newGame);
        await UoW.SaveChangesAsync(ct);

        return newGame.Id;
    }
}
