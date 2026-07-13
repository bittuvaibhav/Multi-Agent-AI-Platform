using Enterprise.Agent.Models.Rag;
using Enterprise.Agent.Ui.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Enterprise.Agent.Ui.Pages;

public sealed class RagModel : PageModel
{
    private readonly PlatformApiClient _api;

    public RagModel(PlatformApiClient api) => _api = api;

    [BindProperty] public string? Query { get; set; }

    [BindProperty] public string? Collection { get; set; }

    [BindProperty] public int TopK { get; set; } = 5;

    [BindProperty] public IFormFile? Upload { get; set; }

    public string? Answer { get; private set; }

    public RagContext? Context { get; private set; }

    public IngestionResult? Ingested { get; private set; }

    public IReadOnlyList<DocumentDto> Documents { get; private set; } = [];

    public string? Error { get; private set; }

    public string? Notice { get; private set; }

    public async Task OnGetAsync(CancellationToken ct) => await LoadDocumentsAsync(ct);

    public async Task<IActionResult> OnPostQueryAsync(CancellationToken ct)
    {
        await LoadDocumentsAsync(ct);
        if (!string.IsNullOrWhiteSpace(Query))
        {
            var result = await _api.RagQueryAsync(Query!, NullIfBlank(Collection), TopK <= 0 ? 5 : TopK, ct);
            if (result.Success) Context = result.Value; else Error = result.Error;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAnswerAsync(CancellationToken ct)
    {
        await LoadDocumentsAsync(ct);
        if (!string.IsNullOrWhiteSpace(Query))
        {
            var result = await _api.RagAnswerAsync(Query!, NullIfBlank(Collection), ct);
            if (result.Success) Answer = result.Value!.Answer; else Error = result.Error;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostIngestAsync(CancellationToken ct)
    {
        if (Upload is null || Upload.Length == 0)
        {
            Error = "Choose a file to ingest.";
        }
        else
        {
            using var ms = new MemoryStream();
            await Upload.CopyToAsync(ms, ct);
            var base64 = Convert.ToBase64String(ms.ToArray());
            var docType = InferDocumentType(Upload.FileName);

            var result = await _api.IngestAsync(
                Guid.NewGuid().ToString("N"), Upload.FileName, docType, base64, NullIfBlank(Collection), ct);

            if (result.Success)
            {
                Ingested = result.Value;
                Notice = $"Ingested '{Upload.FileName}' → {result.Value!.ChunkCount} chunk(s) into '{result.Value.Collection}'.";
            }
            else
            {
                Error = result.Error;
            }
        }

        await LoadDocumentsAsync(ct);
        return Page();
    }

    private async Task LoadDocumentsAsync(CancellationToken ct)
    {
        var docs = await _api.ListDocumentsAsync(null, ct);
        if (docs.Success) Documents = docs.Value!;
    }

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

    // Mirrors Enterprise.Agent.Models.Enums.DocumentType: Text=0, Markdown=1, Pdf=2, Word=3.
    private static int InferDocumentType(string fileName) => Path.GetExtension(fileName).ToLowerInvariant() switch
    {
        ".pdf" => 2,
        ".docx" or ".doc" => 3,
        ".md" or ".markdown" => 1,
        _ => 0
    };
}
