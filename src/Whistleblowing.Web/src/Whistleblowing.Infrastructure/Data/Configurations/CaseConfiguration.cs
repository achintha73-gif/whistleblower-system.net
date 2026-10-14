using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Whistleblowing.Domain.Entities;

namespace Whistleblowing.Infrastructure.Data.Configurations;

public class CaseConfiguration : IEntityTypeConfiguration<Case>
{
    public void Configure(EntityTypeBuilder<Case> builder)
    {
        builder.ToTable("Cases");

        // Primary investigator
        builder.HasOne(c => c.AssignedInvestigator)
            .WithMany(u => u.AssignedCases)
            .HasForeignKey(c => c.AssignedInvestigatorId)
            .OnDelete(DeleteBehavior.SetNull);

        // Complaint relationship (configured from Complaint side)
        builder.HasOne(c => c.Complaint)
            .WithOne(cp => cp.Case)
            .HasForeignKey<Case>(c => c.ComplaintId)
            .OnDelete(DeleteBehavior.Cascade);

        // 1:1 Investigation report
        builder.HasOne(c => c.InvestigationReport)
            .WithOne(r => r.Case)
            .HasForeignKey<InvestigationReport>(r => r.CaseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}