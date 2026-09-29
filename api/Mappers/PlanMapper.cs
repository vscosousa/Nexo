using Nexo.Api.Domain.Models;
using Nexo.Api.Domain.Dtos;

namespace Nexo.Api.Mappers;

public static class PlanMapper
{
    public static PlanDto ToDto(Plan plan) => new()
    {
        Id = plan.Id,
        Name = plan.Name,
        MemberLimit = plan.MemberLimit,
        ResourceLimit = plan.ResourceLimit,
        MonthlyPrice = plan.MonthlyPrice,
        HasIncidentTracking = plan.HasIncidentTracking,
        HasExpenseTracking = plan.HasExpenseTracking,
        HasDecisionHistory = plan.HasDecisionHistory,
        HasAiInsights = plan.HasAiInsights,
        HasPrioritySupport = plan.HasPrioritySupport,
        HasCustomResourceTypes = plan.HasCustomResourceTypes,
    };
}
