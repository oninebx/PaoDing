using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonitorService.Models
{
    public class Message
    {
        public required string DbKey { get; set; }
        public required string ConnectionString { get; set; }
    }
}