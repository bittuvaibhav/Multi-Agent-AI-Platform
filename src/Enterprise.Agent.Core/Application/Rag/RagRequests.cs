using Enterprise.Agent.Core.Abstractions.Rag;
using Enterprise.Agent.Models.Rag;
using FluentValidation;
using MediatR;

namespace Enterprise.Agent.Core.Application.Rag;

/// <summary>Ingests a document into the knowledge base.</summary>
public sealed record IngestDocumentCommand(DocumentIngestionRequest Request) : IRequest<IngestionResult>;

public sealed class IngestDocumentCommandValidator : AbstractValidator<IngestDocumentCommand>
{
    public IngestDocumentCommandValidator()
    {
        RuleFor(x => x.Request).NotNull();
        RuleFor(x => x.Request.DocumentId).NotEmpty();
        RuleFor(x => x.Request.FileName).NotEmpty();
        RuleFor(x => x.Request.Content).NotNull().Must(c => c.Length > 0).WithMessage("Document content is empty.");
    }
}

public sealed class IngestDocumentCommandHandler : IRequestHandler<IngestDocumentCommand, IngestionResult>
{
    private readonly IRagService _rag;

    public IngestDocumentCommandHandler(IRagService rag) => _rag = rag;

    public Task<IngestionResult> Handle(IngestDocumentCommand command, CancellationToken cancellationToken) =>
        _rag.IngestAsync(command.Request, cancellationToken);
}

/// <summary>Retrieves grounded context from the knowledge base for a query.</summary>
public sealed record QueryKnowledgeBaseQuery(string Query, string? Collection, int TopK = 5)
    : IRequest<RagContext>;

public sealed class QueryKnowledgeBaseQueryValidator : AbstractValidator<QueryKnowledgeBaseQuery>
{
    public QueryKnowledgeBaseQueryValidator()
    {
        RuleFor(x => x.Query).NotEmpty().MaximumLength(4_000);
        RuleFor(x => x.TopK).InclusiveBetween(1, 50);
    }
}

public sealed class QueryKnowledgeBaseQueryHandler : IRequestHandler<QueryKnowledgeBaseQuery, RagContext>
{
    private readonly IRagService _rag;

    public QueryKnowledgeBaseQueryHandler(IRagService rag) => _rag = rag;

    public Task<RagContext> Handle(QueryKnowledgeBaseQuery query, CancellationToken cancellationToken) =>
        _rag.RetrieveAsync(query.Query, query.Collection, query.TopK, cancellationToken);
}
