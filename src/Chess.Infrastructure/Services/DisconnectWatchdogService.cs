using Chess.Application.Services;
using Chess.Domain.Entities;
using Chess.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Chess.Infrastructure.Services;

public sealed class DisconnectWatchdogService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DisconnectWatchdogService> _logger;

    public DisconnectWatchdogService(
        IServiceScopeFactory scopeFactory,
        ILogger<DisconnectWatchdogService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAllActiveGamesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking disconnect watchdog");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task CheckAllActiveGamesAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var gameStateManager = scope.ServiceProvider.GetRequiredService<IGameStateManager>();
        var uow = scope.ServiceProvider.GetRequiredService<Application.Ports.IUnitOfWork>();

        var activeGames = await uow.Games.GetActiveGamesAsync();

        foreach (var game in activeGames)
        {
            var state = await gameStateManager.GetAsync(game.Id);
            if (state is null) continue;

            var now = DateTime.UtcNow;

            if (state.BothDisconnected)
            {
                if (state.BothDisconnectedSince is null)
                {
                    state.BothDisconnectedSince = now;
                    await gameStateManager.UpsertAsync(game.Id, state);
                    continue;
                }

                var elapsed = (now - state.BothDisconnectedSince.Value).TotalSeconds;
                if (elapsed >= LiveGameState.ReconnectTimeoutSeconds)
                {
                    game.Abort();
                    uow.Games.Update(game);
                    await uow.SaveChangesAsync();
                    await gameStateManager.RemoveAsync(game.Id);
                    _logger.LogInformation("Game {GameId} aborted: both players disconnected", game.Id);
                }
            }
            else if (!state.WhiteConnected && state.WhiteDisconnectedAt.HasValue)
            {
                var elapsed = (now - state.WhiteDisconnectedAt.Value).TotalSeconds;
                if (elapsed >= LiveGameState.ReconnectTimeoutSeconds)
                {
                    game.Finish(GameResult.BlackWins, ResultReason.Disconnect);
                    uow.Games.Update(game);
                    await uow.SaveChangesAsync();
                    await gameStateManager.RemoveAsync(game.Id);
                    _logger.LogInformation("Game {GameId} finished: white disconnected timeout", game.Id);
                }
            }
            else if (!state.BlackConnected && state.BlackDisconnectedAt.HasValue)
            {
                var elapsed = (now - state.BlackDisconnectedAt.Value).TotalSeconds;
                if (elapsed >= LiveGameState.ReconnectTimeoutSeconds)
                {
                    game.Finish(GameResult.WhiteWins, ResultReason.Disconnect);
                    uow.Games.Update(game);
                    await uow.SaveChangesAsync();
                    await gameStateManager.RemoveAsync(game.Id);
                    _logger.LogInformation("Game {GameId} finished: black disconnected timeout", game.Id);
                }
            }
        }
    }
}
