namespace Enterprise.Agent.Shared.Correlation;

/// <summary>
/// Ambient correlation identifier for a single logical request/operation. Flows
/// through logs, agent execution and downstream calls to enable end-to-end tracing.
/// </summary>
public interface ICorrelationContext
{
    string CorrelationId { get; }

    void Set(string correlationId);
}

/// <summary>
/// Default async-local implementation. Registered as a scoped service so each
/// request gets an isolated correlation id, while background flows inherit it.
/// </summary>
public sealed class CorrelationContext : ICorrelationContext
{
    private static readonly AsyncLocal<string?> Current = new();

    public string CorrelationId
    {
        get
        {
            if (string.IsNullOrEmpty(Current.Value))
            {
                Current.Value = Guid.NewGuid().ToString("N");
            }

            return Current.Value!;
        }
    }

    public void Set(string correlationId)
    {
        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            Current.Value = correlationId;
        }
    }
}
