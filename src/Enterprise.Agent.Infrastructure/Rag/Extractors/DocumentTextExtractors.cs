using System.Text;
using DocumentFormat.OpenXml.Packaging;
using Enterprise.Agent.Core.Abstractions.Rag;
using Enterprise.Agent.Models.Enums;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;

namespace Enterprise.Agent.Infrastructure.Rag.Extractors;

/// <summary>Extracts text from plain-text and Markdown documents.</summary>
public sealed class PlainTextExtractor : IDocumentTextExtractor
{
    public bool CanHandle(DocumentType documentType) =>
        documentType is DocumentType.Text or DocumentType.Markdown;

    public Task<string> ExtractAsync(byte[] content, DocumentType documentType, CancellationToken cancellationToken = default) =>
        Task.FromResult(Encoding.UTF8.GetString(content));
}

/// <summary>Extracts text from PDF documents using PdfPig.</summary>
public sealed class PdfTextExtractor : IDocumentTextExtractor
{
    private readonly ILogger<PdfTextExtractor> _logger;

    public PdfTextExtractor(ILogger<PdfTextExtractor> logger) => _logger = logger;

    public bool CanHandle(DocumentType documentType) => documentType == DocumentType.Pdf;

    public Task<string> ExtractAsync(byte[] content, DocumentType documentType, CancellationToken cancellationToken = default)
    {
        var builder = new StringBuilder();
        using var document = PdfDocument.Open(content);
        foreach (var page in document.GetPages())
        {
            cancellationToken.ThrowIfCancellationRequested();
            builder.AppendLine(page.Text);
            builder.AppendLine();
        }

        return Task.FromResult(builder.ToString().Trim());
    }
}

/// <summary>Extracts text from Word (.docx) documents using the Open XML SDK.</summary>
public sealed class WordTextExtractor : IDocumentTextExtractor
{
    public bool CanHandle(DocumentType documentType) => documentType == DocumentType.Word;

    public Task<string> ExtractAsync(byte[] content, DocumentType documentType, CancellationToken cancellationToken = default)
    {
        using var stream = new MemoryStream(content);
        using var document = WordprocessingDocument.Open(stream, false);
        var body = document.MainDocumentPart?.Document?.Body;
        var text = body?.InnerText ?? string.Empty;
        return Task.FromResult(text.Trim());
    }
}
