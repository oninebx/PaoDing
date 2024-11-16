using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonitorService.Models
{
    public class DbEndPoint
    {
        public required string Name { get; set; }
        public required string Host { get; set; }
        public required int Port { get; set; }
        private string _state = string.Empty;
        public void SetState(string value){
          _state = value;
        }
        public int State 
        { 
          get { return _state switch
            {
              "running" => 1,
              _ => 0
            }; 
          } 
        }
    }

    public class DbEndpointComparer : IEqualityComparer<DbEndPoint>
    {
      public bool Equals(DbEndPoint? x, DbEndPoint? y) 
      {
        return x?.Name == y?.Name && x?.Host == y?.Host && x?.Port == y?.Port && x?.State == y?.State;
      }

      public int GetHashCode(DbEndPoint obj) 
      {
        return HashCode.Combine(obj.Name, obj.Host, obj.Port, obj.State);
      }
    }
}