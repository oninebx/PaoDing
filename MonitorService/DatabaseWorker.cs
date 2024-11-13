using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MonitorService.Core;
using MonitorService.Core.Message;
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
    private AppSettings _appSettings;
    private readonly ManageDbMaintainer _maintainer;
    private readonly ChannelMessenger _messenger;

    public DatabaseWorker(ChannelMessenger messenger, IOptionsMonitor<AppSettings> appSettings, IServiceScopeFactory scopeFactory, ManageDbMaintainer maintainer, ILogger<DatabaseWorker> logger)
    {
      _messenger = messenger;
      _appSettings = appSettings.CurrentValue;
      appSettings.OnChange(async settings => 
      {
        var comparer = new WorkingEntryComparer();
        if(!settings.WorkingEntries.Except(_appSettings.WorkingEntries, comparer).Any()
        && !_appSettings.WorkingEntries.Except(settings.WorkingEntries, comparer).Any())
        {
          return;
        }
        _appSettings = settings;
        // Update configured entries to Manage Database
        _logger.LogCritical("OnChange is called, {count} entries.", settings.WorkingEntries.Count());
        var entriesToAdd = settings.WorkingEntries.Select(e => new DbEntry
        {
          KeyName = e.EntryKey,
          EndpointKey = e.EndpointKey,
          ConnectionString = e.ConnectionString,
          IsMonitoring = e.IsMonitoring
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
            IsMonitoring = entry.IsMonitoring
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

          // Scan Monitor Task
          var avaliableTasks = await _maintainer.GetMonitorTasks();

          // Wait for a ongoing Task
          if(avaliableTasks.Any(t => t.State == 1))
          {
            _logger.LogDebug("There is already 1 onging monitor task.");
            continue;
          }

          // Finalise a concluding Task
          var endingTask = avaliableTasks.FirstOrDefault(t => t.State == 2);
          if(endingTask is not null)
          {
            await _messenger.Send(Task2Message(endingTask), stoppingToken);
            endingTask.State = 3;
            await _maintainer.UpdateMonitorTask(endingTask);
          }

          // Pick up a ready task
          var readyTask = avaliableTasks.FirstOrDefault(t => t.State == 0);
          if(readyTask is not null)
          {
            await _messenger.Send(Task2Message(readyTask), stoppingToken);
            readyTask.State = 1;
            await _maintainer.UpdateMonitorTask(readyTask);
          }

          await Task.Delay(2_000, stoppingToken);
        }
      }
    }

    private Func<MonitorTask, TaskMessage> Task2Message = task => new TaskMessage
    {
      Name = task.Name,
      Endpoint = task.Endpoint,
      State = task.State
    };
    private async Task UpdateAndNotifyDbEntryChanges(IEnumerable<DbEntry> entries)
    {
      _logger.LogCritical("Dispatch {count} tracer message", entries.Count());
      await _maintainer.UpdateEntries(entries);

      var messages = entries.Select(entry => new TracerMessage
      {
        DbKey = $"{entry.EndpointKey}.{entry.KeyName}",
        ConnectionString = entry.ConnectionString,
        IsMonitoring = entry.IsMonitoring
      });
      await _messenger.SendBulk(messages, default);
    }

  }
}