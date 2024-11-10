using System.Runtime.InteropServices;
using Microsoft.Data.Sqlite;
using MonitorService;
using MonitorService.Core;
using MonitorService.DataTracking;
using MonitorService.DBDetectors;
using MonitorService.DBDetectors.Docker;
using MonitorService.DBDetectors.Windows;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) => {
      config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
    })
    .ConfigureServices((hostContext, services) =>
    {
      var appsettings = hostContext.Configuration.GetSection("AppSettings");
      services.Configure<AppSettings>(appsettings);
      
      services.AddScoped<IDbDetector>(provider =>
      {
        var maintainer = provider.GetRequiredService<ManageDbMaintainer>();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
          
          return new DockerDbDetector(appsettings["DockerApiUrl"] ?? "", maintainer);
        }
        return new WindowsDbDetector();
      });

      services.AddSingleton<ManageDbMaintainer>();
      services.AddSingleton<ChannelMessenger>();
      
      // var connectionString = hostContext.Configuration.GetConnectionString("ManageDb");
      // try
      // {
      //   using(var connection = new SqliteConnection(connectionString))
      //   {
      //     connection.Open();
      //     var command = connection.CreateCommand();
      //     command.CommandText = 
      //     @"SELECT KeyName, ConnectionString, EndpointKey FROM DbEntry WHERE IsMonitoring = true";
      //     using (var reader = command.ExecuteReader())
      //     {
      //       while (reader.Read())
      //       {
      //         string database = reader.GetString(0);
      //         string entryString = reader.GetString(1);
      //         string endpoint = reader.GetString(2);
      //         services.AddScoped<IChangeTracer>(provider => 
      //         {
      //           var logger = provider.GetRequiredService<ILogger<SqlServerChangeTracker>>();
      //           var maintainer = provider.GetRequiredService<ManageDbMaintainer>();
      //           var tracker = new SqlServerChangeTracker($"{endpoint}.{database}", entryString, logger);
      //           return tracker;
      //         });
      //       }
      //     }
      //   }
      // }
      // catch(Exception e)
      // {
      //   Console.WriteLine(e.Message);
      // }
      
      
      services.AddHostedService<DatabaseWorker>();
      services.AddHostedService<ChangeTrackingWorker>();
    })
    .Build();

host.Run();
