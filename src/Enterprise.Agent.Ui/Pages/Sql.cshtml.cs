using Enterprise.Agent.Models.Sql;
using Enterprise.Agent.Ui.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Enterprise.Agent.Ui.Pages;

public sealed class SqlModel : PageModel
{
    private readonly PlatformApiClient _api;

    public SqlModel(PlatformApiClient api) => _api = api;

    [BindProperty] public string? Question { get; set; }

    [BindProperty] public string? DataSource { get; set; }

    [BindProperty] public int MaxRows { get; set; } = 100;

    public SqlAgentResult? Result { get; private set; }

    public string? Error { get; private set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(Question))
        {
            var result = await _api.SqlQueryAsync(Question!, DataSource, MaxRows <= 0 ? 100 : MaxRows, ct);
            if (result.Success) Result = result.Value; else Error = result.Error;
        }

        return Page();
    }
}
