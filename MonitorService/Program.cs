using System.Runtime.InteropServices;
using Microsoft.Data.Sqlite;
using MonitorService;
using MonitorService.DataTracking;
using MonitorService.DBDetectors;
using MonitorService.DBDetectors.Docker;
using MonitorService.DBDetectors.Windows;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
      
      var appsettings = hostContext.Configuration.GetSection("AppSettings");
      services.Configure<AppSettings>(appsettings);
      
      
      services.AddScoped<IDbDetector>(serviceProvider =>
      {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
          
          return new DockerDbDetector(appsettings["DockerApiUrl"] ?? "");
        }
        return new WindowsDbDetector();
      });

      services.AddSingleton<ManageDbMaintainer>();
      
      var connectionString = hostContext.Configuration.GetConnectionString("ManageDb");
      try
      {
        using(var connection = new SqliteConnection(connectionString))
        {
          connection.Open();
          var command = connection.CreateCommand();
          command.CommandText = 
          @"SELECT de.EntryKey, dp.Host, dp.Port, de.UserName, de.Password, dp.EndpointKey FROM 
          DbEntry de JOIN DbEndpoint dp 
          ON (de.EndpointId = dp.Id)";
          using (var reader = command.ExecuteReader())
          {
            while (reader.Read())
            {
              string database = reader.GetString(0);
              string host = reader.GetString(1);
              int port = reader.GetInt32(2);
              string username = reader.GetString(3);
              string password = reader.GetString(4);
              string endpoint = reader.GetString(5);
              string entryString = $"Server={host},{port};Database={database};User Id={username};Password={password};TrustServerCertificate=true;";
              services.AddScoped<IChangeTracer>(provider => 
              {
                var logger = provider.GetRequiredService<ILogger<SqlServerChangeTracker>>();
                var maintainer = provider.GetRequiredService<ManageDbMaintainer>();
                var tracker = new SqlServerChangeTracker($"{endpoint}.{database}", entryString, maintainer, logger);
                return tracker;
              });
            }
          }
        }
      }
      catch(Exception e)
      {
        Console.WriteLine(e.Message);
      }
      
      
      services.AddHostedService<DatabaseWorker>();
      services.AddHostedService<ChangeTrackingWorker>();
    })
    .Build();

host.Run();
