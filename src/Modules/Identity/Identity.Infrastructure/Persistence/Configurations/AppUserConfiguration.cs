using Identity.Domain.Entities;
using Identity.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

// Identity fields (Email, PasswordHash, LockoutEnd...) đã được
// IdentityDbContext<AppUser> configure sẵn — chỉ cần map phần custom.
public class AppUserConfiguration : IEntityTypeConfiguration<AppUser> {
    public void Configure(EntityTypeBuilder<AppUser> builder) {
        builder.Property(u => u.FullName)
            .HasColumnName("FullName")
            .HasMaxLength(FullName.MaxLength)
            .IsRequired();

        builder.Property(u => u.Role)
            .HasConversion<string>()
            .HasColumnName("Role")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(u => u.IsActive)
            .HasColumnName("IsActive")
            .IsRequired();

        builder.Property(u => u.CreatedAt)
            .HasColumnName("CreatedAt")
            .IsRequired();

        // Navigation properties — configured via their own IEntityTypeConfiguration
        builder.HasOne(u => u.LecturerProfile)
            .WithOne()
            .HasForeignKey<LecturerProfile>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(u => u.StudentProfile)
            .WithOne()
            .HasForeignKey<StudentProfile>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}