using Enterprise.Agent.Ui.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Enterprise.Agent.Ui.Pages;

public sealed class AuthModel : PageModel
{
    private readonly PlatformApiClient _api;

    public AuthModel(PlatformApiClient api) => _api = api;

    [BindProperty] public string Subject { get; set; } = "ui-user";

    [BindProperty] public string Roles { get; set; } = "operator";

    public TokenDto? Token { get; private set; }

    public string? Error { get; private set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        var roles = Roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var result = await _api.IssueTokenAsync(
            string.IsNullOrWhiteSpace(Subject) ? "ui-user" : Subject,
            roles.Length == 0 ? ["user"] : roles, ct);

        if (result.Success) Token = result.Value; else Error = result.Error;
        return Page();
    }
}
