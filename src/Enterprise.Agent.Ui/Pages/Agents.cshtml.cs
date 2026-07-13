using Enterprise.Agent.Models.Agents;
using Enterprise.Agent.Models.Orchestration;
using Enterprise.Agent.Ui.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Enterprise.Agent.Ui.Pages;

public sealed class AgentsModel : PageModel
{
    private readonly PlatformApiClient _api;

    public AgentsModel(PlatformApiClient api) => _api = api;

    public IReadOnlyList<AgentDescriptor> Agents { get; private set; } = [];

    [BindProperty] public string? Goal { get; set; }

    [BindProperty] public string? SelectedAgent { get; set; }

    [BindProperty] public string? Input { get; set; }

    public WorkflowPlan? Plan { get; private set; }

    public AgentResponse? Result { get; private set; }

    public string? Error { get; private set; }

    public async Task OnGetAsync(CancellationToken ct) => await LoadAgentsAsync(ct);

    public async Task<IActionResult> OnPostPlanAsync(CancellationToken ct)
    {
        await LoadAgentsAsync(ct);
        if (!string.IsNullOrWhiteSpace(Goal))
        {
            var result = await _api.PlanAsync(Goal!, ct);
            if (result.Success) Plan = result.Value; else Error = result.Error;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostRunAsync(CancellationToken ct)
    {
        await LoadAgentsAsync(ct);
        if (!string.IsNullOrWhiteSpace(SelectedAgent) && !string.IsNullOrWhiteSpace(Input))
        {
            var result = await _api.RunAgentAsync(SelectedAgent!, Input!, ct);
            if (result.Success) Result = result.Value; else Error = result.Error;
        }

        return Page();
    }

    private async Task LoadAgentsAsync(CancellationToken ct)
    {
        var result = await _api.GetAgentsAsync(ct);
        if (result.Success) Agents = result.Value!; else Error = result.Error;
    }
}
