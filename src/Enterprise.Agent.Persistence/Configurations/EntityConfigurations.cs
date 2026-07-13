using Enterprise.Agent.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Enterprise.Agent.Persistence.Configurations;

public sealed class ConversationConfiguration : IEntityTypeConfiguration<ConversationEntity>
{
    public void Configure(EntityTypeBuilder<ConversationEntity> builder)
    {
        builder.ToTable("Conversations");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasMaxLength(64);
        builder.Property(c => c.Title).HasMaxLength(512);
        builder.Property(c => c.UserId).HasMaxLength(128);
        builder.HasIndex(c => c.UserId);
        builder.HasMany(c => c.Messages)
            .WithOne(m => m.Conversation!)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class MessageConfiguration : IEntityTypeConfiguration<MessageEntity>
{
    public void Configure(EntityTypeBuilder<MessageEntity> builder)
    {
        builder.ToTable("Messages");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.ConversationId).HasMaxLength(64).IsRequired();
        builder.Property(m => m.Role).HasConversion<int>();
        builder.Property(m => m.Content).IsRequired();
        builder.Property(m => m.Name).HasMaxLength(128);
        builder.HasIndex(m => m.ConversationId);
    }
}

public sealed class DocumentConfiguration : IEntityTypeConfiguration<DocumentEntity>
{
    public void Configure(EntityTypeBuilder<DocumentEntity> builder)
    {
        builder.ToTable("Documents");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasMaxLength(128);
        builder.Property(d => d.FileName).HasMaxLength(512).IsRequired();
        builder.Property(d => d.DocumentType).HasConversion<int>();
        builder.Property(d => d.Collection).HasMaxLength(128);
        builder.HasIndex(d => d.Collection);
    }
}

public sealed class AgentExecutionConfiguration : IEntityTypeConfiguration<AgentExecutionEntity>
{
    public void Configure(EntityTypeBuilder<AgentExecutionEntity> builder)
    {
        builder.ToTable("Executions");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.CorrelationId).HasMaxLength(64).IsRequired();
        builder.Property(e => e.Goal).IsRequired();
        builder.Property(e => e.Mode).HasConversion<int>();
        builder.Property(e => e.Status).HasConversion<int>();
        builder.HasIndex(e => e.CorrelationId);
    }
}
