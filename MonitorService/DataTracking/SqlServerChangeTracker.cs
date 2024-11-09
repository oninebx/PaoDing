using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace MonitorService.DataTracking
{
  public class SqlServerChangeTracker : IChangeTracer
  {
    public string DbKey { get; }
    private readonly ManageDbMaintainer _maintainer;
    private readonly ILogger<SqlServerChangeTracker> _logger;
    private readonly string _connectionString;
    private readonly string _database;
    public SqlServerChangeTracker(string dbKey, string connectionString, ManageDbMaintainer maintainer, ILogger<SqlServerChangeTracker> logger) {
      DbKey = dbKey;
      _maintainer = maintainer;
      _logger = logger;
      _connectionString = connectionString;
      _database = dbKey.Split('.')[1];
    }
    public async Task EnableDatabaseTracking()
    {
      if(await IsChangeTrackingOn()) {
        _logger.LogInformation("Change Tracking is already enabled for {database}", DbKey);
      }
      else
      {
        var query = $@"
            ALTER DATABASE [{_database}] SET CHANGE_TRACKING = ON (CHANGE_RETENTION = 2 DAYS, AUTO_CLEANUP = ON);";
        await ExecuteNonQuery(query);
        _logger.LogInformation("Enable tracking for {database} successfully", DbKey);
      }
      
    }

    public async Task DisableDatabaseTracking()
    {
      var query = $@"
            ALTER DATABASE {_database}
            SET CHANGE_TRACKING = OFF;";

        await ExecuteNonQuery(query);
        _logger.LogInformation("Disable tracking for {database}", DbKey);
    }

    private async Task<bool> IsChangeTrackingOn()
    {
      var query = @$"
          SELECT COUNT(database_id) FROM sys.change_tracking_databases WHERE database_id = DB_ID('{_database}')";
      using(var connection = new SqlConnection(_connectionString))
      using(var command = new SqlCommand(query, connection))
      {
        connection.Open();
        return Convert.ToInt32(await command.ExecuteScalarAsync()) != 0;
      }
    }
    private async Task ExecuteNonQuery(string query)
    {
        using (var connection = new SqlConnection(_connectionString))
        using (var command = new SqlCommand(query, connection))
        {
            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }
    }
  }
}