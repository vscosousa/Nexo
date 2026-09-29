using System.Net;
using System.Net.Http.Json;
using Nexo.Api.Domain.Dtos;
using Nexo.Api.Domain.Models;
using Nexo.Api.Tests.Infrastructure;
using Xunit;

namespace Nexo.Api.Tests.Controllers;

/// <summary>US-006 acceptance tests, run against PostgreSQL.</summary>
public class PlansEndpointTests(PostgresApiFactory factory) : IClassFixture<PostgresApiFactory>, IAsyncLifetime
{
    public Task InitializeAsync() => factory.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GivenTheSeededPlans_WhenListingPlans_ThenEachPlanCarriesItsOwnLimits()
    {
        var response = await factory.CreateClient().GetAsync("/plans");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var plans = await response.Content.ReadFromJsonAsync<List<PlanDto>>();
        Assert.NotNull(plans);
        Assert.Equal(3, plans.Count);

        var free = Assert.Single(plans, p => p.Name == Plan.Free);
        Assert.Equal(20, free.MemberLimit);
        Assert.Equal(10, free.ResourceLimit);
        Assert.Equal(0m, free.MonthlyPrice);
        Assert.False(free.HasIncidentTracking);
        Assert.False(free.HasExpenseTracking);
        Assert.False(free.HasDecisionHistory);
        Assert.False(free.HasAiInsights);
        Assert.False(free.HasPrioritySupport);
        Assert.False(free.HasCustomResourceTypes);

        var team = Assert.Single(plans, p => p.Name == Plan.Team);
        Assert.Equal(100, team.MemberLimit);
        Assert.Equal(100, team.ResourceLimit);
        Assert.Equal(29m, team.MonthlyPrice);
        Assert.True(team.HasIncidentTracking);
        Assert.True(team.HasExpenseTracking);
        Assert.True(team.HasDecisionHistory);
        Assert.False(team.HasAiInsights);
        Assert.False(team.HasPrioritySupport);
        Assert.True(team.HasCustomResourceTypes);

        var enterprise = Assert.Single(plans, p => p.Name == Plan.Enterprise);
        Assert.Equal(Plan.Unlimited, enterprise.MemberLimit);
        Assert.Equal(Plan.Unlimited, enterprise.ResourceLimit);
        Assert.Null(enterprise.MonthlyPrice);
        Assert.True(enterprise.HasIncidentTracking);
        Assert.True(enterprise.HasExpenseTracking);
        Assert.True(enterprise.HasDecisionHistory);
        Assert.True(enterprise.HasAiInsights);
        Assert.True(enterprise.HasPrioritySupport);
        Assert.True(enterprise.HasCustomResourceTypes);
    }
}
