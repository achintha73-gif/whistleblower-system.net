using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Whistleblowing.Domain.Entities;

namespace Whistleblowing.Infrastructure.Data.Configurations;

public class ComplaintConfiguration : IEntityTypeConfiguration<Complaint>
{
    public void Configure(EntityTypeBuilder<Complaint> builder)
    {
        builder.ToTable("Complaints");

        builder.Property(c => c.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Description)
            .IsRequired()
            .HasColumnType("TEXT");

        builder.Property(c => c.Category)
            .HasMaxLength(100);

        builder.Property(c => c.ReferenceCode)
            .HasMaxLength(20);

        builder.HasIndex(c => c.ReferenceCode)
            .IsUnique()
            .HasFilter("[ReferenceCode] IS NOT NULL");

        // User relationship
        builder.HasOne(c => c.User)
            .WithMany(u => u.Complaints)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // 1:1 with Case
        builder.HasOne(c => c.Case)
            .WithOne(cs => cs.Complaint)
            .HasForeignKey<Case>(cs => cs.ComplaintId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}