namespace Enterprise.Agent.Ui.Services;

/// <summary>Access token returned by the API's /api/auth/token endpoint.</summary>
public sealed record TokenDto(string AccessToken, DateTimeOffset ExpiresAt, string TokenType);

/// <summary>Document metadata returned by /api/documents.</summary>
public sealed record DocumentDto(
    string Id,
    string FileName,
    int DocumentType,
    string Collection,
    int ChunkCount,
    int CharactersExtracted,
    DateTimeOffset IngestedAt);

/// <summary>Answer payload returned by /api/rag/answer.</summary>
public sealed record RagAnswerDto(string Query, string Answer);
