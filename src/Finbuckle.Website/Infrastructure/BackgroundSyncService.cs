namespace Finbuckle.Website.Infrastructure;

public class BackgroundSyncService(ILogger<BackgroundSyncService> logger, GitHubService.GitHubService gitHubService) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(TimeSpan.FromMinutes(15));

        try
        {
            logger.LogDebug("Background Sync Service is starting.");
            do
            {
                await DoSync(stoppingToken);
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            logger.LogDebug("Background Sync Service is stopping.");
        }
    }


    private async Task DoSync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Background Sync Service is syncing.");

        logger.LogInformation("gitHubService.LoadAsync()");
        await gitHubService.LoadAsync();
        
        logger.LogInformation("Background Sync Service is finished syncing.");
    }
}