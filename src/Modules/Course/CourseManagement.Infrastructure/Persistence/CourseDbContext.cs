using CourseManagement.Application.Common.Interfaces;
using CourseManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using SharedInfrastructure.Outbox;
using SharedInfrastructure.Persistence;

namespace CourseManagement.Infrastructure.Persistence;

/// Inherits BaseDbContext to get automatic domain event → Outbox dispatch on SaveChangesAsync.
/// Domain events (CourseCreated, ClassSessionAdded, etc.) are written to OutboxMessages
/// within the same transaction as the domain operation.
public class CourseDbContext : BaseDbContext, ICourseUnitOfWork {
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<ClassSession> ClassSessions => Set<ClassSession>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public CourseDbContext(DbContextOptions<CourseDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CourseDbContext).Assembly);
    }

    protected override void AddOutboxMessage(OutboxMessage message) {
        OutboxMessages.Add(message);
    }
}