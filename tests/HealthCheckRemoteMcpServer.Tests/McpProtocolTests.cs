using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using FluentAssertions;
using Xunit;
using ModelContextProtocol.Client;

namespace HealthCheckRemoteMcpServer.Tests;

public class McpProtocolTests : IClassFixture<TestFixture>
{
    private readonly HttpClient _client;
    private const string McpPath = "/";
    
    public McpProtocolTests(TestFixture fixture)
    {
        _client = fixture.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost:5000")
        });
        _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
    }

    [Fact]
    public async Task Initialize_ReturnsSessionId()
    {
        /*
        * I implemented this test to demonstrate how the built in MCP client would interact with the server.
        * For remaining tests I will use the built in client to reduce boilerplate and focus on verifying correctness.
        */
        // Arrange
        var initializeRequest = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "initialize",
            @params = new
            {
                protocolVersion = "2024-11-05",
                capabilities = new { },
                clientInfo = new
                {
                    name = "test-client",
                    version = "1.0.0"
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync(McpPath, initializeRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var jsonPayload = SseResponseHelper.ExtractDataJson(responseContent);
        var responseJson = JsonDocument.Parse(jsonPayload).RootElement;

        var initializedNotification = new {
            jsonrpc = "2.0",
            method = "notifications/initialized",
        };
        await _client.PostAsJsonAsync(McpPath, initializedNotification);

        // Assert
        responseJson.GetProperty("jsonrpc").GetString().Should().Be("2.0");
        responseJson.GetProperty("id").GetInt32().Should().Be(1);
        responseJson.TryGetProperty("result", out var result).Should().BeTrue();
        response.Headers.TryGetValues("Mcp-Session-Id", out var sessionIdHeaders).Should().BeTrue();
        sessionIdHeaders.Should().NotBeEmpty();

        result.TryGetProperty("capabilities", out var capabilities).Should().BeTrue();
        capabilities.TryGetProperty("tools", out var toolsCap).Should().BeTrue();

        result.TryGetProperty("serverInfo", out var serverInfo).Should().BeTrue();
        serverInfo.GetProperty("name").GetString().Should().NotBeNullOrEmpty();

    }

    [Fact]
    public async Task MultipleSessions_Isolated()
    {
        // Arrange
        var httpTransport = new HttpClientTransport(new() {
            Endpoint = _client.BaseAddress!,
        });
        var client1 = await  McpClient.CreateAsync(httpTransport);
        var client2 = await McpClient.CreateAsync(httpTransport);
        
        // Assert
        client1.SessionId.Should().NotBeNullOrEmpty();
        client1.SessionId.Should().NotBe(client2.SessionId); // we see different session IDs
    }
}