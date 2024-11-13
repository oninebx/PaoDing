using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using MonitorService.Models;

namespace MonitorService
{
    public class ManageDbMaintainer
    {
      private readonly string _connectionString;
      private readonly ILogger<ManageDbMaintainer> _logger;
      public ManageDbMaintainer(IConfiguration configure, ILogger<ManageDbMaintainer> logger)
      {
        _connectionString = configure.GetConnectionString("ManageDb");
        _logger = logger;
      }
      public void InitializeDatabase()
      {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        EnsureVersionTableExists(connection);

        int currentVersion = GetSchemaVersion(connection);

        string[] files = Directory.GetFiles("Database");
        int updatedVersion = currentVersion;
        foreach (string file in files)
        {
          var version = Convert.ToInt32(Path.GetFileNameWithoutExtension(file));
          if (version > currentVersion)
          {
            var sqlScript = File.ReadAllText(file);
            using var command = new SqliteCommand(sqlScript, connection);
            command.ExecuteNonQuery();
            updatedVersion = Math.Max(version, updatedVersion);
          }
        }
        if (updatedVersion > currentVersion)
        {
          SetSchemaVersion(connection, updatedVersion);
          _logger.LogInformation("Initialize manage database to version {version}", updatedVersion);
        }
      }

        public async Task<IEnumerable<DbEntry>> GetEntriesOfEndpoint(string endpoint)
        {
          using var connection = new SqliteConnection(_connectionString);
          connection.Open();
          using var command = connection.CreateCommand();
          command.CommandText =
          @"SELECT de.KeyName, de.ConnectionString, de.IsMonitoring, de.EndpointKey FROM 
                DbEntry de JOIN DbEndpoint dp 
                ON (de.EndpointKey = dp.KeyName) WHERE dp.KeyName = @endpoint";
          command.Parameters.AddWithValue("@endpoint", endpoint);
          using var reader = await command.ExecuteReaderAsync();
          return ParseEntriesFromReader(reader);
        }

        public async Task<IEnumerable<DbEntry>> GetAvailableEntries()
        {
          using var connection = new SqliteConnection(_connectionString);
          connection.Open();
          using var command = connection.CreateCommand();
          command.CommandText =
          @"SELECT KeyName, ConnectionString, IsMonitoring, EndpointKey FROM 
                DbEntry WHERE IsMonitoring = true;";
          using var reader = await command.ExecuteReaderAsync();
          return ParseEntriesFromReader(reader);
        }

        public async Task<int> UpdateEndpoints(IEnumerable<DbEndPoint> endPoints)
        {
          int result = 0;
          var sql = @"INSERT INTO DbEndpoint (KeyName, Host, Port, State)
                      VALUES (@keyName, @host, @port, @state)
                      ON CONFLICT(KeyName) DO UPDATE SET
                      State = @state;";
          var sqlForRunning = @"INSERT INTO DbEndpoint (KeyName, Host, Port, State)
                      VALUES (@keyName, @host, @port, @state)
                      ON CONFLICT(KeyName) DO UPDATE SET
                      Host = @host, Port=@port, State = @state;";
          using var connection = new SqliteConnection(_connectionString);
          connection.Open();
          using var transaction = connection.BeginTransaction();
          foreach(var endpoint in endPoints)
          {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = endpoint.State == 1 ? sqlForRunning : sql;
            command.Parameters.AddWithValue("@keyName", endpoint.Name);
            command.Parameters.AddWithValue("@host", endpoint.Host == "0.0.0.0" ? "localhost" : endpoint.Host);
            command.Parameters.AddWithValue("@port", endpoint.Port);
            command.Parameters.AddWithValue("@state", endpoint.State);
            result += await command.ExecuteNonQueryAsync();
            _logger.LogInformation("{name} -> {host}:{port} is {state}", endpoint.Name, endpoint.Host, endpoint.Port, endpoint.State);
          }
          transaction.Commit();
          
          return result;
        }
        public async Task<int> UpdateEntries(IEnumerable<DbEntry> entries)
        {
          int result = 0;
          var sql = @"INSERT INTO DbEntry (KeyName, EndpointKey, ConnectionString, IsMonitoring)
                      VALUES (@keyName, @endpointKey, @connectionString, @isMonitoring)
                      ON CONFLICT(EndpointKey, KeyName) DO UPDATE SET
                      ConnectionString = @connectionString, IsMonitoring = @isMonitoring;";
          using var connection = new SqliteConnection(_connectionString);
          connection.Open();
          using var transaction = connection.BeginTransaction();
          foreach(var entry in entries)
          {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            command.Parameters.AddWithValue("@keyName", entry.KeyName);
            command.Parameters.AddWithValue("@endpointKey", entry.EndpointKey);
            command.Parameters.AddWithValue("@connectionString", entry.ConnectionString);
            command.Parameters.AddWithValue("@isMonitoring", entry.IsMonitoring);
            result += await command.ExecuteNonQueryAsync();
            _logger.LogInformation("{endpoint}.{entry} is added or updated", entry.EndpointKey, entry.KeyName);
          }
          transaction.Commit();
          
          return result;
        }
        public async Task<IEnumerable<MonitorTask>> GetMonitorTasks()
        {
          var sql = @"SELECT * FROM MonitorTask WHERE TaskState IN (0, 1, 2)
                      GROUP BY TaskState
                      ORDER BY TaskState, CreatedAt;";

          using var connection = new SqliteConnection(_connectionString);
          connection.Open();
          using var command = connection.CreateCommand();
          command.CommandText = sql;
          using var reader = await command.ExecuteReaderAsync();

          IList<MonitorTask> result = new List<MonitorTask>();
          while(await reader.ReadAsync())
          {
            var task = new MonitorTask
            {
              Id = reader.GetInt32(0),
              Name = reader.GetString(1),
              Endpoint = reader.GetString(2),
              State = reader.GetInt32(3),
              CreatedAt = reader.GetDateTime(4)
            };
            result.Add(task);
          }
          return result;
        }

        public async Task UpdateMonitorTasks(IEnumerable<MonitorTask> tasks)
        {
          var sql = @"UPDATE MonitorTask SET TaskState = @state WHERE Id = @id";
          using var connection = new SqliteConnection(_connectionString);
          connection.Open();
          using var transaction = connection.BeginTransaction();
          foreach(var task in tasks)
          {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.Parameters.AddWithValue("@state", task.State);
            command.Parameters.AddWithValue("@id", task.Id);
            await command.ExecuteNonQueryAsync();
          }
          transaction.Commit();
        }

        public async Task UpdateMonitorTask(MonitorTask task)
        {
          var sql = @"UPDATE MonitorTask SET TaskState = @state WHERE Id = @id";
          using var connection = new SqliteConnection(_connectionString);
          connection.Open();
          using var command = connection.CreateCommand();
          command.CommandText = sql;
          command.Parameters.AddWithValue("@state", task.State);
          command.Parameters.AddWithValue("@id", task.Id);
          await command.ExecuteNonQueryAsync();
        }
        private void EnsureVersionTableExists(SqliteConnection connection)
        {
          using var command = connection.CreateCommand();
          command.CommandText = @"
              CREATE TABLE IF NOT EXISTS SchemaVersion (
                  Version INTEGER NOT NULL
              );
          ";
          command.ExecuteNonQuery();

          // Insert initial version if the table is empty
          command.CommandText = "SELECT COUNT(*) FROM SchemaVersion";
          var count = (long)(command.ExecuteScalar() ?? 0);
          if (count == 0)
          {
              command.CommandText = "INSERT INTO SchemaVersion (Version) VALUES (0)";
              command.ExecuteNonQuery();
          }
        }

        private int GetSchemaVersion(SqliteConnection connection)
        {
          using var command = connection.CreateCommand();
          command.CommandText = "SELECT Version FROM SchemaVersion LIMIT 1";
          return Convert.ToInt32(command.ExecuteScalar());
        }

      private void SetSchemaVersion(SqliteConnection connection, int version)
      {
          using var command = connection.CreateCommand();
          command.CommandText = "UPDATE SchemaVersion SET Version = @version";
          command.Parameters.AddWithValue("@version", version);
          command.ExecuteNonQuery();
      }

      private IEnumerable<DbEntry> ParseEntriesFromReader(SqliteDataReader reader)
      {
        var entries = new List<DbEntry>();
        while (reader.Read())
        {
          string database = reader.GetString(0);
          string connectionString = reader.GetString(1);
          bool isMonitoring = reader.GetBoolean(2);
          string endpoint = reader.GetString(3);
          var entry = new DbEntry 
          {
            KeyName = database,
            EndpointKey = endpoint,
            ConnectionString = connectionString,
            IsMonitoring = isMonitoring
          };
          entries.Add(entry);
        }
        return entries;
      }
    }
}