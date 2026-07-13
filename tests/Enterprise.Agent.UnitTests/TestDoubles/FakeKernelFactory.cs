using Enterprise.Agent.Core.Abstractions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Enterprise.Agent.UnitTests.TestDoubles;

/// <summary>An <see cref="IKernelFactory"/> that builds kernels backed by a fake chat service.</summary>
public sealed class FakeKernelFactory : IKernelFactory
{
    private readonly IChatCompletionService _chatService;

    public FakeKernelFactory(string cannedResponse)
        : this(new FakeChatCompletionService(cannedResponse))
    {
    }

    public FakeKernelFactory(IChatCompletionService chatService) => _chatService = chatService;

    public Kernel Create(string? providerId = null, bool importPlugins = true)
    {
        var builder = Kernel.CreateBuilder();
        builder.Services.AddSingleton(_chatService);
        return builder.Build();
    }
}
