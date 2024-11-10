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
        public Channel<Message> _channel = Channel.CreateUnbounded<Message>();

        public async Task Send(Message message, CancellationToken token)
        {
          if(message != null) {
            await _channel.Writer.WriteAsync(message, token);
          }
        }

        public async Task SendBulk(IEnumerable<Message> messages, CancellationToken token)
        {
          if(messages?.Any() ?? false)
          {
            foreach(var message in messages)
            {
              await _channel.Writer.WriteAsync(message, token);
            }
          }
        }

        public IAsyncEnumerable<Message> Receive(CancellationToken token)
        {
          return _channel.Reader.ReadAllAsync(token);
        }
    }
}