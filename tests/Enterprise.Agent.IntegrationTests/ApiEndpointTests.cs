using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Enterprise.Agent.IntegrationTests.TestInfrastructure;
using Enterprise.Agent.Shared.Constants;
using Xunit;

namespace Enterprise.Agent.IntegrationTests;

public sealed class ApiEndpointTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ApiEndpointTests(ApiFactory factory) => _factory = factory;

    private HttpClient CreateAuthorizedClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(PlatformConstants.ApiKeyHeader, ApiFactory.TestApiKey);
        return client;
    }

    [Fact]
    public async Task Health_ReturnsHealthy()
    {
        var response = await _factory.CreateClient().GetAsync("/api/health");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Healthy", body);
    }

    [Fact]
    public async Task Agents_WithoutCredentials_ReturnsUnauthorized()
    {
        var response = await _factory.CreateClient().GetAsync("/api/agents");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Agents_WithApiKey_ReturnsAllAgents()
    {
        var response = await CreateAuthorizedClient().GetAsync("/api/agents");
        response.EnsureSuccessStatusCode();
        var agents = await response.Content.ReadFromJsonAsync<List<AgentDescriptorDto>>();
        Assert.NotNull(agents);
        Assert.Equal(12, agents!.Count);
    }

    [Fact]
    public async Task Auth_IssuesToken()
    {
        var response = await _factory.CreateClient()
            .PostAsJsonAsync("/api/auth/token", new { subject = "tester", roles = new[] { "user" } });
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<TokenDto>();
        Assert.False(string.IsNullOrWhiteSpace(payload!.AccessToken));
    }

    [Fact]
    public async Task Chat_WithPreferredAgent_ReturnsMockedAnswer()
    {
        var response = await CreateAuthorizedClient().PostAsJsonAsync("/api/chat", new
        {
            message = "Draft a note.",
            preferredAgent = "WriterAgent"
        });

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<ChatResponseDto>();
        Assert.Equal(FakeKernelFactory.CannedResponse, payload!.Answer);
        Assert.Contains("WriterAgent", payload.AgentsInvoked);
    }

    [Fact]
    public async Task Tools_WithApiKey_ListsCalculator()
    {
        var response = await CreateAuthorizedClient().GetAsync("/api/tools");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Calculator", body);
    }

    private sealed record AgentDescriptorDto(string Name, string Description);
    private sealed record TokenDto(string AccessToken, DateTimeOffset ExpiresAt, string TokenType);
    private sealed record ChatResponseDto(string ConversationId, string Answer, string[] AgentsInvoked);
}
