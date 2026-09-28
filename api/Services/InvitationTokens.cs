using System.Security.Cryptography;
using System.Text;

namespace Nexo.Api.Services;

/// <summary>One-time invitation tokens: the plain token goes to the admin, only its SHA-256 hash is stored.</summary>
public static class InvitationTokens
{
    public static (string Token, string Hash) Create()
    {
        var token = Base64Url(RandomNumberGenerator.GetBytes(32));
        return (token, Hash(token));
    }

    public static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    public static bool Matches(string token, string? hash) =>
        hash is not null
        && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(Hash(token)), Encoding.UTF8.GetBytes(hash));

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
