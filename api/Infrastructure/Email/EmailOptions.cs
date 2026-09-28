namespace Nexo.Api.Infrastructure.Email;

/// <summary>
/// Outgoing mail settings (configuration section <c>Email</c>). The defaults target the local fake SMTP server;
/// point <see cref="Host"/>, <see cref="Port"/>, <see cref="EnableSsl"/> and the credentials at a real provider to send real mail.
/// Keep <see cref="Username"/> and <see cref="Password"/> in user secrets or environment variables, never in committed files.
/// </summary>
public class EmailOptions
{
    public string Host { get; set; } = "localhost";

    public int Port { get; set; } = 1025;

    /// <summary>Upgrades the connection with STARTTLS (typically port 587). Implicit TLS on port 465 is not supported.</summary>
    public bool EnableSsl { get; set; }

    /// <summary>SMTP login; leave empty for servers that need none, such as the fake one.</summary>
    public string? Username { get; set; }

    public string? Password { get; set; }

    /// <summary>The sender address, for example <c>Nexo &lt;no-reply@example.com&gt;</c>.</summary>
    public string From { get; set; } = "Nexo <no-reply@nexo.local>";

    /// <summary>Public address of the web app, used to build links in emails.</summary>
    public string WebBaseUrl { get; set; } = "http://localhost:5173";

    public int TimeoutSeconds { get; set; } = 15;
}
