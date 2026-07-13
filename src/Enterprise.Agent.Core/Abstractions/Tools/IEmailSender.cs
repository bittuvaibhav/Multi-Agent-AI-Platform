namespace Enterprise.Agent.Core.Abstractions.Tools;

/// <summary>A message to be sent by an <see cref="IEmailSender"/>.</summary>
public sealed record EmailMessage
{
    public required string To { get; init; }

    public required string Subject { get; init; }

    public required string Body { get; init; }

    public string? Cc { get; init; }

    public bool IsHtml { get; init; }
}

/// <summary>Abstraction over an outbound email transport (implemented in Infrastructure, e.g. SMTP).</summary>
public interface IEmailSender
{
    Task<bool> SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
