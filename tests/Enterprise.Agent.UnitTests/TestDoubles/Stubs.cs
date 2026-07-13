using Enterprise.Agent.Core.Abstractions.AI;
using Enterprise.Agent.Core.Abstractions.Orchestration;
using Enterprise.Agent.Models.Chat;
using Enterprise.Agent.Models.Orchestration;

namespace Enterprise.Agent.UnitTests.TestDoubles;

/// <summary>A planner that always returns a pre-built plan.</summary>
public sealed class StubPlanner : IPlanner
{
    private readonly WorkflowPlan _plan;

    public StubPlanner(WorkflowPlan plan) => _plan = plan;

    public Task<WorkflowPlan> CreatePlanAsync(string goal, CancellationToken cancellationToken = default) =>
        Task.FromResult(_plan);
}

/// <summary>A chat provider that returns a canned string (mock OpenAI at the abstraction level).</summary>
public sealed class FakeChatProvider : IChatCompletionProvider
{
    private readonly Func<IReadOnlyList<ChatMessage>, string> _responder;

    public FakeChatProvider(string response) => _responder = _ => response;

    public FakeChatProvider(Func<IReadOnlyList<ChatMessage>, string> responder) => _responder = responder;

    public string ProviderId => "fake";

    public Task<string> CompleteAsync(
        IReadOnlyList<ChatMessage> messages,
        ChatCompletionSettings? settings = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_responder(messages));
}
