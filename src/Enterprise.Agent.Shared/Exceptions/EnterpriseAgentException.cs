using Enterprise.Agent.Shared.Results;

namespace Enterprise.Agent.Shared.Exceptions;

/// <summary>
/// Base type for all domain-level exceptions raised by the platform. Carries a
/// structured <see cref="Error"/> so middleware can translate it consistently.
/// </summary>
public abstract class EnterpriseAgentException : Exception
{
    protected EnterpriseAgentException(Error error, Exception? innerException = null)
        : base(error.Message, innerException)
    {
        Error = error;
    }

    public Error Error { get; }
}

/// <summary>Raised when a requested resource does not exist.</summary>
public sealed class NotFoundException : EnterpriseAgentException
{
    public NotFoundException(string resource, object key)
        : base(Error.NotFound("resource.not_found", $"'{resource}' with key '{key}' was not found."))
    {
    }

    public NotFoundException(Error error) : base(error)
    {
    }
}

/// <summary>Raised when input fails validation. Aggregates per-field messages.</summary>
public sealed class ValidationException : EnterpriseAgentException
{
    public ValidationException(IReadOnlyDictionary<string, string[]> failures)
        : base(Error.Validation("validation.failed", "One or more validation errors occurred."))
    {
        Failures = failures;
    }

    public IReadOnlyDictionary<string, string[]> Failures { get; }
}

/// <summary>Raised when an agent fails to execute its task.</summary>
public sealed class AgentExecutionException : EnterpriseAgentException
{
    public AgentExecutionException(string agentName, string message, Exception? inner = null)
        : base(Error.Failure("agent.execution_failed", $"Agent '{agentName}' failed: {message}"), inner)
    {
        AgentName = agentName;
    }

    public string AgentName { get; }
}

/// <summary>Raised when a tool/plugin invocation fails.</summary>
public sealed class ToolExecutionException : EnterpriseAgentException
{
    public ToolExecutionException(string toolName, string message, Exception? inner = null)
        : base(Error.External("tool.execution_failed", $"Tool '{toolName}' failed: {message}"), inner)
    {
        ToolName = toolName;
    }

    public string ToolName { get; }
}
