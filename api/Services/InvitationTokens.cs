using System.Security.Cryptography;
using System.Text;

namespace Nexo.Api.Services;

/// <summary>
/// An invitation has two independent secrets, both hashed (SHA-256) for storage:
/// - the link token: a long opaque value embedded in the invitation link's URL. On its own it only
///   unlocks the activation page, never the account, so it is fine for it to sit in a URL (and so leak
///   through browser history or a forwarded link) without exposing the account.
/// - the code: a short value sent only in the email body text, never in the link, which the person must
///   separately read and type in. Someone who only has the link (not the email itself) cannot supply it.
/// Activating requires both, so having the link alone is not enough.
/// </summary>
public static class InvitationTokens
{
    /// <summary>How long an invitation stays usable; an admin re-invites the email to issue a fresh one.</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(7);

    public const int CodeLength = 6;

    /// <summary>Wrong codes tried with the right link token before the invitation stops working; a resend or re-invite resets the count.</summary>
    public const int MaxCodeAttempts = 5;

    private const string CodeAlphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

    public static (string Token, string Hash) CreateLinkToken()
    {
        var token = Base64Url(RandomNumberGenerator.GetBytes(24));
        return (token, HashToken(token));
    }

    public static (string Code, string Hash) CreateCode()
    {
        var code = new string(RandomNumberGenerator.GetItems<char>(CodeAlphabet, CodeLength));
        return (code, HashCode(code));
    }

    public static string HashToken(string token) => Hash(token);

    public static string HashCode(string code) => Hash(code.ToUpperInvariant());

    public static bool TokenMatches(string token, string? hash) => Matches(HashToken(token), hash);

    public static bool CodeMatches(string code, string? hash) => Matches(HashCode(code), hash);

    private static bool Matches(string computedHash, string? hash) =>
        hash is not null
        && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(computedHash), Encoding.UTF8.GetBytes(hash));

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
