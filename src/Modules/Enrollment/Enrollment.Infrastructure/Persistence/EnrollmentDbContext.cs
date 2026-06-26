using Enrollment.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using SharedInfrastructure.Outbox;
using SharedInfrastructure.Persistence;

namespace Enrollment.Infrastructure.Persistence;

// Kế thừa BaseDbContext để được Outbox dispatch tự động trên SaveChangesAsync.
public class EnrollmentDbContext : BaseDbContext {
    public DbSet<Domain.Entities.Enrollment> Enrollments => Set<Domain.Entities.Enrollment>();
    public DbSet<EnrolledSession> EnrolledSessions => Set<EnrolledSession>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public EnrollmentDbContext(DbContextOptions<EnrollmentDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EnrollmentDbContext).Assembly);
    }

    protected override void AddOutboxMessage(OutboxMessage message) {
        OutboxMessages.Add(message);
    }
}