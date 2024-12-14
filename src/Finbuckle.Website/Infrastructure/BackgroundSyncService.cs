namespace Finbuckle.Website.Infrastructure;

public class BackgroundSyncService : BackgroundService
{
    private readonly ILogger<BackgroundSyncService> _logger;

    public BackgroundSyncService(ILogger<BackgroundSyncService> logger, DocVersionService docVersionService)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(TimeSpan.FromMinutes(10));

        try
        {
            _logger.LogInformation("Background Sync Service is starting.");
            do
            {
                await DoSync(stoppingToken);
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Background Sync Service is stopping.");
        }
    }


    private async Task DoSync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Background Sync Service is syncing.");
        
        _logger.LogInformation("Background Sync Service is finished syncing.");
    }
}