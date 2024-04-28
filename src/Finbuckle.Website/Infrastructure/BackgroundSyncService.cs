namespace Finbuckle.Website.Infrastructure;

public class BackgroundSyncService : BackgroundService
{
    private readonly ILogger<BackgroundSyncService> _logger;
    private readonly DocVersionService _docVersionService;

    public BackgroundSyncService(ILogger<BackgroundSyncService> logger, DocVersionService docVersionService)
    {
        _logger = logger;
        _docVersionService = docVersionService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(TimeSpan.FromMinutes(10));

        try
        {
            _logger.LogInformation("Background Sync Service is starting.");
            
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await DoSync();
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Background Sync Service is stopping.");
        }
    }


    private async Task DoSync()
    {
        _logger.LogInformation("Background Sync Service is syncing.");

       await _docVersionService.LoadAsync();
    }
}