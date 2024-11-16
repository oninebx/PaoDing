using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MonitorService.DataTracking
{
    public interface IChangeTracer
    {
        Task EnableDatabaseTracking();
        Task DisableDatabaseTracking();
        Task UpdateCurrentVersion();
        DataSet GetDataChanges();
        string DbKey { get; }
    }
}