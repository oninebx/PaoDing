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
      public required bool IsMonitoring { get; set;}
    }

    public class WorkingEntryComparer : IEqualityComparer<WorkingEntry>
    {
      public bool Equals(WorkingEntry x, WorkingEntry y) 
      {
        return x.ConnectionString == y.ConnectionString
         && x.EndpointKey == y.EndpointKey
         && x.EntryKey == y.EntryKey
         && x.IsMonitoring == y.IsMonitoring;
      }

      public int GetHashCode(WorkingEntry obj) 
      {
        return HashCode.Combine(obj.EntryKey, obj.EndpointKey, obj.ConnectionString, obj.IsMonitoring);
      }
    }
}