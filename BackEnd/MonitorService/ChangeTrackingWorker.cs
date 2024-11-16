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

      await foreach(var message in _messenger.ForwardReceive(stoppingToken))
      {
        if(message is TracerMessage tracerMsg)
        {
          var backMessage = await trancerHandler.Handle(tracerMsg);
          await _messenger.BacwardSend(backMessage, stoppingToken);
        }
        if(message is TaskMessage taskMsg)
        {
          var backMessage = await taskHandler.Handle(taskMsg);
          await _messenger.BacwardSend(backMessage, stoppingToken);
        }
        
        _logger.LogInformation("Waiting for new Message...");
      }
    }
}
