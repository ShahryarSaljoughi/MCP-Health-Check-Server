using System.IO;
using System.Text;
namespace HealthCheckRemoteMcpServer.Tests;

internal static class SseResponseHelper
{
    public static string ExtractDataJson(string ssePayload)
    {
        if (string.IsNullOrWhiteSpace(ssePayload)) return ssePayload;
        var sb = new StringBuilder();
        using var reader = new StringReader(ssePayload);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (line.StartsWith("data:"))
            {
                var rest = line.Length > 5 ? line.Substring(5) : string.Empty;
                if (rest.Length > 0 && rest[0] == ' ') rest = rest.Substring(1);
                sb.AppendLine(rest);
            }
        }
        var data = sb.ToString().Trim();
        return string.IsNullOrEmpty(data) ? ssePayload : data;
    }
}
