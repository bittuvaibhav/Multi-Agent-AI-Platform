namespace Enterprise.Agent.Core.Options;

/// <summary>Tunable behaviour of the agent orchestrator. Bound from configuration.</summary>
public sealed class OrchestratorOptions
{
    public const string SectionName = "Orchestrator";

    /// <summary>Maximum retry attempts per step (in addition to the initial attempt).</summary>
    public int MaxRetries { get; set; } = 2;

    /// <summary>Base delay between retries; grows linearly with attempt number.</summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMilliseconds(500);

    /// <summary>Maximum wall-clock time allowed for a single step before it times out.</summary>
    public TimeSpan StepTimeout { get; set; } = TimeSpan.FromSeconds(120);

    /// <summary>Maximum degree of parallelism when executing a parallel plan.</summary>
    public int MaxDegreeOfParallelism { get; set; } = 4;

    /// <summary>When true, a failed step aborts the remaining sequential steps.</summary>
    public bool StopOnFirstFailure { get; set; } = true;
}
