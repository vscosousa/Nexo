using System.Net;
using Nexo.Api.Infrastructure.Email;

namespace Nexo.Api.Services;

/// <summary>Builds the email that invites a person to activate their account.</summary>
public static class InvitationEmail
{
    public static EmailMessage Create(string to, string organizationName, string inviterName, string webBaseUrl, string token)
    {
        var link = $"{webBaseUrl.TrimEnd('/')}/activate?email={Uri.EscapeDataString(to)}&token={Uri.EscapeDataString(token)}";
        var subject = $"{inviterName} invited you to join {organizationName} on Nexo";

        var text = $"""
            {inviterName} invited you to join {organizationName} on Nexo.

            Create your account: {link}

            If the link does not open, use this invitation token: {token}

            If you were not expecting this invitation, you can ignore this email.
            """;

        var html = $"""
            <div style="font-family:Arial,Helvetica,sans-serif;max-width:480px;margin:0 auto;color:#202124">
              <h2 style="font-weight:500">You're invited to {Encode(organizationName)}</h2>
              <p><strong>{Encode(inviterName)}</strong> invited you to join <strong>{Encode(organizationName)}</strong> on Nexo.</p>
              <p><a href="{Encode(link)}" style="display:inline-block;padding:10px 24px;border-radius:4px;background:#1a73e8;color:#fff;text-decoration:none">Create your account</a></p>
              <p style="color:#5f6368;font-size:13px">If the button does not work, use this invitation token:<br><code>{Encode(token)}</code></p>
              <p style="color:#5f6368;font-size:13px">If you were not expecting this invitation, you can ignore this email.</p>
            </div>
            """;

        return new EmailMessage(to, subject, text, html);
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}
