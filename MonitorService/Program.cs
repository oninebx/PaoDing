using System.Runtime.InteropServices;
using MonitorService;
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
      services.AddSingleton<ManageDbInitializer>();
      
      services.AddHostedService<DatabaseWorker>();
        // services.AddHostedService<Worker>();
    })
    .Build();

host.Run();
