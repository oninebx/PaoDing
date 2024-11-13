using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonitorService.Models
{
    public class MonitorTask
    {
      public required int Id { get; set;}
      public required string Name { get; set; }
      public required string Endpoint { get; set; }
      public required int State { get; set; }
      public required DateTime CreatedAt { get; set; }
    }
}