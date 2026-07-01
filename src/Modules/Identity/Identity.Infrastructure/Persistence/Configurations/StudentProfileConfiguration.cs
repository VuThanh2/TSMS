using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

// StudentProfile dùng UserId làm PK — shared primary key với AppUser (1-to-1).
// Cascade delete được set ở AppUserConfiguration.
public class StudentProfileConfiguration : IEntityTypeConfiguration<StudentProfile> {
    public void Configure(EntityTypeBuilder<StudentProfile> builder) {
        builder.ToTable("StudentProfiles");

        builder.HasKey(p => p.UserId);

        builder.Property(p => p.UserId)
            .IsRequired();

        builder.Property(p => p.Major)
            .HasMaxLength(200)
            .IsRequired(false);
    }
}