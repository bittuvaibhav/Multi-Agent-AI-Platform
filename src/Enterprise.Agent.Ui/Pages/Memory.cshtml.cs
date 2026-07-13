using Enterprise.Agent.Models.Memory;
using Enterprise.Agent.Ui.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Enterprise.Agent.Ui.Pages;

public sealed class MemoryModel : PageModel
{
    private readonly PlatformApiClient _api;

    public MemoryModel(PlatformApiClient api) => _api = api;

    [BindProperty] public string? ConversationId { get; set; }

    [BindProperty] public string? RecallQuery { get; set; }

    [BindProperty] public string? MemoryKey { get; set; }

    [BindProperty] public string? MemoryContent { get; set; }

    public IReadOnlyList<MemoryRecord> Conversation { get; private set; } = [];

    public IReadOnlyList<MemoryRecord> Recalled { get; private set; } = [];

    public string? Error { get; private set; }

    public string? Notice { get; private set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostConversationAsync(CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(ConversationId))
        {
            var result = await _api.GetConversationMemoryAsync(ConversationId!, 20, ct);
            if (result.Success) Conversation = result.Value!; else Error = result.Error;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostRecallAsync(CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(RecallQuery))
        {
            var result = await _api.RecallMemoryAsync(RecallQuery!, 10, ct);
            if (result.Success) Recalled = result.Value!; else Error = result.Error;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostRememberAsync(CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(MemoryKey) && !string.IsNullOrWhiteSpace(MemoryContent))
        {
            var result = await _api.RememberAsync(MemoryKey!, MemoryContent!, ConversationId, ct);
            if (result.Success) Notice = $"Stored semantic memory '{MemoryKey}'."; else Error = result.Error;
        }

        return Page();
    }
}
