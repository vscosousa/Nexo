namespace Nexo.Api.Domain.Models;

/// <summary>Links an account to an identity at a social login provider.</summary>
public class ExternalLogin
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public const int ProviderMaxLength = 20;

    public const int ProviderKeyMaxLength = 200;

    public Guid AccountId { get; set; }

    /// <summary>Lowercase provider name: <c>google</c> or <c>microsoft</c>.</summary>
    public required string Provider { get; set; }

    /// <summary>The account's stable id at the provider (not its email).</summary>
    public required string ProviderKey { get; set; }
}
