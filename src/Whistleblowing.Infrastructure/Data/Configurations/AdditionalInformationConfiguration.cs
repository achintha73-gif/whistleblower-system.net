using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Whistleblowing.Domain.Entities;

namespace Whistleblowing.Infrastructure.Data.Configurations;

public class AdditionalInformationConfiguration : IEntityTypeConfiguration<AdditionalInformation>
{
    public void Configure(EntityTypeBuilder<AdditionalInformation> builder)
    {
        builder.ToTable("AdditionalInformation");
        builder.Property(a => a.Title).IsRequired().HasMaxLength(200);
        builder.Property(a => a.Description).IsRequired().HasColumnType("TEXT");

        builder.HasOne(a => a.Complaint)
            .WithMany(c => c.AdditionalInfo)
            .HasForeignKey(a => a.ComplaintId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.SubmittedBy)
            .WithMany(u => u.AdditionalInfo)
            .HasForeignKey(a => a.SubmittedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}