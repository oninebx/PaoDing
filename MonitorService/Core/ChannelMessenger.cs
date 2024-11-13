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
        public Channel<IMessage> _channel = Channel.CreateUnbounded<IMessage>();

        public async Task Send(IMessage message, CancellationToken token)
        {
          if(message != null) {
            await _channel.Writer.WriteAsync(message, token);
          }
        }

        public async Task SendBulk(IEnumerable<IMessage> messages, CancellationToken token)
        {
          if(messages?.Any() ?? false)
          {
            foreach(var message in messages)
            {
              await _channel.Writer.WriteAsync(message, token);
            }
          }
        }

        public IAsyncEnumerable<IMessage> Receive(CancellationToken token)
        {
          
          return _channel.Reader.ReadAllAsync(token);
        }
    }
}