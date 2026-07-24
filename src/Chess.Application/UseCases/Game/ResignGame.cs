using Chess.Application.Common;
using Chess.Application.DTOs;
using Chess.Application.Ports;
using Chess.Application.Services;
using Chess.Domain.Entities;
using Chess.Domain.ValueObjects;

namespace Chess.Application.UseCases.Game;

public sealed class ResignGame : UseCaseBase<(Guid UserId, Guid GameId), GameResultDto>
{
    private readonly IGameStateManager _stateManager;
    private readonly IRatingService _ratingService;

    public ResignGame(IUnitOfWork uow, IGameStateManager stateManager, IRatingService ratingService) : base(uow)
    {
        _stateManager = stateManager;
        _ratingService = ratingService;
    }

    public override async Task<GameResultDto> ExecuteAsync((Guid UserId, Guid GameId) request, CancellationToken ct = default)
    {
        var game = await UoW.Games.GetByIdAsync(request.GameId);
        if (game is null)
            throw new InvalidOperationException("Game not found");

        if (game.Status != GameStatus.Active)
            throw new InvalidOperationException("Game is not active");

        if (game.WhitePlayerId != request.UserId && game.BlackPlayerId != request.UserId)
            throw new UnauthorizedAccessException("Not a player in this game");

        var winner = game.WhitePlayerId == request.UserId
            ? GameResult.BlackWins
            : GameResult.WhiteWins;

        var whiteUser = await UoW.Users.GetByIdAsync(game.WhitePlayerId);
        var blackUser = await UoW.Users.GetByIdAsync(game.BlackPlayerId);

        var ratingResult = _ratingService.Calculate(
            whiteUser?.Rating ?? 1200,
            blackUser?.Rating ?? 1200,
            winner,
            game.IsRated);

        game.Finish(winner, ResultReason.Resignation);
        if (whiteUser is not null) whiteUser.SetRating(ratingResult.WhiteNewRating);
        if (blackUser is not null) blackUser.SetRating(ratingResult.BlackNewRating);

        UoW.Games.Update(game);
        if (whiteUser is not null) UoW.Users.Update(whiteUser);
        if (blackUser is not null) UoW.Users.Update(blackUser);
        await UoW.SaveChangesAsync(ct);

        var state = await _stateManager.GetAsync(request.GameId);
        if (state is not null)
        {
            await _stateManager.RemoveAsync(request.GameId);
        }

        return new GameResultDto
        {
            GameId = game.Id,
            Result = winner.ToString(),
            Reason = ResultReason.Resignation.ToString(),
            WhiteRating = new RatingChangeDto(ratingResult.WhiteOldRating, ratingResult.WhiteNewRating, ratingResult.WhiteDelta),
            BlackRating = new RatingChangeDto(ratingResult.BlackOldRating, ratingResult.BlackNewRating, ratingResult.BlackDelta)
        };
    }
}
