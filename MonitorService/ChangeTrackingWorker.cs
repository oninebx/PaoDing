namespace MonitorService;

public class ChangeTrackingWorker : BackgroundService
{
    private readonly ILogger<ChangeTrackingWorker> _logger;

    public ChangeTrackingWorker(ILogger<ChangeTrackingWorker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("ChangeTrackingWorker running at: {time}", DateTimeOffset.Now);
            await Task.Delay(1000, stoppingToken);
        }
    }
}
