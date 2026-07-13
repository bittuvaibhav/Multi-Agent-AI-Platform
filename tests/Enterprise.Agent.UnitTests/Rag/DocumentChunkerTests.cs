using Enterprise.Agent.Infrastructure.Rag;
using Xunit;

namespace Enterprise.Agent.UnitTests.Rag;

public sealed class DocumentChunkerTests
{
    private readonly DocumentChunker _chunker = new();

    [Fact]
    public void EmptyText_ReturnsNoChunks()
    {
        Assert.Empty(_chunker.Chunk("   ", 100, 20));
    }

    [Fact]
    public void ShortText_ReturnsSingleChunk()
    {
        var chunks = _chunker.Chunk("A short paragraph.", 1000, 100);
        Assert.Single(chunks);
    }

    [Fact]
    public void LongText_IsSplitIntoMultipleChunks()
    {
        var paragraph = string.Join("\n\n", Enumerable.Range(0, 40).Select(i => $"Paragraph number {i} with some content."));
        var chunks = _chunker.Chunk(paragraph, 200, 40);
        Assert.True(chunks.Count > 1);
        Assert.All(chunks, c => Assert.False(string.IsNullOrWhiteSpace(c)));
    }

    [Fact]
    public void OversizedParagraph_IsHardSplit()
    {
        var big = new string('x', 1000);
        var chunks = _chunker.Chunk(big, 200, 20);
        Assert.True(chunks.Count >= 5);
    }
}
