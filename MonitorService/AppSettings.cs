using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonitorService
{
    public class AppSettings
    {
        public IEnumerable<WorkingEntry> WorkingEntries { get; set; }
    }

    public class WorkingEntry 
    {
      public required string EndpointKey { get; set; }
      public required string EntryKey { get; set; }
      public required string ConnectionString { get; set; }
    }
}