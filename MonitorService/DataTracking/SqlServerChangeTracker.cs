using System;
using System.Collections.Generic;
using System.Data;
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
    private long _currentVersion = -1;
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
            SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' AND TABLE_NAME != 'MSchange_tracking_history';
            OPEN TableCursor;
              FETCH NEXT FROM TableCursor INTO @TableName;

              WHILE @@FETCH_STATUS = 0
              BEGIN
                  DECLARE @SQL NVARCHAR(MAX) = 'ALTER TABLE ' + QUOTENAME(@TableName) + ' ENABLE CHANGE_TRACKING WITH (TRACK_COLUMNS_UPDATED = ON);';
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
      var sqlToDisableTablesChangeTracking = $@"
            USE [{_database}];
            DECLARE @TableName NVARCHAR(128);
            DECLARE TableCursor CURSOR FOR 
            SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' AND TABLE_NAME != 'MSchange_tracking_history';
            OPEN TableCursor;
              FETCH NEXT FROM TableCursor INTO @TableName;

              WHILE @@FETCH_STATUS = 0
              BEGIN
                  DECLARE @SQL NVARCHAR(MAX) = 'ALTER TABLE ' + QUOTENAME(@TableName) + ' DISABLE CHANGE_TRACKING';
                  EXEC sp_executesql @SQL;
                  FETCH NEXT FROM TableCursor INTO @TableName;
              END;

            CLOSE TableCursor;
            DEALLOCATE TableCursor;
          ";
      var sqlToDisableDbChangeTracking = $@"
            ALTER DATABASE [{_database}]
            SET CHANGE_TRACKING = OFF;";

        await ExecuteNonQuery($"{sqlToDisableTablesChangeTracking}{sqlToDisableDbChangeTracking}");
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
      using var connection = new SqlConnection(_connectionString);
      using var command = new SqlCommand(query, connection);
      await connection.OpenAsync();
      await command.ExecuteNonQueryAsync();
    }

    public DataSet GetDataChanges()
    {
      var sql = $@"DECLARE @last_known_version BIGINT = {_currentVersion};
                  DECLARE @current_version BIGINT = CHANGE_TRACKING_CURRENT_VERSION();
                  DECLARE @table_name NVARCHAR(128);
                  DECLARE table_cursor CURSOR FOR 
                  SELECT t.name FROM sys.change_tracking_tables ct JOIN sys.tables t ON ct.object_id = t.object_id;

                  OPEN table_cursor;
                  FETCH NEXT FROM table_cursor INTO @table_name;
                  WHILE @@FETCH_STATUS = 0
                  BEGIN
                    DECLARE @primaryKey NVARCHAR(50);
                    SELECT @primaryKey = COLUMN_NAME FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
                    WHERE TABLE_NAME = @table_name
                    AND OBJECTPROPERTY(OBJECT_ID(CONSTRAINT_SCHEMA + '.' + CONSTRAINT_NAME), 'IsPrimaryKey') = 1;

                    DECLARE @sql NVARCHAR(MAX) = 'SELECT ''' + @table_name + ''' AS TableName, t.*,  ct.* FROM ' + @table_name + ' AS t RIGHT OUTER JOIN CHANGETABLE(CHANGES ' + @table_name + ', ' + CAST(@last_known_version AS NVARCHAR) + ') AS ct ON t.' + @primaryKey + ' = ct.' + @primaryKey;
                    EXEC sp_executesql @sql;
                    FETCH NEXT FROM table_cursor INTO @table_name;
                  END;

                  CLOSE table_cursor;
                  DEALLOCATE table_cursor;";
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        SqlDataAdapter adapter = new(sql, connection);
        DataSet changeSet = new();
        
        adapter.Fill(changeSet);

        for (int i = changeSet.Tables.Count - 1; i >= 0; i--)
        {
            DataTable table = changeSet.Tables[i];
            if (table.Rows.Count == 0)
            {
                changeSet.Tables.Remove(table);
            }
        }
        
        _logger.LogCritical("Data changes in {count} table(s) from {database}",changeSet.Tables.Count, DbKey);
        return changeSet;

    }

    public async Task UpdateCurrentVersion()
    {
      using SqlConnection connection = new(_connectionString);
      connection.Open();
      using SqlCommand command = new("SELECT CHANGE_TRACKING_CURRENT_VERSION()", connection);
      object result = await command.ExecuteScalarAsync();
      _currentVersion = result != DBNull.Value ? Convert.ToInt64(result) : 0;
      _logger.LogInformation("The current version in {name} is {version}", DbKey, _currentVersion);
    }
  }
}