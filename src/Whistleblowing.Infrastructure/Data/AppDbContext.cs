using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Whistleblowing.Domain.Common;
using Whistleblowing.Domain.Entities;

namespace Whistleblowing.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Complaint> Complaints => Set<Complaint>();
    public DbSet<Case> Cases => Set<Case>();
    public DbSet<CaseInvestigator> CaseInvestigators => Set<CaseInvestigator>();
    public DbSet<Evidence> Evidence => Set<Evidence>();
    public DbSet<InvestigationReport> InvestigationReports => Set<InvestigationReport>();
    public DbSet<AdditionalInformation> AdditionalInformation => Set<AdditionalInformation>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<CaseStatusHistory> CaseStatusHistories => Set<CaseStatusHistory>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Apply all configurations from this assembly
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Global soft-delete filter — EXCLUDES ApplicationUser because it is
        // the required end of several relationships (CaseInvestigator,
        // Notification, CaseStatusHistory). Filtering it globally causes EF
        // Core warnings + potential missing-related-entity errors.
        var excludedFromFilter = new HashSet<Type>
        {
            typeof(Whistleblowing.Domain.Entities.ApplicationUser),
        };

        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (excludedFromFilter.Contains(entityType.ClrType))
                continue;

            if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
                var property = System.Linq.Expressions.Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
                var filter = System.Linq.Expressions.Expression.Lambda(
                    System.Linq.Expressions.Expression.Equal(property, System.Linq.Expressions.Expression.Constant(false)),
                    parameter);
                builder.Entity(entityType.ClrType).HasQueryFilter(filter);
            }
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditInfo();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        ApplyAuditInfo();
        return base.SaveChanges();
    }

    private void ApplyAuditInfo()
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is IAuditable auditable)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        auditable.CreatedAt = now;
                        break;
                    case EntityState.Modified:
                        auditable.UpdatedAt = now;
                        break;
                }
            }

            if (entry.Entity is ISoftDeletable soft)
            {
                if (entry.State == EntityState.Deleted)
                {
                    // Convert hard delete to soft delete
                    entry.State = EntityState.Modified;
                    soft.IsDeleted = true;
                    soft.DeletedAt = now;
                }
            }
        }
    }
}