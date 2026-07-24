using Chess.Application.Common;
using Chess.Application.DTOs;
using Chess.Application.Ports;
using Chess.Domain.Entities;
using Chess.Domain.ValueObjects;

namespace Chess.Application.UseCases.Social;

public sealed class GetGameDetails : UseCaseBase<GetGameDetailsRequest, GameDetailsDto?>
{
    public GetGameDetails(IUnitOfWork uow) : base(uow) { }

    public override async Task<GameDetailsDto?> ExecuteAsync(GetGameDetailsRequest request, CancellationToken ct = default)
    {
        var game = await UoW.Games.GetByIdAsync(request.GameId);
        if (game is null)
            return null;

        var white = await UoW.Users.GetByIdAsync(game.WhitePlayerId);
        var black = await UoW.Users.GetByIdAsync(game.BlackPlayerId);

        var moves = await UoW.Moves.GetByGameIdAsync(game.Id);
        var moveDtos = moves.Select(m => new MoveDto
        {
            MoveNumber = m.MoveNumber,
            From = m.From.ToAlgebraic(),
            To = m.To.ToAlgebraic(),
            San = m.SanNotation,
            IsCheck = m.IsCheck,
            IsCheckmate = m.IsCheckmate,
            IsCapture = m.IsCapture,
            Timestamp = m.Timestamp
        }).ToList();

        // Build FEN history from pre-computed FenBefore/FenAfter stored on each MoveRecord
        var fenHistory = new List<string>();
        if (moves.Count > 0)
        {
            fenHistory.Add(moves[0].FenBefore);
            foreach (var move in moves)
                fenHistory.Add(move.FenAfter);
        }
        else
        {
            fenHistory.Add(BoardState.Initial().ToFen());
        }

        return new GameDetailsDto
        {
            GameId = game.Id,
            Status = game.Status.ToString(),
            Result = game.Result.ToString(),
            Reason = game.Reason.ToString(),
            IsRated = game.IsRated,
            Variant = game.Variant,
            TimeControl = new TimeControlDto(game.BaseTimeSeconds, game.IncrementSeconds),
            White = new PlayerDto(game.WhitePlayerId, white?.Username ?? "Unknown", white?.Rating ?? 1200),
            Black = new PlayerDto(game.BlackPlayerId, black?.Username ?? "Unknown", black?.Rating ?? 1200),
            FinalFen = game.CurrentFen,
            Moves = moveDtos,
            FenHistory = fenHistory,
            CreatedAt = game.CreatedAt,
            FinishedAt = game.FinishedAt
        };
    }
}
