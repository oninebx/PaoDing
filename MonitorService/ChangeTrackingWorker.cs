using MonitorService.Core;
using MonitorService.DataTracking;

namespace MonitorService;

public class ChangeTrackingWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ChangeTrackingWorker> _logger;
    private readonly ChannelMessenger _messenger;
    private readonly IList<IChangeTracer> _tracers;

    public ChangeTrackingWorker(ChannelMessenger messenger, IServiceScopeFactory scopeFactory, ILogger<ChangeTrackingWorker> logger)
    {
      _messenger = messenger;
      _scopeFactory = scopeFactory;
      _logger = logger;
      _tracers = new List<IChangeTracer>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      using var scope = _scopeFactory.CreateScope();
      var provider = scope.ServiceProvider;
      while (!stoppingToken.IsCancellationRequested)
      {
          await foreach(var message in _messenger.Receive(stoppingToken))
          {
            _logger.LogCritical("Receive message - {dbKey} connects via {connectionString}", message.DbKey, message.ConnectionString);

            var logger = provider.GetRequiredService<ILogger<SqlServerChangeTracker>>();
            var tracer = new SqlServerChangeTracker(message.DbKey, message.ConnectionString, logger);
            try
            {
              await tracer.EnableDatabaseTracking();
            }
            catch(Exception e)
            {
              _logger.LogError(e.Message);
            }
            _tracers.Add(tracer);
          }
      }
        
    }
}
