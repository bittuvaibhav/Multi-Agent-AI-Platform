using Enterprise.Agent.Models.Agents;
using Enterprise.Agent.Ui.Options;
using Enterprise.Agent.Ui.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace Enterprise.Agent.Ui.Pages;

public sealed class ChatModel : PageModel
{
    private readonly PlatformApiClient _api;

    public ChatModel(PlatformApiClient api, IOptions<ApiClientOptions> options)
    {
        _api = api;
        HubUrl = options.Value.ResolvePublicBaseUrl().TrimEnd('/') + "/hubs/chat";
    }

    /// <summary>Absolute URL of the API's SignalR chat hub, used by the browser client.</summary>
    public string HubUrl { get; }

    public IReadOnlyList<AgentDescriptor> Agents { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken ct)
    {
        var result = await _api.GetAgentsAsync(ct);
        if (result.Success)
        {
            Agents = result.Value!;
        }
    }
}
