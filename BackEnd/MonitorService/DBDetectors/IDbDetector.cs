using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MonitorService.Models;

namespace MonitorService.DBDetectors
{
    public interface IDbDetector
    {
        Task<IEnumerable<DbEndPoint>> GetEndpoints();
    }
}