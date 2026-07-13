using Enterprise.Agent.Models.Tools;
using Enterprise.Agent.Ui.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Enterprise.Agent.Ui.Pages;

public sealed class ToolsModel : PageModel
{
    private readonly PlatformApiClient _api;

    public ToolsModel(PlatformApiClient api) => _api = api;

    public IReadOnlyList<ToolDescriptor> Tools { get; private set; } = [];

    [BindProperty] public string? Plugin { get; set; }

    [BindProperty] public string? Function { get; set; }

    [BindProperty] public string? ArgumentsJson { get; set; }

    public ToolResult? Result { get; private set; }

    public string? Error { get; private set; }

    public async Task OnGetAsync(CancellationToken ct) => await LoadAsync(ct);

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        await LoadAsync(ct);
        if (string.IsNullOrWhiteSpace(Plugin) || string.IsNullOrWhiteSpace(Function))
        {
            Error = "Plugin and function are required.";
            return Page();
        }

        Dictionary<string, string> args;
        try
        {
            args = string.IsNullOrWhiteSpace(ArgumentsJson)
                ? new Dictionary<string, string>()
                : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(ArgumentsJson!) ?? new();
        }
        catch (System.Text.Json.JsonException)
        {
            Error = "Arguments must be a JSON object of string values, e.g. {\"expression\":\"2+2\"}.";
            return Page();
        }

        var result = await _api.InvokeToolAsync(Plugin!, Function!, args, ct);
        if (result.Success) Result = result.Value; else Error = result.Error;
        return Page();
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        var result = await _api.GetToolsAsync(ct);
        if (result.Success) Tools = result.Value!; else Error = result.Error;
    }
}
