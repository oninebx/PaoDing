using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonitorService.Models
{
    public class DbEntry
    {
      public required string EntryKey { get; set; }
      public required string UserName { get; set; }
      public required string Password { get; set; }
      public required bool IsMonitored { get; set; }
    }
}