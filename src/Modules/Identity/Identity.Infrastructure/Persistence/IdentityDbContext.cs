using Identity.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Persistence;

// Kế thừa IdentityDbContext<AppUser, IdentityRole<Guid>, Guid> để có đủ
// các Identity tables (AspNetUsers, AspNetRoles, AspNetUserRoles...).
//
// Không kế thừa BaseDbContext vì:
//   1. IdentityDbContext<> đã override SaveChangesAsync cho concurrency stamps.
//   2. Identity events được publish qua MediatR trực tiếp ở Application Layer —
//      không cần Outbox scan (in-process Modular Monolith, MediatR delivery đủ reliable).
public class IdentityDbContext : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid> {
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options) { }

    public DbSet<LecturerProfile> LecturerProfiles => Set<LecturerProfile>();
    public DbSet<StudentProfile> StudentProfiles => Set<StudentProfile>();

    protected override void OnModelCreating(ModelBuilder builder) {
        // Phải gọi base trước để Identity configure các tables của nó
        base.OnModelCreating(builder);

        // Apply tất cả IEntityTypeConfiguration trong assembly này
        builder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
    }
}