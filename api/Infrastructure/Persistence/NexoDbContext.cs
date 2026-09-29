using Microsoft.EntityFrameworkCore;
using Nexo.Api.Domain.Models;

namespace Nexo.Api.Infrastructure.Persistence;

/// <summary>
/// The application's EF Core context. Org-owned sets are filtered to the signed-in caller's organization (the
/// session token's <c>orgId</c> claim, per ADR-011); with no signed-in caller they return no org-owned rows.
/// </summary>
public class NexoDbContext(DbContextOptions<NexoDbContext> options, IHttpContextAccessor? http = null) : DbContext(options)
{
    public static readonly Guid FreePlanId = new("6f1c2b1e-7a52-4d0a-9a53-1f3e5c0b8d01");
    public static readonly Guid TeamPlanId = new("8b2d4f3a-9c61-4e2b-8a64-2f4e6c0b9d02");
    public static readonly Guid EnterprisePlanId = new("a1e5c7d4-1b83-4f0c-9b75-3a5f7d1c0e03");

    public static readonly Guid EquipmentTypeId = new("3c7e1a52-0d4b-4c6e-8f19-6a2b5d8e1f02");
    public static readonly Guid VehicleTypeId = new("3c7e1a52-0d4b-4c6e-8f19-6a2b5d8e1f03");
    public static readonly Guid OtherTypeId = new("3c7e1a52-0d4b-4c6e-8f19-6a2b5d8e1f04");
    public static readonly Guid UtensilTypeId = new("3c7e1a52-0d4b-4c6e-8f19-6a2b5d8e1f05");

    private Guid? CurrentOrganizationId =>
        Guid.TryParse(http?.HttpContext?.User.FindFirst("orgId")?.Value, out var id) ? id : null;

    public DbSet<Plan> Plans => Set<Plan>();

    public DbSet<Organization> Organizations => Set<Organization>();

    public DbSet<Account> Accounts => Set<Account>();

    public DbSet<ExternalLogin> ExternalLogins => Set<ExternalLogin>();

    public DbSet<ResourceType> ResourceTypes => Set<ResourceType>();

    public DbSet<Resource> Resources => Set<Resource>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Plan>(plan =>
        {
            plan.Property(p => p.Name).HasMaxLength(50);
            plan.Property(p => p.MonthlyPrice).HasPrecision(8, 2);
            plan.HasIndex(p => p.Name).IsUnique();
            plan.HasData(
                new Plan
                {
                    Id = FreePlanId,
                    Name = Plan.Free,
                    MemberLimit = 20,
                    ResourceLimit = 10,
                    MonthlyPrice = 0m,
                },
                new Plan
                {
                    Id = TeamPlanId,
                    Name = Plan.Team,
                    MemberLimit = 100,
                    ResourceLimit = 100,
                    MonthlyPrice = 29m,
                    HasIncidentTracking = true,
                    HasExpenseTracking = true,
                    HasDecisionHistory = true,
                    HasCustomResourceTypes = true,
                },
                new Plan
                {
                    Id = EnterprisePlanId,
                    Name = Plan.Enterprise,
                    MemberLimit = Plan.Unlimited,
                    ResourceLimit = Plan.Unlimited,
                    MonthlyPrice = null,
                    HasIncidentTracking = true,
                    HasExpenseTracking = true,
                    HasDecisionHistory = true,
                    HasAiInsights = true,
                    HasPrioritySupport = true,
                    HasCustomResourceTypes = true,
                });
        });

        modelBuilder.Entity<Organization>(organization =>
        {
            organization.Property(o => o.Name).HasMaxLength(Organization.NameMaxLength);
            organization.HasOne(o => o.Plan).WithMany().HasForeignKey(o => o.PlanId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Account>(account =>
        {
            account.Property(a => a.Email).HasMaxLength(Account.EmailMaxLength);
            account.HasIndex(a => a.Email).IsUnique();
            account.Property(a => a.FirstName).HasMaxLength(Account.NameMaxLength);
            account.Property(a => a.LastName).HasMaxLength(Account.NameMaxLength);
            account.Property(a => a.PasswordHash).HasMaxLength(200);
            account.Property(a => a.InvitationTokenHash).HasMaxLength(64);
            account.Property(a => a.InvitationCodeHash).HasMaxLength(64);
            account.Property(a => a.InvitationExpiresAt).HasColumnType("timestamp with time zone");
            account.Property(a => a.UnlockTokenHash).HasMaxLength(64);
            account.Property(a => a.UnlockExpiresAt).HasColumnType("timestamp with time zone");
            account.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();
            account.Property(a => a.Role).HasConversion<string>().HasMaxLength(20);
            account.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);
            account.HasOne<Organization>().WithMany().HasForeignKey(a => a.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ExternalLogin>(login =>
        {
            login.Property(l => l.Provider).HasMaxLength(ExternalLogin.ProviderMaxLength);
            login.Property(l => l.ProviderKey).HasMaxLength(ExternalLogin.ProviderKeyMaxLength);
            login.HasIndex(l => new { l.Provider, l.ProviderKey }).IsUnique();
            login.HasOne<Account>().WithMany().HasForeignKey(l => l.AccountId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ResourceType>(type =>
        {
            type.Property(t => t.Name).HasMaxLength(ResourceType.NameMaxLength);
            type.HasIndex(t => t.Name).IsUnique().HasFilter("\"OrganizationId\" IS NULL");
            type.HasIndex(t => new { t.OrganizationId, t.Name }).IsUnique().HasFilter("\"OrganizationId\" IS NOT NULL");
            type.HasOne<Organization>().WithMany().HasForeignKey(t => t.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            type.HasQueryFilter(t => t.OrganizationId == null || t.OrganizationId == CurrentOrganizationId);
            type.HasData(
                new ResourceType { Id = EquipmentTypeId, Name = "Equipment" },
                new ResourceType { Id = VehicleTypeId, Name = "Vehicle" },
                new ResourceType { Id = UtensilTypeId, Name = "Utensil" },
                new ResourceType { Id = OtherTypeId, Name = "Other" });
        });

        modelBuilder.Entity<Resource>(resource =>
        {
            resource.Property(r => r.Name).HasMaxLength(Resource.NameMaxLength);
            resource.Property(r => r.Description).HasMaxLength(Resource.DescriptionMaxLength);
            resource.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
            resource.HasOne<ResourceType>().WithMany().HasForeignKey(r => r.TypeId).OnDelete(DeleteBehavior.Restrict);
            resource.HasOne<Organization>().WithMany().HasForeignKey(r => r.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            resource.HasQueryFilter(r => r.OrganizationId == CurrentOrganizationId);
        });
    }
}
