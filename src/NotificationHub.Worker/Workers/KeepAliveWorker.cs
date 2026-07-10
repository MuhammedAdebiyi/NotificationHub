using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NotificationHub.Infrastructure.Persistence;

namespace NotificationHub.Worker.Workers;

public class KeepAliveWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<KeepAliveWorker> _logger;

    public KeepAliveWorker(IServiceScopeFactory scopeFactory, ILogger<KeepAliveWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("KeepAliveWorker started — pinging DB every 4 minutes");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await context.Database.ExecuteSqlRawAsync("SELECT 1", stoppingToken);
                _logger.LogDebug("KeepAlive ping sent");
            }
            catch (Exception ex)
            {
                _logger.LogWarning("KeepAlive ping failed: {Message}", ex.Message);
            }

            await Task.Delay(TimeSpan.FromMinutes(4), stoppingToken);
        }
    }
}