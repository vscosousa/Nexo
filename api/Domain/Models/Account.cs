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

    public required string Email { get; set; }

    public string? Name { get; set; }

    public Role Role { get; set; }

    public AccountStatus Status { get; set; }

    public Guid OrganizationId { get; set; }

    /// <summary>Canonical form used for storage and lookup: trimmed and lowercased.</summary>
    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
