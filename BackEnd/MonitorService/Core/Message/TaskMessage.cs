using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonitorService.Core.Message
{
    public class TaskMessage : IMessage
    {
        public required int Id { get; set; }
        public required string Name { get; set; }
        public required string Endpoint { get; set; }
        public required int State { get; set;}
    }
}