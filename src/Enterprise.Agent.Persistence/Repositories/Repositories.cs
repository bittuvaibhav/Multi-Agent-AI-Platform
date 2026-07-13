using Enterprise.Agent.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Enterprise.Agent.Persistence.Repositories;

/// <summary>Persistence operations for conversations and their messages.</summary>
public interface IConversationRepository
{
    Task<ConversationEntity?> GetAsync(string id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConversationEntity>> ListAsync(string? userId, CancellationToken cancellationToken = default);

    Task UpsertAsync(ConversationEntity conversation, CancellationToken cancellationToken = default);

    Task AddMessageAsync(MessageEntity message, CancellationToken cancellationToken = default);
}

/// <summary>Persistence operations for orchestration audit records.</summary>
public interface IExecutionHistoryRepository
{
    Task SaveAsync(AgentExecutionEntity execution, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AgentExecutionEntity>> ListRecentAsync(int limit = 50, CancellationToken cancellationToken = default);
}

/// <summary>Persistence operations for ingested document metadata.</summary>
public interface IDocumentRepository
{
    Task SaveAsync(DocumentEntity document, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DocumentEntity>> ListAsync(string? collection, CancellationToken cancellationToken = default);
}

public sealed class ConversationRepository : IConversationRepository
{
    private readonly AgentDbContext _db;

    public ConversationRepository(AgentDbContext db) => _db = db;

    public Task<ConversationEntity?> GetAsync(string id, CancellationToken cancellationToken = default) =>
        _db.Conversations.Include(c => c.Messages).FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ConversationEntity>> ListAsync(string? userId, CancellationToken cancellationToken = default)
    {
        var query = _db.Conversations.AsQueryable();
        if (!string.IsNullOrWhiteSpace(userId))
        {
            query = query.Where(c => c.UserId == userId);
        }

        return await query.OrderByDescending(c => c.UpdatedAt).Take(200).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpsertAsync(ConversationEntity conversation, CancellationToken cancellationToken = default)
    {
        var existing = await _db.Conversations.FirstOrDefaultAsync(c => c.Id == conversation.Id, cancellationToken)
            .ConfigureAwait(false);
        if (existing is null)
        {
            await _db.Conversations.AddAsync(conversation, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            existing.Title = conversation.Title;
            existing.UserId = conversation.UserId;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task AddMessageAsync(MessageEntity message, CancellationToken cancellationToken = default)
    {
        await _db.Messages.AddAsync(message, cancellationToken).ConfigureAwait(false);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

public sealed class ExecutionHistoryRepository : IExecutionHistoryRepository
{
    private readonly AgentDbContext _db;

    public ExecutionHistoryRepository(AgentDbContext db) => _db = db;

    public async Task SaveAsync(AgentExecutionEntity execution, CancellationToken cancellationToken = default)
    {
        await _db.Executions.AddAsync(execution, cancellationToken).ConfigureAwait(false);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<AgentExecutionEntity>> ListRecentAsync(int limit = 50, CancellationToken cancellationToken = default) =>
        await _db.Executions.OrderByDescending(e => e.StartedAt).Take(limit).ToListAsync(cancellationToken).ConfigureAwait(false);
}

public sealed class DocumentRepository : IDocumentRepository
{
    private readonly AgentDbContext _db;

    public DocumentRepository(AgentDbContext db) => _db = db;

    public async Task SaveAsync(DocumentEntity document, CancellationToken cancellationToken = default)
    {
        var existing = await _db.Documents.FirstOrDefaultAsync(d => d.Id == document.Id, cancellationToken)
            .ConfigureAwait(false);
        if (existing is null)
        {
            await _db.Documents.AddAsync(document, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            existing.FileName = document.FileName;
            existing.DocumentType = document.DocumentType;
            existing.Collection = document.Collection;
            existing.ChunkCount = document.ChunkCount;
            existing.CharactersExtracted = document.CharactersExtracted;
            existing.IngestedAt = DateTimeOffset.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<DocumentEntity>> ListAsync(string? collection, CancellationToken cancellationToken = default)
    {
        var query = _db.Documents.AsQueryable();
        if (!string.IsNullOrWhiteSpace(collection))
        {
            query = query.Where(d => d.Collection == collection);
        }

        return await query.OrderByDescending(d => d.IngestedAt).ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}
