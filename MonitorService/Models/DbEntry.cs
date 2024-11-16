using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonitorService.Models
{
    public class DbEntry
    {
      public required string KeyName { get; set; }
      public required string EndpointKey { get; set; }
      public string ConnectionString { get; set; }
      public required bool IsMonitoring { get; set; }
    }

    public class DbEntryComparer : IEqualityComparer<DbEntry>
    {
      public bool Equals(DbEntry x, DbEntry y) 
      {
        return x.KeyName == y.KeyName && x.EndpointKey == y.EndpointKey;
      }

      public int GetHashCode(DbEntry obj) 
      {
        return HashCode.Combine(obj.KeyName, obj.EndpointKey);
      }
    }
}