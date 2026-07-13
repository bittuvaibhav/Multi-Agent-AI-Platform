using Enterprise.Agent.Ui.Options;
using Enterprise.Agent.Ui.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace Enterprise.Agent.Ui.Pages;

public sealed class IndexModel : PageModel
{
    private readonly PlatformApiClient _api;

    public IndexModel(PlatformApiClient api, IOptions<ApiClientOptions> options)
    {
        _api = api;
        ScalarUrl = options.Value.ResolvePublicBaseUrl().TrimEnd('/') + "/scalar/v1";
    }

    public bool ApiOnline { get; private set; }

    public string HealthStatus { get; private set; } = "Unknown";

    public int AgentCount { get; private set; }

    public string ScalarUrl { get; }

    public string? Error { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var health = await _api.GetHealthAsync(cancellationToken);
        if (health.Success)
        {
            ApiOnline = true;
            HealthStatus = health.Value.TryGetProperty("status", out var s) ? s.GetString() ?? "Unknown" : "Unknown";
            AgentCount = health.Value.TryGetProperty("agents", out var a) ? a.GetInt32() : 0;
        }
        else
        {
            Error = health.Error;
        }
    }
}
