using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Whistleblowing.Domain.Entities;

namespace Whistleblowing.Infrastructure.Data.Configurations;

public class CaseConfiguration : IEntityTypeConfiguration<Case>
{
    public void Configure(EntityTypeBuilder<Case> builder)
    {
        builder.ToTable("Cases");

        builder.HasOne(c => c.AssignedInvestigator)
            .WithMany(u => u.AssignedCases)
            .HasForeignKey(c => c.AssignedInvestigatorId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(c => c.Complaint)
            .WithOne(cp => cp.Case)
            .HasForeignKey<Case>(c => c.ComplaintId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.InvestigationReport)
            .WithOne(r => r.Case)
            .HasForeignKey<InvestigationReport>(r => r.CaseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}