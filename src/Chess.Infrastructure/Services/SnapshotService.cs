using Chess.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Chess.Infrastructure.Services;

public sealed class SnapshotService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SnapshotService> _logger;
    private readonly TimeSpan _snapshotInterval = TimeSpan.FromMinutes(5);

    public SnapshotService(
        IServiceScopeFactory scopeFactory,
        ILogger<SnapshotService> logger)
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
                await SnapshotAllAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during snapshot");
            }

            await Task.Delay(_snapshotInterval, stoppingToken);
        }
    }

    private async Task SnapshotAllAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var stateManager = scope.ServiceProvider.GetRequiredService<IGameStateManager>();

        await stateManager.SnapshotAllActiveAsync();
        _logger.LogDebug("Snapshot completed");
    }
}
