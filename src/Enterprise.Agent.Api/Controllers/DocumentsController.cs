using Enterprise.Agent.Api.Contracts;
using Enterprise.Agent.Core.Application.Rag;
using Enterprise.Agent.Models.Enums;
using Enterprise.Agent.Models.Rag;
using Enterprise.Agent.Persistence.Entities;
using Enterprise.Agent.Persistence.Repositories;
using Enterprise.Agent.Shared.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Enterprise.Agent.Api.Controllers;

/// <summary>Ingests documents into the knowledge base and lists ingested documents.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = PlatformConstants.Policies.ApiKeyOrJwt)]
public sealed class DocumentsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IDocumentRepository _documents;

    public DocumentsController(ISender sender, IDocumentRepository documents)
    {
        _sender = sender;
        _documents = documents;
    }

    /// <summary>Uploads a document (multipart/form-data) and ingests it.</summary>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(IngestionResult), StatusCodes.Status200OK)]
    [RequestSizeLimit(52_428_800)]
    public async Task<IActionResult> Upload(
        IFormFile file, [FromQuery] string? collection, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "A non-empty file is required." });
        }

        using var memory = new MemoryStream();
        await file.CopyToAsync(memory, cancellationToken);

        var result = await Ingest(
            Guid.NewGuid().ToString("N"), file.FileName, InferType(file.FileName),
            memory.ToArray(), collection, cancellationToken);

        return Ok(result);
    }

    /// <summary>Ingests a document supplied as base64 (for non-multipart clients).</summary>
    [HttpPost("ingest")]
    [ProducesResponseType(typeof(IngestionResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Ingest([FromBody] IngestBase64Request request, CancellationToken cancellationToken)
    {
        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(request.ContentBase64);
        }
        catch (FormatException)
        {
            return BadRequest(new { message = "ContentBase64 is not valid base64." });
        }

        var result = await Ingest(
            request.DocumentId, request.FileName, request.DocumentType, bytes, request.Collection, cancellationToken);
        return Ok(result);
    }

    /// <summary>Lists ingested documents, optionally filtered by collection.</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? collection, CancellationToken cancellationToken)
        => Ok(await _documents.ListAsync(collection, cancellationToken));

    private async Task<IngestionResult> Ingest(
        string id, string fileName, DocumentType type, byte[] content, string? collection, CancellationToken ct)
    {
        var result = await _sender.Send(new IngestDocumentCommand(new DocumentIngestionRequest
        {
            DocumentId = id,
            FileName = fileName,
            DocumentType = type,
            Content = content,
            Collection = collection
        }), ct);

        await _documents.SaveAsync(new DocumentEntity
        {
            Id = result.DocumentId,
            FileName = fileName,
            DocumentType = type,
            Collection = result.Collection,
            ChunkCount = result.ChunkCount,
            CharactersExtracted = result.CharactersExtracted
        }, ct);

        return result;
    }

    private static DocumentType InferType(string fileName) => Path.GetExtension(fileName).ToLowerInvariant() switch
    {
        ".pdf" => DocumentType.Pdf,
        ".docx" or ".doc" => DocumentType.Word,
        ".md" or ".markdown" => DocumentType.Markdown,
        _ => DocumentType.Text
    };
}
