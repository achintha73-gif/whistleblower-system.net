using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Whistleblowing.Domain.Entities;

namespace Whistleblowing.Infrastructure.Data.Configurations;

public class CaseInvestigatorConfiguration : IEntityTypeConfiguration<CaseInvestigator>
{
    public void Configure(EntityTypeBuilder<CaseInvestigator> builder)
    {
        builder.ToTable("CaseInvestigators");
        builder.HasIndex(ci => new { ci.CaseId, ci.InvestigatorId }).IsUnique();

        builder.HasOne(ci => ci.Case)
            .WithMany(c => c.Investigators)
            .HasForeignKey(ci => ci.CaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ci => ci.Investigator)
            .WithMany(u => u.InvestigatorAssignments)
            .HasForeignKey(ci => ci.InvestigatorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}