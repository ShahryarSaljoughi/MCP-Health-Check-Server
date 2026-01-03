using System.ComponentModel;
using System.Text.Json;
using System.Diagnostics;
using ModelContextProtocol.Server;

namespace HealthCheckRemoteMcpServer.Tools;

[McpServerToolType]
public class HealthCheckTool
{
    [McpServerTool(Name = "check_api_status"), Description("Checks the status of an API endpoint by performing an HTTP probe.")]
    public async Task<string> CheckApiStatus(string url)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var client = new HttpClient();
            var response = await client.GetAsync(url);
            stopwatch.Stop();
            var result = new
            {
                status = "UP",
                http_status_code = (int)response.StatusCode,
                latency_ms = stopwatch.ElapsedMilliseconds,
                timestamp = DateTime.UtcNow.ToString("o"),
                error_details = (string?)null
            };
            return JsonSerializer.Serialize(result);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var result = new
            {
                status = "DOWN",
                http_status_code = (int?)null,
                latency_ms = (long?)null,
                timestamp = DateTime.UtcNow.ToString("o"),
                error_details = ex.Message
            };
            return JsonSerializer.Serialize(result);
        }
    }
}
