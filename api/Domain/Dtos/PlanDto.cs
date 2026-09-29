namespace Nexo.Api.Domain.Dtos;

public class PlanDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = "";

    public int MemberLimit { get; set; }

    public int ResourceLimit { get; set; }

    public decimal? MonthlyPrice { get; set; }

    public bool HasIncidentTracking { get; set; }

    public bool HasExpenseTracking { get; set; }

    public bool HasDecisionHistory { get; set; }

    public bool HasAiInsights { get; set; }

    public bool HasPrioritySupport { get; set; }
}
