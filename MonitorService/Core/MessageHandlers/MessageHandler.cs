using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonitorService.Core.MessageHandlers
{
    public abstract class MessageHandler<H, M> : IHandler<M> where H: IHandler<M> where M : IMessage
    {
      protected ILogger<H> _logger;
      public MessageHandler(ILogger<H> logger)
      {
        _logger = logger;
      }
      public abstract Task Handle(M message);

    }
}