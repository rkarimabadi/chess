using Chess.Application.Ports;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Chess.Infrastructure.Services;

public sealed class RoomCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RoomCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(5);

    public RoomCleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<RoomCleanupService> logger)
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
                var removed = await CleanupExpiredRoomsAsync();
                if (removed > 0)
                    _logger.LogInformation("Cleaned up {Count} expired rooms", removed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during room cleanup");
            }

            await Task.Delay(_cleanupInterval, stoppingToken);
        }
    }

    private async Task<int> CleanupExpiredRoomsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        return await uow.Rooms.CleanupExpiredAsync();
    }
}
