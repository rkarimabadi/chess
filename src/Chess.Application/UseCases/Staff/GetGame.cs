using Chess.Application.Common;
using Chess.Application.DTOs;
using Chess.Application.Ports;
using Chess.Application.Services;
using Chess.Domain.Entities;
using Chess.Domain.ValueObjects;

namespace Chess.Application.UseCases.Staff;

public sealed class GetGame : UseCaseBase<Guid, GameStateDto?>
{
    private readonly IGameStateManager _stateManager;

    public GetGame(IUnitOfWork uow, IGameStateManager stateManager) : base(uow)
    {
        _stateManager = stateManager;
    }

    public override async Task<GameStateDto?> ExecuteAsync(Guid gameId, CancellationToken ct = default)
    {
        var game = await UoW.Games.GetByIdAsync(gameId);
        if (game is null)
            return null;

        var white = await UoW.Users.GetByIdAsync(game.WhitePlayerId);
        var black = await UoW.Users.GetByIdAsync(game.BlackPlayerId);

        var state = await _stateManager.GetAsync(gameId);

        return new GameStateDto
        {
            GameId = game.Id,
            Status = game.Status.ToString(),
            IsRated = game.IsRated,
            Variant = game.Variant,
            TimeControl = new TimeControlDto(game.BaseTimeSeconds, game.IncrementSeconds),
            White = new PlayerDto(game.WhitePlayerId, white?.Username ?? "Unknown", white?.Rating ?? 1200),
            Black = new PlayerDto(game.BlackPlayerId, black?.Username ?? "Unknown", black?.Rating ?? 1200),
            CurrentTurn = state?.CurrentTurn.ToString() ?? PieceColor.White.ToString(),
            BoardFen = state?.Board.ToFen() ?? game.CurrentFen,
            WhiteTimeMs = state?.WhiteTimeMs ?? game.WhiteTimeRemainingMs,
            BlackTimeMs = state?.BlackTimeMs ?? game.BlackTimeRemainingMs,
            MoveCount = state?.MoveHistory.Count ?? 0,
            DrawOfferPending = state?.DrawOfferPending ?? false,
            Material = new MaterialDto(new(), new())
        };
    }
}
