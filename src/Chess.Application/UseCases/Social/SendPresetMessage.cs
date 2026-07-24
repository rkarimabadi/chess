using Chess.Application.Common;
using Chess.Application.DTOs;
using Chess.Application.Ports;
using Chess.Application.Services;
using Chess.Domain.Entities;
using Chess.Domain.ValueObjects;

namespace Chess.Application.UseCases.Social;

public sealed class SendPresetMessage : UseCaseBase<(Guid UserId, Guid GameId, string MessageKey), SuccessResponse>
{
    private readonly IMetricsService _metrics;

    public SendPresetMessage(IUnitOfWork uow, IMetricsService metrics) : base(uow)
    {
        _metrics = metrics;
    }

    public override async Task<SuccessResponse> ExecuteAsync((Guid UserId, Guid GameId, string MessageKey) request, CancellationToken ct = default)
    {
        var game = await UoW.Games.GetByIdAsync(request.GameId);
        if (game is null)
            throw new InvalidOperationException("Game not found");

        if (game.Status != GameStatus.Active)
            throw new InvalidOperationException("Game is not active");

        if (game.WhitePlayerId != request.UserId && game.BlackPlayerId != request.UserId)
            throw new UnauthorizedAccessException("Not a player in this game");

        var user = await UoW.Users.GetByIdAsync(request.UserId);
        if (user is null)
            throw new InvalidOperationException("User not found");

        // Check if user is muted
        if (user.PresetMessagesMuted && user.PresetMessagesMuteEndsAt > DateTime.UtcNow)
            throw new InvalidOperationException("Preset messages are muted");

        // Validate message key
        var validKeys = new[] { "good_game", "nice_move", "well_played", "good_luck", "thanks" };
        if (!validKeys.Contains(request.MessageKey.ToLowerInvariant()))
            throw new InvalidOperationException("Invalid message key");

        _metrics.IncrementCounter("preset_message_sent", new Dictionary<string, string>
        {
            ["gameId"] = request.GameId.ToString(),
            ["messageKey"] = request.MessageKey
        });

        return new SuccessResponse(true);
    }
}
