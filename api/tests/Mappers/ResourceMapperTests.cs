using Nexo.Api.Domain.Dtos;
using Nexo.Api.Domain.Models;
using Nexo.Api.Mappers;
using Xunit;

namespace Nexo.Api.Tests.Mappers;

public class ResourceMapperTests
{
    private static readonly Guid OrganizationId = Guid.NewGuid();

    private static readonly Guid TypeId = Guid.NewGuid();

    [Fact]
    public void GivenRegistrationDetails_WhenMappedToResource_ThenItIsAnAvailableTrimmedResourceOfTheOrganization()
    {
        var resource = ResourceMapper.ToResource(
            new RegisterResourceDto { Name = " Main hall ", TypeId = TypeId, Description = " Seats 80 " }, OrganizationId);

        Assert.Equal("Main hall", resource.Name);
        Assert.Equal(TypeId, resource.TypeId);
        Assert.Equal("Seats 80", resource.Description);
        Assert.Equal(ResourceStatus.Available, resource.Status);
        Assert.Equal(OrganizationId, resource.OrganizationId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void GivenNoDescription_WhenMappedToResource_ThenTheDescriptionIsNull(string? description)
    {
        var resource = ResourceMapper.ToResource(
            new RegisterResourceDto { Name = "Van", TypeId = TypeId, Description = description }, OrganizationId);

        Assert.Null(resource.Description);
    }

    [Fact]
    public void GivenAResourceAndItsType_WhenMappedToDto_ThenEveryFieldIsCopied()
    {
        var resource = new Resource { Name = "Van", TypeId = TypeId, Description = "9 seats", OrganizationId = OrganizationId };

        var dto = ResourceMapper.ToDto(resource, new ResourceType { Id = TypeId, Name = "Vehicle" });

        Assert.Equal(resource.Id, dto.Id);
        Assert.Equal("Van", dto.Name);
        Assert.Equal(TypeId, dto.TypeId);
        Assert.Equal("Vehicle", dto.TypeName);
        Assert.Equal("9 seats", dto.Description);
        Assert.Equal(ResourceStatus.Available, dto.Status);
        Assert.Equal(OrganizationId, dto.OrganizationId);
    }
}
