using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MonitorService.DataTracking;
using MonitorService.DBDetectors;
using MonitorService.Models;

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
    private readonly ManageDbMaintainer _maintainer;

    public DatabaseWorker(IOptions<AppSettings> appSettings, IServiceScopeFactory scopeFactory, ManageDbMaintainer maintainer, ILogger<DatabaseWorker> logger)
    {
      _appSettings = appSettings.Value;
      _scopeFactory = scopeFactory;
      _logger = logger;
      _maintainer = maintainer;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      _maintainer.InitializeDatabase();
      
      using (var scope = _scopeFactory.CreateScope())
      {
        var detector = scope.ServiceProvider.GetRequiredService<IDbDetector>();
        _logger.LogInformation("Worker running at: {time} with {detector}", DateTimeOffset.Now, detector.GetType().Name);
        var currentEndpoints = Enumerable.Empty<DbEndPoint>();
        var tracers = scope.ServiceProvider.GetServices<IChangeTracer>();
        
        while (!stoppingToken.IsCancellationRequested)
        {
          var endpoints = await detector.GetEndpoints();
          var diffEndpoints = endpoints.Except(currentEndpoints, new DbEndpointComparer());
          if(diffEndpoints.Any()){
            var count = await _maintainer.UpdateEndpoints(diffEndpoints);
            _logger.LogCritical("{count} endpoints are updated", count);
            currentEndpoints = endpoints;
            foreach(var endpoint in diffEndpoints)
            {
              var entries = await _maintainer.GetEntriesOfEndpoint(endpoint.Name);
              foreach(var entry in entries)
              {
                var matchedTracer = tracers.FirstOrDefault(t => t.DbKey == $"{endpoint.Name}.{entry.EntryKey}");
                if(matchedTracer != null)
                {
                  var database = $"{endpoint.Name}.{entry.EntryKey}";
                  if(endpoint.State == 1)
                  {
                    await matchedTracer.EnableDatabaseTracking();
                  }
                }
              }
            }
          }
          await Task.Delay(1_000, stoppingToken);
        }
      }
    }
  }
}