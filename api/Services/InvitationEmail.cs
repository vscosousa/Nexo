using System.Net;
using Nexo.Api.Infrastructure.Email;

namespace Nexo.Api.Services;

/// <summary>Builds the email that invites a person to activate their account.</summary>
public static class InvitationEmail
{
    public static EmailMessage Create(
        string to, string organizationName, string inviterName, string webBaseUrl, string linkToken, string code)
    {
        var link = $"{webBaseUrl.TrimEnd('/')}/activate?email={Uri.EscapeDataString(to)}&token={Uri.EscapeDataString(linkToken)}";
        var subject = $"{inviterName} invited you to join {organizationName} on Nexo";

        var text = $"""
            {inviterName} invited you to join {organizationName} on Nexo.

            Create your account: {link}

            Your invitation code: {code}

            The activation page asks for this code separately, so make sure to keep this email once you have opened the link.

            If you were not expecting this invitation, you can ignore this email.
            """;

        var html = $"""
            <div style="font-family:Arial,Helvetica,sans-serif;max-width:480px;margin:0 auto;color:#202124">
              <h2 style="font-weight:500">You're invited to {Encode(organizationName)}</h2>
              <p><strong>{Encode(inviterName)}</strong> invited you to join <strong>{Encode(organizationName)}</strong> on Nexo.</p>
              <p><a href="{Encode(link)}" style="display:inline-block;padding:10px 24px;border-radius:4px;background:#1a73e8;color:#fff;text-decoration:none">Create your account</a></p>
              <p style="color:#5f6368;font-size:13px">The page will ask for this invitation code:</p>
              <p style="font-size:20px;font-weight:600;letter-spacing:4px;font-family:monospace">{Encode(code)}</p>
              <p style="color:#5f6368;font-size:13px">If you were not expecting this invitation, you can ignore this email.</p>
            </div>
            """;

        return new EmailMessage(to, subject, text, html);
    }

    /// <summary>
    /// Just a fresh code, for a resend: the link token does not change, so the person's already-open
    /// activation page keeps working once they type in this new code. No link is included, since the
    /// original one is still valid and repeating it here would invite confusion about which link to use.
    /// </summary>
    public static EmailMessage CreateCodeReminder(string to, string organizationName, string code)
    {
        var subject = $"Your new invitation code for {organizationName} on Nexo";

        var text = $"""
            Here is a fresh invitation code for {organizationName} on Nexo: {code}

            Enter it on the activation page you already had open. The invitation link itself has not changed.

            If you were not expecting this, you can ignore this email.
            """;

        var html = $"""
            <div style="font-family:Arial,Helvetica,sans-serif;max-width:480px;margin:0 auto;color:#202124">
              <h2 style="font-weight:500">Your new invitation code</h2>
              <p>Here is a fresh invitation code for <strong>{Encode(organizationName)}</strong> on Nexo:</p>
              <p style="font-size:20px;font-weight:600;letter-spacing:4px;font-family:monospace">{Encode(code)}</p>
              <p style="color:#5f6368;font-size:13px">Enter it on the activation page you already had open. The invitation link itself has not changed.</p>
              <p style="color:#5f6368;font-size:13px">If you were not expecting this, you can ignore this email.</p>
            </div>
            """;

        return new EmailMessage(to, subject, text, html);
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}
