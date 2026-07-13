using System.Net;
using System.Net.Mail;
using Enterprise.Agent.Core.Abstractions.Tools;
using Enterprise.Agent.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Enterprise.Agent.Infrastructure.Email;

/// <summary>SMTP implementation of <see cref="IEmailSender"/>. Disabled by default; when
/// disabled it logs the message and reports failure instead of throwing.</summary>
public sealed class SmtpEmailSender : IEmailSender
{
    private readonly SmtpOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation(
                "SMTP disabled; not sending email to {To} with subject '{Subject}'.", message.To, message.Subject);
            return false;
        }

        try
        {
            using var mail = new MailMessage(_options.From, message.To, message.Subject, message.Body)
            {
                IsBodyHtml = message.IsHtml
            };
            if (!string.IsNullOrWhiteSpace(message.Cc))
            {
                mail.CC.Add(message.Cc);
            }

            using var client = new SmtpClient(_options.Host, _options.Port)
            {
                EnableSsl = _options.UseSsl,
                Credentials = string.IsNullOrWhiteSpace(_options.Username)
                    ? CredentialCache.DefaultNetworkCredentials
                    : new NetworkCredential(_options.Username, _options.Password)
            };

            await client.SendMailAsync(mail, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Email sent to {To}.", message.To);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}.", message.To);
            return false;
        }
    }
}
