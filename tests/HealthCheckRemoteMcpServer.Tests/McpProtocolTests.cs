using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using FluentAssertions;
using Xunit;

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
    public async Task Initialize_ReturnsSessionIdAndTools()
    {
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
    public async Task Handshake_SseStreamResponds()
    {
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

        var initResponse = await _client.PostAsJsonAsync(McpPath, initializeRequest);
        var initContent = await initResponse.Content.ReadAsStringAsync();
        var initJson = JsonDocument.Parse(initContent).RootElement;
        var sessionId = initResponse.Headers.GetValues("Mcp-Session-Id").First();

        // Determine stream URL from initialize result when present, otherwise fall back
        string? streamUrl = null;
        if (initJson.GetProperty("result").TryGetProperty("streamUrl", out var streamUrlProp))
        {
            streamUrl = streamUrlProp.GetString();
        }

        if (string.IsNullOrEmpty(streamUrl) && !string.IsNullOrEmpty(sessionId))
        {
            streamUrl = $"/mcp/stream/{sessionId}"; // fallback to conventional stream path
        }

        // Act: Connect to StreamableHTTP endpoint
        var streamRequest = new HttpRequestMessage(HttpMethod.Get, streamUrl!);
        streamRequest.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/stream+json"));
        var streamResponse = await _client.SendAsync(streamRequest, HttpCompletionOption.ResponseHeadersRead);

        // Assert basic connection
        streamResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Optionally ensure a readable stream is available
        using var stream = await streamResponse.Content.ReadAsStreamAsync();
        stream.CanRead.Should().BeTrue();
        // Try reading a single byte (non-blocking) with small timeout
        var buffer = new byte[1];
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(2000));
        var readTask = stream.ReadAsync(buffer, 0, 1, cts.Token);
        try
        {
            await Task.WhenAny(readTask, Task.Delay(2000, CancellationToken.None));
        }
        catch { }
    }

    [Fact]
    public async Task MultipleSessions_Isolated()
    {
        // Arrange
        var client1 = new TestFixture().CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost:5000")
        });
        var client2 = new TestFixture().CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost:5000")
        });

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

        // Act: Initialize two sessions in parallel
        var task1 = client1.PostAsJsonAsync(McpPath, initializeRequest);
        var task2 = client2.PostAsJsonAsync(McpPath, initializeRequest);

        await Task.WhenAll(task1, task2);

        var response1 = await task1;
        var response2 = await task2;

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        var content1 = await response1.Content.ReadAsStringAsync();
        var content2 = await response2.Content.ReadAsStringAsync();

        var json1 = JsonDocument.Parse(content1).RootElement;
        var json2 = JsonDocument.Parse(content2).RootElement;

        var sessionId1 = json1.GetProperty("result").GetProperty("sessionId").GetString();
        var sessionId2 = json2.GetProperty("result").GetProperty("sessionId").GetString();

        sessionId1.Should().NotBeNullOrEmpty();
        sessionId2.Should().NotBeNullOrEmpty();
        sessionId1.Should().NotBe(sessionId2); // Isolated sessions
    }
}