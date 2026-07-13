using System.Text;
using Enterprise.Agent.Core.Abstractions.Rag;

namespace Enterprise.Agent.Infrastructure.Rag;

/// <summary>
/// Splits text into overlapping, paragraph-aware chunks. Paragraphs are packed up to the
/// target size; oversized paragraphs are hard-split. Consecutive chunks share an overlap
/// window to preserve context across boundaries.
/// </summary>
public sealed class DocumentChunker : IDocumentChunker
{
    public IReadOnlyList<string> Chunk(string text, int maxChars, int overlapChars)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        maxChars = Math.Max(100, maxChars);
        overlapChars = Math.Clamp(overlapChars, 0, maxChars / 2);

        var normalized = text.Replace("\r\n", "\n").Trim();
        var paragraphs = normalized.Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var chunks = new List<string>();
        var current = new StringBuilder();

        void Flush()
        {
            if (current.Length > 0)
            {
                chunks.Add(current.ToString().Trim());
                current.Clear();
            }
        }

        foreach (var paragraph in paragraphs)
        {
            if (paragraph.Length > maxChars)
            {
                Flush();
                chunks.AddRange(HardSplit(paragraph, maxChars, overlapChars));
                continue;
            }

            if (current.Length + paragraph.Length + 2 > maxChars)
            {
                Flush();
            }

            if (current.Length > 0)
            {
                current.Append("\n\n");
            }

            current.Append(paragraph);
        }

        Flush();
        return ApplyOverlap(chunks, overlapChars);
    }

    private static IEnumerable<string> HardSplit(string text, int maxChars, int overlapChars)
    {
        var step = Math.Max(1, maxChars - overlapChars);
        for (var start = 0; start < text.Length; start += step)
        {
            var length = Math.Min(maxChars, text.Length - start);
            yield return text.Substring(start, length).Trim();
            if (start + length >= text.Length)
            {
                yield break;
            }
        }
    }

    private static IReadOnlyList<string> ApplyOverlap(IReadOnlyList<string> chunks, int overlapChars)
    {
        if (overlapChars == 0 || chunks.Count <= 1)
        {
            return chunks;
        }

        var result = new List<string>(chunks.Count) { chunks[0] };
        for (var i = 1; i < chunks.Count; i++)
        {
            var previous = chunks[i - 1];
            var tail = previous.Length <= overlapChars ? previous : previous[^overlapChars..];
            result.Add((tail + "\n\n" + chunks[i]).Trim());
        }

        return result;
    }
}
