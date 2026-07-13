using System.Text;

namespace Enterprise.Agent.Shared.Extensions;

public static class StringExtensions
{
    public static bool IsNullOrWhiteSpace(this string? value) => string.IsNullOrWhiteSpace(value);

    public static bool HasValue(this string? value) => !string.IsNullOrWhiteSpace(value);

    /// <summary>Truncates a string to <paramref name="maxLength"/>, appending an ellipsis when cut.</summary>
    public static string Truncate(this string value, int maxLength, string suffix = "…")
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value;
        }

        return string.Concat(value.AsSpan(0, Math.Max(0, maxLength - suffix.Length)), suffix);
    }

    /// <summary>Collapses runs of whitespace into single spaces and trims the result.</summary>
    public static string NormalizeWhitespace(this string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);
        var previousWhitespace = false;
        foreach (var ch in value)
        {
            if (char.IsWhiteSpace(ch))
            {
                if (!previousWhitespace)
                {
                    builder.Append(' ');
                }

                previousWhitespace = true;
            }
            else
            {
                builder.Append(ch);
                previousWhitespace = false;
            }
        }

        return builder.ToString().Trim();
    }
}
