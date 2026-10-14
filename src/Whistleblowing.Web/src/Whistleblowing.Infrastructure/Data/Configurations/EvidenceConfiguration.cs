using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Whistleblowing.Domain.Entities;

namespace Whistleblowing.Infrastructure.Data.Configurations;

public class EvidenceConfiguration : IEntityTypeConfiguration<Evidence>
{
    public void Configure(EntityTypeBuilder<Evidence> builder)
    {
        builder.ToTable("Evidence");

        builder.Property(e => e.FileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.FileType)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(e => e.FilePath)
            .HasMaxLength(500);

        builder.HasOne(e => e.Case)
            .WithMany(c => c.Evidence)
            .HasForeignKey(e => e.CaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Complaint)
            .WithMany(c => c.Evidence)
            .HasForeignKey(e => e.ComplaintId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.UploadedBy)
            .WithMany(u => u.UploadedEvidence)
            .HasForeignKey(e => e.UploadedById)
            .OnDelete(DeleteBehavior.SetNull);
    }
}