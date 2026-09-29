using System.Net;
using Nexo.Api.Infrastructure.Email;

namespace Nexo.Api.Services;

/// <summary>Builds the emails about a person's own account: confirming its address, a repeated registration, and unlocking it.</summary>
public static class AccountEmails
{
    /// <summary>Asks a newly registered admin to confirm the email address before the account can sign in.</summary>
    public static EmailMessage Confirmation(string to, string organizationName, string webBaseUrl, string token)
    {
        var link = Link(webBaseUrl, "confirm-email", to, token);
        return Build(
            to,
            $"Confirm your email for {organizationName} on Nexo",
            "Confirm your email",
            $"You registered {organizationName} on Nexo. Confirm this email address to finish and sign in.",
            "Confirm email",
            link,
            "The link works for 24 hours. If you did not register, you can ignore this email and nothing will be created.");
    }

    /// <summary>
    /// Sent instead of creating anything when someone registers an email that already has an account, so the
    /// registration form responds the same either way and cannot be used to check which emails are registered.
    /// </summary>
    public static EmailMessage AlreadyRegistered(string to, string webBaseUrl) => Build(
        to,
        "You already have a Nexo account",
        "You already have an account",
        "Someone tried to register a new organization on Nexo with this email, which already has an account. Nothing was changed.",
        "Sign in",
        $"{webBaseUrl.TrimEnd('/')}/login",
        "If this was you, sign in instead. If you just registered and have not confirmed yet, use the link in the first email. Otherwise you can ignore this email.");

    /// <summary>Sent when too many wrong passwords lock the account; the link is the only way to unlock it.</summary>
    public static EmailMessage Unlock(string to, string webBaseUrl, string token)
    {
        var link = Link(webBaseUrl, "unlock", to, token);
        return Build(
            to,
            "Your Nexo account was locked",
            "Your account was locked",
            "Your Nexo account was locked after too many wrong passwords, and any open sessions were signed out.",
            "Unlock account",
            link,
            "The link works for 24 hours; after that, signing in again emails you a new one. If it was not you, someone may know your email; unlock the account and choose a strong password.");
    }

    private static string Link(string webBaseUrl, string page, string email, string token) =>
        $"{webBaseUrl.TrimEnd('/')}/{page}?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";

    private static EmailMessage Build(
        string to, string subject, string heading, string intro, string action, string link, string footnote)
    {
        var text = $"""
            {intro}

            {action}: {link}

            {footnote}
            """;

        var html = $"""
            <div style="font-family:Arial,Helvetica,sans-serif;max-width:480px;margin:0 auto;color:#202124">
              <h2 style="font-weight:500">{Encode(heading)}</h2>
              <p>{Encode(intro)}</p>
              <p><a href="{Encode(link)}" style="display:inline-block;padding:10px 24px;border-radius:4px;background:#1a73e8;color:#fff;text-decoration:none">{Encode(action)}</a></p>
              <p style="color:#5f6368;font-size:13px">{Encode(footnote)}</p>
            </div>
            """;

        return new EmailMessage(to, subject, text, html);
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}
