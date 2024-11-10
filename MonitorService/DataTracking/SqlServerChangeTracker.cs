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
    private readonly ILogger<SqlServerChangeTracker> _logger;
    private readonly string _connectionString;
    private readonly string _database;
    public SqlServerChangeTracker(string dbKey, string connectionString, ILogger<SqlServerChangeTracker> logger) {
      DbKey = dbKey;
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
        var sqlToEnableDbChangeTracking = $@"
            ALTER DATABASE [{_database}] SET CHANGE_TRACKING = ON (CHANGE_RETENTION = 2 DAYS, AUTO_CLEANUP = ON);";
        var sqlToEnableTablesChangeTracking = $@"
            USE [{_database}];
            DECLARE @TableName NVARCHAR(128);
            DECLARE TableCursor CURSOR FOR 
            SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE';
            OPEN TableCursor;
              FETCH NEXT FROM TableCursor INTO @TableName;

              WHILE @@FETCH_STATUS = 0
              BEGIN
                  DECLARE @SQL NVARCHAR(MAX) = 'ALTER TABLE ' + QUOTENAME(@TableName) + ' ENABLE CHANGE_TRACKING';
                  EXEC sp_executesql @SQL;
                  FETCH NEXT FROM TableCursor INTO @TableName;
              END;

            CLOSE TableCursor;
            DEALLOCATE TableCursor;
          ";
        await ExecuteNonQuery($"{sqlToEnableDbChangeTracking}{sqlToEnableTablesChangeTracking}");
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

    public async Task GetChanges()
    {
      var sql = $@"DECLARE @last_known_version BIGINT = 0;
DECLARE @current_version BIGINT = CHANGE_TRACKING_CURRENT_VERSION();

-- Loop through each table with Change Tracking enabled
DECLARE @table_name NVARCHAR(128);
DECLARE table_cursor CURSOR FOR 
SELECT t.name
FROM sys.change_tracking_tables ct
JOIN sys.tables t ON ct.object_id = t.object_id;

OPEN table_cursor;
FETCH NEXT FROM table_cursor INTO @table_name;

WHILE @@FETCH_STATUS = 0
BEGIN
    DECLARE @sql NVARCHAR(MAX) = 
        'SELECT ''' + @table_name + ''' AS TableName, * FROM CHANGETABLE(CHANGES ' + @table_name + ', ' + CAST(@last_known_version AS NVARCHAR) + ') AS CT';
        EXEC sp_executesql @sql;
PRINT @sql;
    FETCH NEXT FROM table_cursor INTO @table_name;
END;

CLOSE table_cursor;
DEALLOCATE table_cursor;";
    }
  }
}