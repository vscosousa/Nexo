namespace Nexo.Api.Services;

/// <summary>An identity a social login provider vouched for.</summary>
/// <param name="Provider">Lowercase provider name, such as <c>google</c>.</param>
/// <param name="ProviderKey">The account's stable id at the provider.</param>
/// <param name="Email">The email the provider reported, if any.</param>
/// <param name="EmailVerified">Whether the provider attested that the person owns the email.</param>
public record ExternalIdentity(string Provider, string ProviderKey, string? Email, bool EmailVerified);
