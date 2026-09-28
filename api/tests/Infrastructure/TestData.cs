namespace Nexo.Api.Tests.Infrastructure;

/// <summary>Synthetic values that satisfy the password policy and never appear in test names.</summary>
internal static class TestData
{
    public const string StrongPassword = "Str0ng!Passw0rd";

    public const string JwtKey = "test-signing-key-at-least-32-bytes-long!";

    public const string InvitationLinkToken = "test-invitation-link-token";

    public const string InvitationCode = "ABC123";
}
