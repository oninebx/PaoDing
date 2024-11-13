using System.Runtime.InteropServices;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MonitorService;
using MonitorService.Core;
using MonitorService.Core.MessageHandlers;
using MonitorService.DataTracking;
using MonitorService.DBDetectors;
using MonitorService.DBDetectors.Docker;
using MonitorService.DBDetectors.Windows;
using MonitorService.Models;

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

      services.AddSingleton<TracerContainer>();
      services.AddSingleton<ManageDbMaintainer>();
      services.AddScoped<TracerMessageHandler>();
      services.AddScoped<TaskMessageHandler>();
      services.AddSingleton<ChannelMessenger>();
      
      services.AddHostedService<DatabaseWorker>();
      services.AddHostedService<ChangeTrackingWorker>();
    })
    .Build();

host.Run();
