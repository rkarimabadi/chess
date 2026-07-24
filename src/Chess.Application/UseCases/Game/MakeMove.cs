using Chess.Application.Common;
using Chess.Application.DTOs;
using Chess.Application.Ports;
using Chess.Application.Services;
using Chess.Domain.Entities;
using Chess.Domain.ValueObjects;

namespace Chess.Application.UseCases.Game;

public sealed class MakeMove : UseCaseBase<MakeMoveRequest, MakeMoveResponse>
{
    private readonly IGameStateManager _stateManager;
    private readonly IClockService _clockService;
    private readonly Chess.Domain.Chess.Rules.IRuleSet _ruleSet;

    public MakeMove(
        IUnitOfWork uow,
        IGameStateManager stateManager,
        IClockService clockService,
        Chess.Domain.Chess.Rules.IRuleSet ruleSet) : base(uow)
    {
        _stateManager = stateManager;
        _clockService = clockService;
        _ruleSet = ruleSet;
    }

    public override async Task<MakeMoveResponse> ExecuteAsync(MakeMoveRequest request, CancellationToken ct = default)
    {
        var game = await UoW.Games.GetByIdAsync(request.GameId);
        if (game is null)
            throw new InvalidOperationException("Game not found");

        if (game.Status != GameStatus.Active)
            throw new InvalidOperationException("Game is not active");

        // Verify player is part of this game
        var playerColor = game.WhitePlayerId == request.UserId ? PieceColor.White : PieceColor.Black;
        if (game.BlackPlayerId != request.UserId && game.WhitePlayerId != request.UserId)
            throw new UnauthorizedAccessException("Not a player in this game");

        // Get or create live state
        var state = await _stateManager.GetAsync(request.GameId)
                    ?? CreateLiveGameState(game);

        // Verify it's the player's turn
        if (state.CurrentTurn != playerColor)
            throw new InvalidOperationException("Not your turn");

        var from = Square.Parse(request.From);
        var to = Square.Parse(request.To);
        var promotion = request.Promotion is not null
            ? Enum.Parse<PieceType>(request.Promotion)
            : (PieceType?)null;

        // Validate move
        var moveResult = _ruleSet.ValidateMove(state.Board, from, to, promotion);
        if (moveResult.Status == Chess.Domain.Chess.Rules.MoveResultStatus.Illegal)
            throw new InvalidOperationException(moveResult.Reason ?? "Illegal move");

        // Apply move
        var moveRecord = ApplyMove(state, from, to, promotion, moveResult);
        state.MoveHistory.Add(moveRecord);
        state.PositionHistory.Add(state.Board.ToFen());
        state.CurrentTurn = state.CurrentTurn.Opposite();
        state.LastMoveAt = DateTime.UtcNow;

        // Apply clock
        var elapsed = DateTime.UtcNow - state.LastMoveAt;
        var clockState = _clockService.Tick(state, playerColor, elapsed);
        state.WhiteTimeMs = clockState.WhiteTimeMs;
        state.BlackTimeMs = clockState.BlackTimeMs;

        // Apply increment
        var increment = _clockService.ApplyIncrement(state, playerColor);
        if (playerColor == PieceColor.White)
            state.WhiteTimeMs += increment;
        else
            state.BlackTimeMs += increment;

        await _stateManager.UpsertAsync(request.GameId, state);

        // Check for game end conditions
        var gameOver = CheckGameEnd(state, game);
        if (gameOver is not null)
        {
            return new MakeMoveResponse(
                Status: gameOver.Value.Result.ToString(),
                SanNotation: moveResult.SanNotation,
                NewFen: state.Board.ToFen(),
                WhiteTimeMs: state.WhiteTimeMs,
                BlackTimeMs: state.BlackTimeMs);
        }

        return new MakeMoveResponse(
            Status: "Ok",
            SanNotation: moveResult.SanNotation,
            NewFen: state.Board.ToFen(),
            WhiteTimeMs: state.WhiteTimeMs,
            BlackTimeMs: state.BlackTimeMs);
    }

    private static LiveGameState CreateLiveGameState(Chess.Domain.Entities.Game game)
    {
        return new LiveGameState
        {
            GameId = game.Id,
            Board = BoardState.FromFen(game.CurrentFen),
            CurrentTurn = PieceColor.White,
            WhiteTimeMs = game.WhiteTimeRemainingMs,
            BlackTimeMs = game.BlackTimeRemainingMs,
            PositionHistory = new List<string>(),
            LastMoveAt = DateTime.UtcNow
        };
    }

    private static MoveRecord ApplyMove(LiveGameState state, Square from, Square to, PieceType? promotion, Chess.Domain.Chess.Rules.MoveResult moveResult)
    {
        var piece = state.Board.GetPiece(from);
        var captured = state.Board.GetPiece(to);

        state.Board.MovePiece(from, to);

        if (promotion.HasValue)
        {
            state.Board.SetPiece(to, new Piece(piece!.Color, promotion.Value));
        }

        return MoveRecord.Create(
            gameId: Guid.Empty,
            moveNumber: state.MoveHistory.Count + 1,
            move: new Chess.Domain.Chess.Move(from, to, piece!, captured, false, false, false, promotion),
            san: moveResult.SanNotation ?? "",
            fenBefore: state.Board.ToFen(),
            fenAfter: state.Board.ToFen(),
            isCheck: moveResult.Status == Chess.Domain.Chess.Rules.MoveResultStatus.Check,
            isCheckmate: moveResult.Status == Chess.Domain.Chess.Rules.MoveResultStatus.Checkmate);
    }

    private (GameResult Result, ResultReason Reason)? CheckGameEnd(LiveGameState state, Chess.Domain.Entities.Game game)
    {
        if (_ruleSet.IsCheckmate(state.Board))
        {
            var winner = state.CurrentTurn.Opposite();
            return winner == PieceColor.White
                ? (GameResult.WhiteWins, ResultReason.Checkmate)
                : (GameResult.BlackWins, ResultReason.Checkmate);
        }

        if (_ruleSet.IsStalemate(state.Board))
        {
            return (GameResult.Draw, ResultReason.Stalemate);
        }

        if (_ruleSet.IsDrawByRules(state.Board, state.PositionHistory))
        {
            return (GameResult.Draw, ResultReason.ThreefoldRepetition);
        }

        if (_clockService.IsFlagged(new ClockState(state.WhiteTimeMs, state.BlackTimeMs), state.CurrentTurn))
        {
            var winner = state.CurrentTurn.Opposite();
            return winner == PieceColor.White
                ? (GameResult.WhiteWins, ResultReason.Timeout)
                : (GameResult.BlackWins, ResultReason.Timeout);
        }

        return null;
    }
}
