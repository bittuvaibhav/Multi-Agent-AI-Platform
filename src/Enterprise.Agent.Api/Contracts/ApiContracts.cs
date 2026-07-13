using Enterprise.Agent.Models.Enums;

namespace Enterprise.Agent.Api.Contracts;

/// <summary>Request body for issuing a development JWT.</summary>
public sealed record IssueTokenRequest(string Subject, string[]? Roles = null, string? TenantId = null);

/// <summary>Request body to run a single named agent directly.</summary>
public sealed record RunAgentRequest(string Input, Dictionary<string, string>? Parameters = null);

/// <summary>Request body for planning a goal.</summary>
public sealed record PlanRequest(string Goal);

/// <summary>Request body for a RAG query.</summary>
public sealed record RagQueryRequest(string Query, string? Collection = null, int TopK = 5);

/// <summary>Request body for a SQL agent question.</summary>
public sealed record SqlQuestionRequest(string Question, string? DataSource = null, int MaxRows = 100);

/// <summary>Request body to invoke a tool function directly.</summary>
public sealed record InvokeToolRequest(string Plugin, string Function, Dictionary<string, string>? Arguments = null);

/// <summary>Request body to store a semantic memory.</summary>
public sealed record RememberRequest(string Key, string Content, string? UserId = null, string? ConversationId = null);

/// <summary>Request body to ingest a document supplied as base64 (non-multipart clients).</summary>
public sealed record IngestBase64Request(
    string DocumentId, string FileName, DocumentType DocumentType, string ContentBase64, string? Collection = null);
