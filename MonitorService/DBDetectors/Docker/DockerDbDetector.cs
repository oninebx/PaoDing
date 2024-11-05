using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MonitorService.Models;

namespace MonitorService.DBDetectors.Docker
{
    public class DockerDbDetector : IDbDetector
    {
      private readonly string _apiUrl;
      public DockerDbDetector(string url){
        _apiUrl = url;
      }

    public async Task<IEnumerable<SqlServerEntry>> GetActiveEntries()
    {
      using var client = new HttpClient();
      client.BaseAddress = new Uri(_apiUrl);
      var response = await client.GetAsync("/containers/json");
      response.EnsureSuccessStatusCode();

      var containers = await response.Content.ReadFromJsonAsync<IEnumerable<DockerContainer>>();
      return containers?.Where(c => c.Image.Contains("mssql", StringComparison.OrdinalIgnoreCase))
                        .SelectMany(c => c.Names)
                        .Select(n => new SqlServerEntry
                        {
                          Name = n
                        }) ?? Enumerable.Empty<SqlServerEntry>();
    }
  }

  public class DockerContainer
  {
    public required string Id { get; set; }
    public required string Image { get; set; }
    public required IEnumerable<string> Names { get; set;}
  }
}