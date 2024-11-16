using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using MonitorService.Models;

namespace MonitorService.Core
{
    public class ChannelMessenger
    {
        private readonly Channel<IMessage> _forwardChannel = Channel.CreateUnbounded<IMessage>();
        private readonly Channel<IMessage> _backwardChannel = Channel.CreateUnbounded<IMessage>();

        public async Task ForwardSend(IMessage message, CancellationToken token)
        {
          if(message != null) {
            await _forwardChannel.Writer.WriteAsync(message, token);
          }
        }

        public async Task ForwardSendBulk(IEnumerable<IMessage> messages, CancellationToken token)
        {
          if(messages?.Any() ?? false)
          {
            foreach(var message in messages)
            {
              await _forwardChannel.Writer.WriteAsync(message, token);
            }
          }
        }

        public IAsyncEnumerable<IMessage> ForwardReceive(CancellationToken token)
        {
          return _forwardChannel.Reader.ReadAllAsync(token);
        }

        public async Task BacwardSend(IMessage message, CancellationToken token)
        {
          if(message != null) {
            await _backwardChannel.Writer.WriteAsync(message, token);
          }
        }

        public async Task BackwardSendBulk(IEnumerable<IMessage> messages, CancellationToken token)
        {
          if(messages?.Any() ?? false)
          {
            foreach(var message in messages)
            {
              await _backwardChannel.Writer.WriteAsync(message, token);
            }
          }
        }

        public IAsyncEnumerable<IMessage> BackwardReceive(CancellationToken token)
        {
          return _backwardChannel.Reader.ReadAllAsync(token);
        }
    }
}