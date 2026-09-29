namespace Nexo.Api.Domain.Models;

/// <summary>A subscription tier and the limits and features it grants an organization.</summary>
public class Plan
{
    public const string Free = "Free";
    public const string Team = "Team";
    public const string Enterprise = "Enterprise";

    /// <summary>Marks a limit as unbounded.</summary>
    public const int Unlimited = int.MaxValue;

    public Guid Id { get; set; } = Guid.NewGuid();

    public required string Name { get; set; }

    /// <summary>Maximum number of active member accounts.</summary>
    public int MemberLimit { get; set; }

    /// <summary>Maximum number of resources the organization can register.</summary>
    public int ResourceLimit { get; set; }

    /// <summary>Monthly price in euros, or null for a custom/"contact us" plan.</summary>
    public decimal? MonthlyPrice { get; set; }

    /// <summary>Whether the plan includes incident and maintenance tracking.</summary>
    public bool HasIncidentTracking { get; set; }

    /// <summary>Whether the plan includes expense tracking.</summary>
    public bool HasExpenseTracking { get; set; }

    /// <summary>Whether the plan includes decision history (the audit trail of board/assembly decisions).</summary>
    public bool HasDecisionHistory { get; set; }

    /// <summary>Whether the plan includes AI-powered insights and suggestions.</summary>
    public bool HasAiInsights { get; set; }

    /// <summary>Whether the plan includes priority support.</summary>
    public bool HasPrioritySupport { get; set; }
}
