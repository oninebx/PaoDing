using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonitorService.Core.Message
{
    public class TracerBackMessage : IMessage
    {
        public required string DbKey { get; set; }
        public required bool IsActive { get; set; }
    }
}