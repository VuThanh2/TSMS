using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

// LecturerProfile dùng UserId làm PK — shared primary key với AppUser (1-to-1).
// Cascade delete được set ở AppUserConfiguration.
public class LecturerProfileConfiguration : IEntityTypeConfiguration<LecturerProfile> {
    public void Configure(EntityTypeBuilder<LecturerProfile> builder) {
        builder.ToTable("LecturerProfiles");

        builder.HasKey(p => p.UserId);

        builder.Property(p => p.UserId)
            .IsRequired();

        builder.Property(p => p.Department)
            .HasMaxLength(200)
            .IsRequired(false);
    }
}