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
      private readonly string? _connectionString;
      private readonly ILogger<ManageDbMaintainer> _logger;
      public ManageDbMaintainer(IConfiguration configure, ILogger<ManageDbMaintainer> logger)
      {
        _connectionString = configure.GetConnectionString("ManageDb");
        _logger = logger;
      }
        public void InitializeDatabase()
        {
          using (var connection = new SqliteConnection(_connectionString))
          {
            connection.Open();
            EnsureVersionTableExists(connection);

            int currentVersion = GetSchemaVersion(connection);

            string[] files = Directory.GetFiles("Database");
            int updatedVersion = currentVersion;
            foreach (string file in files)
            {
                var version = Convert.ToInt32(Path.GetFileNameWithoutExtension(file));
                if(version > currentVersion){
                  var sqlScript = File.ReadAllText(file);
                  using var command = new SqliteCommand(sqlScript, connection);
                  command.ExecuteNonQuery();
                  updatedVersion = Math.Max(version, updatedVersion);
                }
            }
            if(updatedVersion > currentVersion){
              SetSchemaVersion(connection, updatedVersion);
              _logger.LogInformation("Initialize manage database to version {version}", updatedVersion);
            }
          }
        }

        // public async Task<string> GetConnectionString(string database)
        // {
        //   using (var connection = new SqliteConnection(_connectionString))
        //   {
        //     connection.Open();
        //     using(var command = connection.CreateCommand())
        //     {
        //       command.CommandText = "SELECT Host, Port, Username, PasswordHash From DbEndpoint WHERE DBKey = @database";
        //       command.Parameters.AddWithValue("@database", database);
        //       using var reader = await command.ExecuteReaderAsync();
        //       if(await reader.ReadAsync()){
        //         string host = reader.GetString(0);
        //         string port = reader.GetString(1);
        //         string username = reader.GetString(2);
        //         string password = reader.GetString(3);
        //         return $"Server={host},{port};Database={database};User Id={username};Password={password};";
        //       }
        //     }
        //     return string.Empty;
        //   }
        // }

        public async Task<IEnumerable<DbEntry>> GetEntriesOfEndpoint(string endpoint)
        {
          using(var connection = new SqliteConnection(_connectionString))
          {
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = 
            @"SELECT de.EntryKey, dp.Host, dp.Port, de.UserName, de.Password FROM 
            DbEntry de  JOIN DbEndpoint dp 
            ON (de.EndpointId = dp.Id) WHERE dp.EndpointKey = @endpoint";
            command.Parameters.AddWithValue("@endpoint", endpoint);
            var entries = new List<DbEntry>();
            using (var reader = await command.ExecuteReaderAsync())
            {
              while (reader.Read())
              {
                string database = reader.GetString(0);
                string host = reader.GetString(1);
                int port = reader.GetInt32(2);
                string username = reader.GetString(3);
                string password = reader.GetString(4);
                var entry = new DbEntry 
                {
                  EntryKey = database,
                  UserName = username,
                  Password = password,
                  IsMonitored = false
                };
                entries.Add(entry);
              }
            }
            return entries;
          }
        }

        public async Task<int> UpdateEndpoints(IEnumerable<DbEndPoint> endPoints)
        {
          int result = 0;
          var sql = @"INSERT INTO DbEndpoint (EndpointKey, Host, Port, State)
                      VALUES (@endpointKey, @host, @port, @state)
                      ON CONFLICT(EndpointKey) DO UPDATE SET
                      State = @state;";
          var sqlForRunning = @"INSERT INTO DbEndpoint (EndpointKey, Host, Port, State)
                      VALUES (@endpointKey, @host, @port, @state)
                      ON CONFLICT(EndpointKey) DO UPDATE SET
                      Host = @host, Port=@port, State = @state;";
          using(var connection = new SqliteConnection(_connectionString))
          {
              connection.Open();
              foreach(var endpoint in endPoints)
              {
                var command = connection.CreateCommand();
                command.CommandText = endpoint.State == 1 ? sqlForRunning : sql;
                command.Parameters.AddWithValue("@endpointKey", endpoint.Name);
                command.Parameters.AddWithValue("@host", endpoint.Host == "0.0.0.0" ? "localhost" : endpoint.Host);
                command.Parameters.AddWithValue("@port", endpoint.Port);
                command.Parameters.AddWithValue("@state", endpoint.State);
                result += await command.ExecuteNonQueryAsync();
                _logger.LogInformation("{name} -> {host}:{port} is {state}", endpoint.Name, endpoint.Host, endpoint.Port, endpoint.State);
              }
          }
          
          return result;
        }
        private void EnsureVersionTableExists(SqliteConnection connection)
        {
          var command = connection.CreateCommand();
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
          var command = connection.CreateCommand();
          command.CommandText = "SELECT Version FROM SchemaVersion LIMIT 1";
          return Convert.ToInt32(command.ExecuteScalar());
        }

      private void SetSchemaVersion(SqliteConnection connection, int version)
      {
          var command = connection.CreateCommand();
          command.CommandText = "UPDATE SchemaVersion SET Version = @version";
          command.Parameters.AddWithValue("@version", version);
          command.ExecuteNonQuery();
      }
    }
}