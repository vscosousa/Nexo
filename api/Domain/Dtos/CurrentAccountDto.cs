namespace Nexo.Api.Domain.Dtos;

/// <summary>The signed-in account, as read from its session.</summary>
public class CurrentAccountDto
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public string Role { get; set; } = "";

    public string Email { get; set; } = "";

    public string? FirstName { get; set; }

    public string? LastName { get; set; }
}
