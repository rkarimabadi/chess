using Chess.Application.Common;
using Chess.Application.DTOs;
using Chess.Application.Ports;
using Chess.Application.Services;
using Chess.Domain.Entities;
using Chess.Domain.ValueObjects;

namespace Chess.Application.UseCases.Game;

public sealed class RespondDraw : UseCaseBase<(Guid UserId, Guid GameId, bool Accept), GameResultDto?>
{
    private readonly IGameStateManager _stateManager;
    private readonly IRatingService _ratingService;

    public RespondDraw(IUnitOfWork uow, IGameStateManager stateManager, IRatingService ratingService) : base(uow)
    {
        _stateManager = stateManager;
        _ratingService = ratingService;
    }

    public override async Task<GameResultDto?> ExecuteAsync((Guid UserId, Guid GameId, bool Accept) request, CancellationToken ct = default)
    {
        var game = await UoW.Games.GetByIdAsync(request.GameId);
        if (game is null)
            throw new InvalidOperationException("Game not found");

        if (game.Status != GameStatus.Active)
            throw new InvalidOperationException("Game is not active");

        if (!game.DrawOfferPending)
            throw new InvalidOperationException("No draw offer pending");

        if (game.DrawOfferedById == request.UserId)
            throw new InvalidOperationException("Cannot respond to your own draw offer");

        var state = await _stateManager.GetAsync(request.GameId);
        if (state is null)
            throw new InvalidOperationException("Game state not found");

        state.DrawOfferPending = false;

        if (!request.Accept)
        {
            await _stateManager.UpsertAsync(request.GameId, state);
            return null;
        }

        // Draw accepted - finish game
        var whiteUser = await UoW.Users.GetByIdAsync(game.WhitePlayerId);
        var blackUser = await UoW.Users.GetByIdAsync(game.BlackPlayerId);

        var ratingResult = _ratingService.Calculate(
            whiteUser?.Rating ?? 1200,
            blackUser?.Rating ?? 1200,
            GameResult.Draw,
            game.IsRated);

        game.Finish(GameResult.Draw, ResultReason.Agreement);
        if (whiteUser is not null) whiteUser.SetRating(ratingResult.WhiteNewRating);
        if (blackUser is not null) blackUser.SetRating(ratingResult.BlackNewRating);

        UoW.Games.Update(game);
        if (whiteUser is not null) UoW.Users.Update(whiteUser);
        if (blackUser is not null) UoW.Users.Update(blackUser);
        await _stateManager.UpsertAsync(request.GameId, state);
        await UoW.SaveChangesAsync(ct);

        return new GameResultDto
        {
            GameId = game.Id,
            Result = GameResult.Draw.ToString(),
            Reason = ResultReason.Agreement.ToString(),
            WhiteRating = new RatingChangeDto(ratingResult.WhiteOldRating, ratingResult.WhiteNewRating, ratingResult.WhiteDelta),
            BlackRating = new RatingChangeDto(ratingResult.BlackOldRating, ratingResult.BlackNewRating, ratingResult.BlackDelta)
        };
    }
}
