namespace Enterprise.Agent.Infrastructure.Options;

/// <summary>SMTP transport configuration for the email tool/sender.</summary>
public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    /// <summary>When false, emails are logged but not sent.</summary>
    public bool Enabled { get; set; }

    public string Host { get; set; } = "localhost";

    public int Port { get; set; } = 587;

    public bool UseSsl { get; set; } = true;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string From { get; set; } = "no-reply@enterprise-agent.local";
}
