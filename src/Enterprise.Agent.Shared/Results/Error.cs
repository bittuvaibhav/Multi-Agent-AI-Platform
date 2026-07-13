namespace Enterprise.Agent.Shared.Results;

/// <summary>
/// Categorises the nature of a failure so callers (and the API layer) can map it
/// to an appropriate transport-level response without inspecting message strings.
/// </summary>
public enum ErrorType
{
    Failure = 0,
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    Unauthorized = 4,
    Forbidden = 5,
    Timeout = 6,
    Cancelled = 7,
    External = 8
}

/// <summary>
/// An immutable, structured description of a failure. Prefer this over throwing
/// for expected, recoverable error conditions (see <see cref="Result"/>).
/// </summary>
public sealed record Error(string Code, string Message, ErrorType Type = ErrorType.Failure)
{
    /// <summary>Represents the absence of an error.</summary>
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);

    public static Error Failure(string code, string message) => new(code, message, ErrorType.Failure);
    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);
    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);
    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);
    public static Error Unauthorized(string code, string message) => new(code, message, ErrorType.Unauthorized);
    public static Error Forbidden(string code, string message) => new(code, message, ErrorType.Forbidden);
    public static Error Timeout(string code, string message) => new(code, message, ErrorType.Timeout);
    public static Error Cancelled(string code, string message) => new(code, message, ErrorType.Cancelled);
    public static Error External(string code, string message) => new(code, message, ErrorType.External);

    public override string ToString() => $"[{Type}] {Code}: {Message}";
}
