namespace Nexo.Api.Domain.Dtos;

public class RegisterResourceDto
{
    public string? Name { get; set; }

    public Guid? TypeId { get; set; }

    public string? Description { get; set; }
}
