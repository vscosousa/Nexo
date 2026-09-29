using Nexo.Api.Domain.Models;
using Nexo.Api.Mappers;
using Xunit;

namespace Nexo.Api.Tests.Mappers;

public class PlanMapperTests
{
    [Fact]
    public void GivenAPlan_WhenMapped_ThenDtoCarriesIdNameLimitsPriceAndFeatures()
    {
        var plan = new Plan
        {
            Name = Plan.Team,
            MemberLimit = 100,
            ResourceLimit = 100,
            MonthlyPrice = 29m,
            HasIncidentTracking = true,
            HasExpenseTracking = true,
            HasDecisionHistory = true,
            HasCustomResourceTypes = true,
        };

        var dto = PlanMapper.ToDto(plan);

        Assert.Equal(plan.Id, dto.Id);
        Assert.Equal("Team", dto.Name);
        Assert.Equal(100, dto.MemberLimit);
        Assert.Equal(100, dto.ResourceLimit);
        Assert.Equal(29m, dto.MonthlyPrice);
        Assert.True(dto.HasIncidentTracking);
        Assert.True(dto.HasExpenseTracking);
        Assert.True(dto.HasDecisionHistory);
        Assert.False(dto.HasAiInsights);
        Assert.False(dto.HasPrioritySupport);
        Assert.True(dto.HasCustomResourceTypes);
    }

    [Fact]
    public void GivenAPlanWithNoFixedPrice_WhenMapped_ThenDtoCarriesNullPrice()
    {
        var plan = new Plan { Name = Plan.Enterprise, MonthlyPrice = null };

        var dto = PlanMapper.ToDto(plan);

        Assert.Null(dto.MonthlyPrice);
    }
}
