using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MonitorService.DataTracking;

namespace MonitorService.Core
{
    public class TracerContainer
    {
      private ILogger<TracerContainer> _logger;
      private IDictionary<string, IChangeTracer> _container { get; }

      public TracerContainer(ILogger<TracerContainer> logger)
      {
        _container = new Dictionary<string, IChangeTracer>();
        _logger = logger;
      }

      public void Add(string key, IChangeTracer value)
      {
        if(_container.TryAdd(key, value))
        {
          _logger.LogInformation("A change tracer for {name} is added", key);
        }
        else
        {
          _logger.LogWarning("{key} change tracer has already been added before.", key);
        }
      }

      public void Remove(string key)
      {
        if(_container.Remove(key))
        {
          _logger.LogInformation("A change tracer for {name} is removed", key);
        }
        else
        {
          _logger.LogWarning("{name} change tracer has already been removed before.", key);
        }
      }

      public IChangeTracer Get(string key)
      {
        if(_container.TryGetValue(key, out var tracer))
        {
          return tracer;
        }
        _logger.LogWarning("{name} change tracer does not exist.", key);
        return null;
      }

      public IEnumerable<IChangeTracer> Search(string keyword)
      {
        var keys = _container.Keys.Where(k => k.Contains(keyword));
        foreach(var key in keys)
        {
          yield return _container[key];
        }

      }

      public int Size { get => _container.Count; }
    }
}