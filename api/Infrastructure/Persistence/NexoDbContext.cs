using Microsoft.EntityFrameworkCore;
using Nexo.Api.Domain.Models;

namespace Nexo.Api.Infrastructure.Persistence;

public class NexoDbContext(DbContextOptions<NexoDbContext> options) : DbContext(options)
{
    public static readonly Guid FreePlanId = new("6f1c2b1e-7a52-4d0a-9a53-1f3e5c0b8d01");
    public static readonly Guid TeamPlanId = new("8b2d4f3a-9c61-4e2b-8a64-2f4e6c0b9d02");
    public static readonly Guid EnterprisePlanId = new("a1e5c7d4-1b83-4f0c-9b75-3a5f7d1c0e03");

    public DbSet<Plan> Plans => Set<Plan>();

    public DbSet<Organization> Organizations => Set<Organization>();

    public DbSet<Account> Accounts => Set<Account>();

    public DbSet<ExternalLogin> ExternalLogins => Set<ExternalLogin>();

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
    }
}
