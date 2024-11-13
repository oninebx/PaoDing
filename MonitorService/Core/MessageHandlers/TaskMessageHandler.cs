using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MonitorService.Core.Message;

namespace MonitorService.Core.MessageHandlers
{
  public class TaskMessageHandler : MessageHandler<TaskMessageHandler, TaskMessage>
  {
    private readonly TracerContainer _container;
    public TaskMessageHandler(TracerContainer container, ILogger<TaskMessageHandler> logger) : base(logger)
    {
      _container = container;
    }

    public override async Task Handle(TaskMessage message)
    {
      switch(message.State)
      {
        case 0:
          // Set the start version to monitor
          var tracers = _container.Search(message.Endpoint);
          foreach(var tracer in tracers)
          {
            await tracer.UpdateCurrentVersion();
            _logger.LogInformation("{name} monitor task started in {endpoint}.", message.Name, message.Endpoint);
          }
          
          break;
        case 1:
          break;
        case 2:
          break;
      }
    }
  }
}