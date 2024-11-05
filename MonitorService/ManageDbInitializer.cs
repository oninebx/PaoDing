using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace MonitorService
{
    public class ManageDbInitializer
    {
      private readonly string? _connectionString;
      private readonly ILogger<ManageDbInitializer> _logger;
      public ManageDbInitializer(IConfiguration configure, ILogger<ManageDbInitializer> logger)
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