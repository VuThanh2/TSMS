using Identity.Application.Common.Interfaces;
using Identity.Domain.Errors;
using Identity.Domain.Repositories;
using Identity.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Identity;
using SharedKernel.Primitives;

namespace Identity.Application.Users.UpdateUserStatus;

// currentUserId: ID của Admin đang thực hiện, resolve từ JWT claim ở Presentation Layer.
public sealed record UpdateUserStatusCommand(
    Guid TargetUserId,
    bool IsActive,
    Guid CurrentUserId) : IRequest<Result<UpdateUserStatusOutputDto>>;

// LockoutEnd phải được sync với Identity để /login check lockout đúng:
//   Deactivate → SetLockoutEndDateAsync(MaxValue)
//   Activate   → SetLockoutEndDateAsync(null)
public sealed class UpdateUserStatusCommandHandler
    : IRequestHandler<UpdateUserStatusCommand, Result<UpdateUserStatusOutputDto>> {
    private readonly IUserRepository _userRepository;
    private readonly UserManager<Domain.Entities.AppUser> _userManager;
    private readonly ICourseLookupService _courseLookup;
    private readonly IEnrollmentIdentityService _enrollmentIdentity;
    private readonly IPublisher _publisher;

    public UpdateUserStatusCommandHandler(
        IUserRepository userRepository,
        UserManager<Domain.Entities.AppUser> userManager,
        ICourseLookupService courseLookup,
        IEnrollmentIdentityService enrollmentIdentity,
        IPublisher publisher) {
        _userRepository = userRepository;
        _userManager = userManager;
        _courseLookup = courseLookup;
        _enrollmentIdentity = enrollmentIdentity;
        _publisher = publisher;
    }

    public async Task<Result<UpdateUserStatusOutputDto>> Handle(
        UpdateUserStatusCommand request,
        CancellationToken cancellationToken) {
        var user = await _userRepository.GetByIdAsync(request.TargetUserId, cancellationToken);

        if (user is null)
            return Result.Failure<UpdateUserStatusOutputDto>(UserErrors.NotFound);

        if (request.IsActive)
            return await ActivateAsync(user, cancellationToken);

        return await DeactivateAsync(user, request.CurrentUserId, cancellationToken);
    }

    // ── Private helpers

    private async Task<Result<UpdateUserStatusOutputDto>> ActivateAsync(
        Domain.Entities.AppUser user,
        CancellationToken cancellationToken) {
        var domainResult = user.Activate();

        if (domainResult.IsFailure)
            return Result.Failure<UpdateUserStatusOutputDto>(domainResult.Error);

        await _userManager.UpdateAsync(user);

        // Xóa lockout để user có thể login lại.
        await _userManager.SetLockoutEndDateAsync(user, null);

        foreach (var domainEvent in user.DomainEvents)
            await _publisher.Publish(domainEvent, cancellationToken);

        user.ClearDomainEvents();

        return Result.Success(new UpdateUserStatusOutputDto(user.Id, user.IsActive));
    }

    private async Task<Result<UpdateUserStatusOutputDto>> DeactivateAsync(
        Domain.Entities.AppUser user,
        Guid currentUserId,
        CancellationToken cancellationToken) {
        if (user.Id == currentUserId)
            return Result.Failure<UpdateUserStatusOutputDto>(UserErrors.CannotDeactivateSelf);
 
        if (user.Role == UserRole.Lecturer) {
            var hasActiveCourses = await _courseLookup.HasActiveCoursesByLecturerAsync(
                user.Id, cancellationToken);
 
            if (hasActiveCourses)
                return Result.Failure<UpdateUserStatusOutputDto>(UserErrors.LecturerHasActiveCourses);
        }
 
        if (user.Role == UserRole.Student) {
            // Step 1: hỏi Enrollment BC — courseIds nào Student đang Active enroll.
            var activeCourseIds = await _enrollmentIdentity.GetActiveCourseIdsByStudentAsync(
                user.Id, cancellationToken);
 
            // Step 2: hỏi Course BC — trong số đó có courseId nào đang Active không.
            // Application layer orchestrate 2 cross-BC calls — đúng trách nhiệm.
            if (activeCourseIds.Count > 0) {
                var hasActiveCourse = await _courseLookup.AreAnyActiveAsync(
                    activeCourseIds, cancellationToken);
 
                if (hasActiveCourse)
                    return Result.Failure<UpdateUserStatusOutputDto>(
                        UserErrors.StudentHasActiveEnrollments);
            }
        }
 
        var domainResult = user.Deactivate();
 
        if (domainResult.IsFailure)
            return Result.Failure<UpdateUserStatusOutputDto>(domainResult.Error);
 
        await _userManager.UpdateAsync(user);
        await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
 
        foreach (var domainEvent in user.DomainEvents)
            await _publisher.Publish(domainEvent, cancellationToken);
 
        user.ClearDomainEvents();
 
        return Result.Success(new UpdateUserStatusOutputDto(user.Id, user.IsActive));
    }
}