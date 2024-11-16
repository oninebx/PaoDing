using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MonitorService.Models;

namespace MonitorService.DBDetectors.Docker
{
    public class DockerDbDetector : IDbDetector
    {
      private readonly string _apiUrl;
      private readonly ManageDbMaintainer _maintainer;
      public DockerDbDetector(string url, ManageDbMaintainer maintainer){
        _apiUrl = url;
        _maintainer = maintainer;
      }

    public async Task<IEnumerable<DbEndPoint>> GetEndpoints()
    {
      using var client = new HttpClient();
      client.BaseAddress = new Uri(_apiUrl);
      // /containers/json?filters={\"status\":[\"running\"]}
      var response = await client.GetAsync("/containers/json?all=true");
      response.EnsureSuccessStatusCode();

      var containers = await response.Content.ReadFromJsonAsync<IEnumerable<DockerContainer>>();
      return containers?.Where(c => c.Image.Contains("mssql", StringComparison.OrdinalIgnoreCase))
                        .SelectMany(c => c.Names.Select((n, i) => 
                        {
                          var isRunning = c.State == "running";
                          var ports = c.Ports.ToList();
                          var point = new DbEndPoint
                          {
                            Name = n.Substring(1),
                            Host = isRunning ? ports[i].IP : string.Empty,
                            Port = isRunning ? ports[i].PublicPort : -1,
                          };
                          point.SetState(c.State);
                          return point;
                        })) ?? Enumerable.Empty<DbEndPoint>();
    }
  }

  public class DockerContainer
  {
    public required string Id { get; set; }
    public required string Image { get; set; }
    public required IEnumerable<string> Names { get; set;}
    public required IEnumerable<ContainerPort> Ports { get; set; }
    public required string State { get; set; }
  }

  public class ContainerPort 
  {
    public required string IP { get; set; }
    public required int PublicPort { get ; set; }
  }
}