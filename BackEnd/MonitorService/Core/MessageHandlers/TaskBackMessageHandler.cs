using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MonitorService.Core.Message;

namespace MonitorService.Core.MessageHandlers
{
  public class TaskBackMessageHandler : MessageHandler<EndMessage, TaskBackMessageHandler, TaskBackMessage>
  {
    private readonly ManageDbMaintainer _maintainer;
    public TaskBackMessageHandler(ManageDbMaintainer maintainer, ILogger<TaskBackMessageHandler> logger) : base(logger)
    {
      _maintainer = maintainer;
    }

    public override Task<EndMessage> Handle(TaskBackMessage message)
    {
      _maintainer.UpdateMonitorChanges(message.Id, message.Content);
      _logger.LogCritical("Handle TaskBackMessage");
      return Task.FromResult(new EndMessage());
    }
  }
}