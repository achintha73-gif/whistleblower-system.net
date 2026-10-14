using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Whistleblowing.Domain.Entities;

namespace Whistleblowing.Infrastructure.Data.Configurations;

public class InvestigationReportConfiguration : IEntityTypeConfiguration<InvestigationReport>
{
    public void Configure(EntityTypeBuilder<InvestigationReport> builder)
    {
        builder.ToTable("InvestigationReports");
        builder.Property(r => r.Findings).IsRequired().HasColumnType("TEXT");
        builder.Property(r => r.Recommendation).IsRequired().HasColumnType("TEXT");

        builder.HasOne(r => r.Case)
            .WithOne(c => c.InvestigationReport)
            .HasForeignKey<InvestigationReport>(r => r.CaseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}