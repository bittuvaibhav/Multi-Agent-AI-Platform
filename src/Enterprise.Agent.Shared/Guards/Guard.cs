using System.Runtime.CompilerServices;

namespace Enterprise.Agent.Shared.Guards;

/// <summary>
/// Lightweight argument validation helpers that throw standard exceptions with the
/// offending parameter name automatically captured via <see cref="CallerArgumentExpressionAttribute"/>.
/// </summary>
public static class Guard
{
    public static T AgainstNull<T>(T? value, [CallerArgumentExpression(nameof(value))] string? name = null)
        where T : class
        => value ?? throw new ArgumentNullException(name);

    public static string AgainstNullOrEmpty(string? value, [CallerArgumentExpression(nameof(value))] string? name = null)
    {
        if (string.IsNullOrEmpty(value))
        {
            throw new ArgumentException("Value cannot be null or empty.", name);
        }

        return value;
    }

    public static string AgainstNullOrWhiteSpace(string? value, [CallerArgumentExpression(nameof(value))] string? name = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", name);
        }

        return value;
    }

    public static int AgainstNegative(int value, [CallerArgumentExpression(nameof(value))] string? name = null)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(name, value, "Value cannot be negative.");
        }

        return value;
    }

    public static int AgainstNonPositive(int value, [CallerArgumentExpression(nameof(value))] string? name = null)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(name, value, "Value must be greater than zero.");
        }

        return value;
    }

    public static IReadOnlyCollection<T> AgainstNullOrEmpty<T>(
        IReadOnlyCollection<T>? value,
        [CallerArgumentExpression(nameof(value))] string? name = null)
    {
        if (value is null || value.Count == 0)
        {
            throw new ArgumentException("Collection cannot be null or empty.", name);
        }

        return value;
    }
}
