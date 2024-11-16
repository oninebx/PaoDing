using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonitorService.Core.MessageHandlers
{
    public abstract class MessageHandler<R, H, M> : IHandler<R, M> where H: IHandler<R, M> where M : IMessage
    {
      protected ILogger<H> _logger;
      public MessageHandler(ILogger<H> logger)
      {
        _logger = logger;
      }
      public abstract Task<R> Handle(M message);

    }
}