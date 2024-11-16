using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MonitorService.Core.Message;
using MonitorService.DataTracking;
using MonitorService.Models;

namespace MonitorService.Core.MessageHandlers
{
  public class TracerMessageHandler : MessageHandler<TracerBackMessage, TracerMessageHandler, TracerMessage>
  {
    private ILogger<SqlServerChangeTracker> _tracerLogger;
    private TracerContainer _container;
    public TracerMessageHandler(TracerContainer container, ILogger<TracerMessageHandler> logger, ILogger<SqlServerChangeTracker> tracerLogger) : base(logger)
    {
      _tracerLogger= tracerLogger;
      _container = container;
    }

    public override async Task<TracerBackMessage> Handle(TracerMessage message)
    {
      bool isActive = message.IsMonitoring;
      _logger.LogCritical("Receive message - {dbKey} connects via {connectionString}", message.DbKey, message.ConnectionString);
      
      var tracer = _container.Get(message.DbKey);
      if(message.IsMonitoring)
      {
        if(tracer == null)
        {
          tracer = new SqlServerChangeTracker(message.DbKey, message.ConnectionString, _tracerLogger);
          _container.Add(message.DbKey, tracer);
        }
        if(!await EnsureStartTracking(tracer))
        {
          _container.Remove(message.DbKey);
          isActive = false;
        }
        
      }
      else
      {
        if(tracer != null)
        {
          try
          {
            await tracer.DisableDatabaseTracking();
          }
          catch(Exception e)
          {
            _logger.LogError(e.Message);
          }
          _container.Remove(tracer.DbKey);
        }
      }
      _logger.LogCritical("Tracers Update - {count} tracers are working.", _container.Size);
      return new TracerBackMessage {DbKey = message.DbKey, IsActive = isActive };
    }

    private async Task<bool> EnsureStartTracking(IChangeTracer tracer)
    {
      try
      {
        await tracer.DisableDatabaseTracking();
      }
      catch(Exception e)
      {
        _logger.LogError(e.Message);
      }
      try
      {
        await tracer.EnableDatabaseTracking();
      }
      catch(Exception e)
      {
        _logger.LogError(e.Message);
        return false;
      }
      return true;
    }
  }
}