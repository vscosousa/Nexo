namespace Nexo.Api.Domain.Models;

public enum Role
{
    Admin,
    Member,
}

public enum AccountStatus
{
    Invited,
    Active,
}

public class Account
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public const int EmailMaxLength = 320;

    public const int NameMaxLength = 200;

    public required string Email { get; set; }

    public string? Name { get; set; }

    /// <summary>Null until a password is set; an invited or SSO-only account has none.</summary>
    public string? PasswordHash { get; set; }

    /// <summary>SHA-256 of the one-time invitation token; set while the account is <c>Invited</c>, cleared on activation.</summary>
    public string? InvitationTokenHash { get; set; }

    public Role Role { get; set; }

    public AccountStatus Status { get; set; }

    public Guid OrganizationId { get; set; }

    /// <summary>Canonical form used for storage and lookup: trimmed and lowercased.</summary>
    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
