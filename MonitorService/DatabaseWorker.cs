using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MonitorService.Core;
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
    private readonly ChannelMessenger _messenger;

    public DatabaseWorker(ChannelMessenger messenger, IOptionsMonitor<AppSettings> appSettings, IServiceScopeFactory scopeFactory, ManageDbMaintainer maintainer, ILogger<DatabaseWorker> logger)
    {
      _messenger = messenger;
      _appSettings = appSettings.CurrentValue;
      appSettings.OnChange(async settings => 
      {
        // Update configured entries to Manage Database
        var entriesToAdd = settings.WorkingEntries.Select(e => new DbEntry
        {
          KeyName = e.EntryKey,
          EndpointKey = e.EndpointKey,
          ConnectionString = e.ConnectionString,
          IsMonitoring = true
        });
        await UpdateAndNotifyDbEntryChanges(entriesToAdd);
      });

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
        
        var currentEndpoints = await detector.GetEndpoints();
        var count = await _maintainer.UpdateEndpoints(currentEndpoints);
        _logger.LogCritical("Update {count} Database Endpoints", count);

        // Scan Database Entries from Configuration and ManageDB
        var configuredEntries = _appSettings.WorkingEntries.Select(entry => 
          new DbEntry
          {
            KeyName = entry.EntryKey,
            EndpointKey = entry.EndpointKey,
            ConnectionString = entry.ConnectionString,
            IsMonitoring = true
          });
        var savedEntries = await _maintainer.GetAvailableEntries();
        var diffEntries = savedEntries.Except(configuredEntries, new DbEntryComparer());
        var availableEntries = configuredEntries.Concat(diffEntries);
        await UpdateAndNotifyDbEntryChanges(availableEntries);

        while (!stoppingToken.IsCancellationRequested)
        {
          // Scan Database Endpoints
          var endpoints = await detector.GetEndpoints();
          var diffEndpoints = endpoints.Except(currentEndpoints, new DbEndpointComparer());
          if(diffEndpoints.Any())
          {
            count = await _maintainer.UpdateEndpoints(diffEndpoints);
            _logger.LogCritical("Update {count} Database Endpoints", count);
            currentEndpoints = endpoints;
          }

          await Task.Delay(2_000, stoppingToken);
        }
      }
    }

    private async Task UpdateAndNotifyDbEntryChanges(IEnumerable<DbEntry> entries)
    {
      await _maintainer.UpdateEntries(entries);

      var messages = entries.Select(entry => new Message
      {
        DbKey = $"{entry.EndpointKey}.{entry.KeyName}",
        ConnectionString = entry.ConnectionString
      });
      await _messenger.SendBulk(messages, default);
    }

  }
}