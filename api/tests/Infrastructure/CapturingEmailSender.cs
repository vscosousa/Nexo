using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Nexo.Api.Infrastructure.Email;

namespace Nexo.Api.Tests.Infrastructure;

/// <summary>Replaces SMTP in API tests: keeps every message so tests can read what was "sent".</summary>
public sealed partial class CapturingEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<EmailMessage> _sent = new();

    public IReadOnlyList<EmailMessage> Sent => [.. _sent];

    public Task SendAsync(EmailMessage message)
    {
        _sent.Enqueue(message);
        return Task.CompletedTask;
    }

    public void Clear() => _sent.Clear();

    /// <summary>The link token in the activation link of a captured message.</summary>
    public static string TokenIn(EmailMessage message) => TokenPattern().Match(message.Text).Groups[1].Value;

    /// <summary>The invitation code in the body text of a captured message.</summary>
    public static string CodeIn(EmailMessage message) => CodePattern().Match(message.Text).Groups[1].Value;

    [GeneratedRegex(@"token=([^&\s""]+)")]
    private static partial Regex TokenPattern();

    [GeneratedRegex(@"invitation code[^:]*:\s*(\S+)")]
    private static partial Regex CodePattern();
}
