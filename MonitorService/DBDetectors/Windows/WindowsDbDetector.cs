using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MonitorService.Models;

namespace MonitorService.DBDetectors.Windows
{
  public class WindowsDbDetector : IDbDetector
  {
    public Task<IEnumerable<DbEndPoint>> GetEndpoints()
    {
      throw new NotImplementedException();
    }
  }
}