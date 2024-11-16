using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MonitorService.Core.Message;
using MonitorService.Models;

namespace MonitorService.Core.MessageHandlers
{
  public class TracerBackMessageHandler : MessageHandler<EndMessage, TracerBackMessageHandler, TracerBackMessage>
  {
    private readonly ManageDbMaintainer _maintainer;
    public TracerBackMessageHandler(ManageDbMaintainer maintainer, ILogger<TracerBackMessageHandler> logger) : base(logger)
    {
      _maintainer = maintainer;
    }

    public override Task<EndMessage> Handle(TracerBackMessage message)
    {
      _logger.LogCritical("Handle TracerBackMessage for {name}.", message.DbKey);
      var keys = message.DbKey.Split('.');
      _maintainer.UpdateEntry(new DbEntry {KeyName = keys[1], EndpointKey = keys[0], IsMonitoring = message.IsActive});
      return Task.FromResult(new EndMessage());
    }
  }

}