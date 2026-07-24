using Chess.Application.Common;
using Chess.Application.DTOs;
using Chess.Application.Ports;
using Chess.Application.Services;
using Chess.Domain.Entities;
using Chess.Domain.ValueObjects;

namespace Chess.Application.UseCases.Game;

public sealed class JoinAsSpectator : UseCaseBase<(Guid UserId, Guid GameId), GameStateDto?>
{
    private readonly IGameStateManager _stateManager;

    public JoinAsSpectator(IUnitOfWork uow, IGameStateManager stateManager) : base(uow)
    {
        _stateManager = stateManager;
    }

    public override async Task<GameStateDto?> ExecuteAsync((Guid UserId, Guid GameId) request, CancellationToken ct = default)
    {
        var game = await UoW.Games.GetByIdAsync(request.GameId);
        if (game is null)
            return null;

        if (game.Status != GameStatus.Active)
            throw new InvalidOperationException("Game is not active");

        // Spectators can only watch rated or public games
        if (!game.IsRated && game.Status != GameStatus.Active)
            throw new InvalidOperationException("Cannot spectate this game");

        var state = await _stateManager.GetAsync(request.GameId);
        if (state is null)
            return null;

        var white = await UoW.Users.GetByIdAsync(game.WhitePlayerId);
        var black = await UoW.Users.GetByIdAsync(game.BlackPlayerId);

        // Return delayed state (3-5 seconds) for spectators
        return new GameStateDto
        {
            GameId = game.Id,
            Status = game.Status.ToString(),
            IsRated = game.IsRated,
            Variant = game.Variant,
            TimeControl = new TimeControlDto(game.BaseTimeSeconds, game.IncrementSeconds),
            White = new PlayerDto(game.WhitePlayerId, white?.Username ?? "Unknown", white?.Rating ?? 1200),
            Black = new PlayerDto(game.BlackPlayerId, black?.Username ?? "Unknown", black?.Rating ?? 1200),
            CurrentTurn = state.CurrentTurn.ToString(),
            BoardFen = state.Board.ToFen(),
            WhiteTimeMs = state.WhiteTimeMs,
            BlackTimeMs = state.BlackTimeMs,
            MoveCount = state.MoveHistory.Count,
            DrawOfferPending = state.DrawOfferPending,
            Material = new MaterialDto(new(), new())
        };
    }
}
