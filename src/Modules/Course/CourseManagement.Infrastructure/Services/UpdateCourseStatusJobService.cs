using CourseManagement.Domain.ValueObjects;
using CourseManagement.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CourseManagement.Infrastructure.Services;

/// Hangfire recurring job — runs daily to auto-transition course statuses.
public class UpdateCourseStatusJobService {
    private readonly CourseDbContext _context;
    private readonly IPublisher _publisher;
    private readonly ILogger<UpdateCourseStatusJobService> _logger;

    public UpdateCourseStatusJobService(
        CourseDbContext context,
        IPublisher publisher,
        ILogger<UpdateCourseStatusJobService> logger) {
        _context = context;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default) {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var candidates = await _context.Courses
            .Where(c => c.Status == CourseStatus.Upcoming || c.Status == CourseStatus.Active)
            .ToListAsync(cancellationToken);

        // Phase 1: Upcoming → Active (StartDate has passed)
        var toActivate = candidates
            .Where(c => c.Status == CourseStatus.Upcoming && c.StartDate <= today)
            .ToList();

        foreach (var course in toActivate) {
            var result = course.TransitionStatus(CourseStatus.Active);

            if (result.IsFailure) {
                _logger.LogWarning(
                    "Failed to activate course {CourseId}: {Error}",
                    course.Id, result.Error.Message);
                continue;
            }

            _logger.LogInformation("Course {CourseId} transitioned to Active.", course.Id);
        }

        // Phase 2: Active → Completed (EndDate has passed)
        // Re-check status after Phase 1 — candidates that just became Active are excluded.
        var toComplete = candidates
            .Where(c => c.Status == CourseStatus.Active && c.EndDate < today)
            .ToList();

        foreach (var course in toComplete) {
            var result = course.TransitionStatus(CourseStatus.Completed);

            if (result.IsFailure) {
                _logger.LogWarning(
                    "Failed to complete course {CourseId}: {Error}",
                    course.Id, result.Error.Message);
                continue;
            }

            _logger.LogInformation("Course {CourseId} transitioned to Completed.", course.Id);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}