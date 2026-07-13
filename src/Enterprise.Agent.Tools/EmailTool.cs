using System.ComponentModel;
using Enterprise.Agent.Core.Abstractions.Tools;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace Enterprise.Agent.Tools;

/// <summary>Lets agents send email through the configured <see cref="IEmailSender"/> transport.</summary>
public sealed class EmailTool : IKernelPluginSource
{
    public const string PluginName = "Email";

    private readonly IEmailSender _emailSender;
    private readonly ILogger<EmailTool> _logger;

    public EmailTool(IEmailSender emailSender, ILogger<EmailTool> logger)
    {
        _emailSender = emailSender;
        _logger = logger;
    }

    [KernelFunction("send_email"), Description("Sends an email with a subject and body to a recipient.")]
    public async Task<string> SendEmailAsync(
        [Description("Recipient email address.")] string to,
        [Description("Email subject line.")] string subject,
        [Description("Email body text.")] string body,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(to) || !to.Contains('@'))
        {
            return "A valid recipient email address is required.";
        }

        try
        {
            var sent = await _emailSender.SendAsync(
                new EmailMessage { To = to, Subject = subject, Body = body }, cancellationToken).ConfigureAwait(false);
            return sent ? $"Email sent to {to}." : $"Email to {to} was not sent.";
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send email to {To}.", to);
            return $"Failed to send email: {ex.Message}";
        }
    }

    public KernelPlugin BuildPlugin() => KernelPluginFactory.CreateFromObject(this, PluginName);
}
