using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MonitorService.Core;

namespace MonitorService.Core.Message
{
    public class TracerMessage : IMessage
    {
        public required string DbKey { get; set; }
        public required string ConnectionString { get; set; }
        public required bool IsMonitoring { get; set; }
    }
}