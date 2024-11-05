using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MonitorService.Models;

namespace MonitorService.DBDetectors
{
    public interface IDbDetector
    {
        Task<IEnumerable<SqlServerEntry>> GetActiveEntries();
    }
}


// while (!stoppingToken.IsCancellationRequested)
//         {
//             _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
//             await Task.Delay(1_000, stoppingToken);
//         }