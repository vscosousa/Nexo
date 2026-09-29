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

    /// <summary>A self-registered admin whose email is not confirmed yet; cannot sign in or be linked to a social login.</summary>
    Unverified,
}

public class Account
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public const int EmailMaxLength = 320;

    public const int NameMaxLength = 100;

    public required string Email { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    /// <summary>Null until a password is set; an invited or SSO-only account has none.</summary>
    public string? PasswordHash { get; set; }

    /// <summary>SHA-256 of the invitation link's token (in the email's URL); set while <c>Invited</c>, cleared on activation.</summary>
    public string? InvitationTokenHash { get; set; }

    /// <summary>SHA-256 of the invitation code (in the email's body text, typed in separately); set while <c>Invited</c>, cleared on activation.</summary>
    public string? InvitationCodeHash { get; set; }

    /// <summary>UTC time after which the invitation token and code stop working; set with them, cleared on activation. A missing value counts as expired.</summary>
    public DateTime? InvitationExpiresAt { get; set; }

    public Role Role { get; set; }

    public AccountStatus Status { get; set; }

    public Guid OrganizationId { get; set; }

    /// <summary>Canonical form used for storage and lookup: trimmed and lowercased.</summary>
    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    /// <summary>First and last name joined, or empty before either is set (a pending invited account).</summary>
    public string FullName => $"{FirstName} {LastName}".Trim();
}
