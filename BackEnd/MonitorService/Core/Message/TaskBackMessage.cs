using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonitorService.Core.Message
{
    public class TaskBackMessage : IMessage
    {
      public required int Id { get; set; }
      public required int State { get; set; }
      public required string Content { get; set; }
    }
}