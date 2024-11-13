using MonitorService.Core;
using MonitorService.Core.Message;
using MonitorService.Core.MessageHandlers;
using MonitorService.DataTracking;
using MonitorService.Models;

namespace MonitorService;

public class ChangeTrackingWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ChangeTrackingWorker> _logger;
    private readonly ChannelMessenger _messenger;

    public ChangeTrackingWorker(ChannelMessenger messenger, IServiceScopeFactory scopeFactory, ILogger<ChangeTrackingWorker> logger)
    {
      _messenger = messenger;
      _scopeFactory = scopeFactory;
      _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      using var scope = _scopeFactory.CreateScope();
      var provider = scope.ServiceProvider;
      var trancerHandler= provider.GetRequiredService<TracerMessageHandler>();
      var taskHandler = provider.GetRequiredService<TaskMessageHandler>();

      await foreach(var message in _messenger.Receive(stoppingToken))
      {
        if(message is TracerMessage tracerMsg)
        {
          await trancerHandler.Handle(tracerMsg);
        }
        if(message is TaskMessage taskMsg)
        {
          await taskHandler.Handle(taskMsg);
        }
        
        _logger.LogInformation("Waiting for new Message...");
      }
    }
}
