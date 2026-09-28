using Nexo.Api.Domain.Models;

namespace Nexo.Api.Domain.Dtos;

public class AccountDto
{
    public Guid Id { get; set; }

    public string Email { get; set; } = "";

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public Role Role { get; set; }

    public AccountStatus Status { get; set; }

    public Guid OrganizationId { get; set; }
}
