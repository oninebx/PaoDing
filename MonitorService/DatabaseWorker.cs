using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MonitorService.DBDetectors;
using MonitorService.DBDetectors.Docker;

namespace MonitorService
{
  public class SqlServerContainer {
    public required string Id { get; set; }
    public required string Image { get; set; }
  }
  public class DatabaseWorker : BackgroundService
  {
    private readonly ILogger<DatabaseWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AppSettings _appSettings;
    private readonly ManageDbInitializer _initializer;

    public DatabaseWorker(IOptions<AppSettings> appSettings, IServiceScopeFactory scopeFactory, ManageDbInitializer initializer, ILogger<DatabaseWorker> logger)
    {
      _appSettings = appSettings.Value;
      _scopeFactory = scopeFactory;
      _logger = logger;
      _initializer = initializer;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      _initializer.InitializeDatabase();
      using (var scope = _scopeFactory.CreateScope())
      {
        var detector = scope.ServiceProvider.GetRequiredService<IDbDetector>();
        _logger.LogInformation("Worker running at: {time} with {detector}", DateTimeOffset.Now, detector.GetType().Name);

        while (!stoppingToken.IsCancellationRequested)
        {
          var sqlEntries = await detector.GetActiveEntries();
          _logger.LogCritical("{count} SQL Server(s) are running in the machine.", sqlEntries.Count());
          await Task.Delay(1_000, stoppingToken);
        }
      }
      
    }
  }
}

/*
using(HttpClient client = new HttpClient()){
        string dockerApiUrl = "http://localhost:2375/containers/json";
        HttpResponseMessage response = await client.GetAsync(dockerApiUrl);

            if (response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync();

                // Deserialize JSON response into a list of DockerContainerInfo objects
                var containers = JsonSerializer.Deserialize<List<SqlServerContainer>>(responseBody);
                var sqlServerContainers = containers?.FindAll(container => container.Image.Contains("mssql"));

                if (sqlServerContainers.Count == 0)
        {
            Console.WriteLine("No SQL Server containers found.");
        }
        else
        {
            Console.WriteLine("SQL Server Containers:");
            foreach (var container in sqlServerContainers)
            {
                Console.WriteLine($"ID: {container.Id}");
                Console.WriteLine($"Image: {container.Image}");
                Console.WriteLine();
            }
        }
                
                // Return the full list of containers
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }
            else
            {
                throw new Exception("Failed to retrieve containers from Docker.");
            }
      }
*/