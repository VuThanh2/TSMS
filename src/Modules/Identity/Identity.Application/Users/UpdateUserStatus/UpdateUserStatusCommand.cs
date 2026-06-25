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
    private readonly IEnrollmentLookupService _enrollmentLookup;
    private readonly IPublisher _publisher;

    public UpdateUserStatusCommandHandler(
        IUserRepository userRepository,
        UserManager<Domain.Entities.AppUser> userManager,
        ICourseLookupService courseLookup,
        IEnrollmentLookupService enrollmentLookup,
        IPublisher publisher) {
        _userRepository = userRepository;
        _userManager = userManager;
        _courseLookup = courseLookup;
        _enrollmentLookup = enrollmentLookup;
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
        // Precondition 1: Admin không thể deactivate chính mình.
        if (user.Id == currentUserId)
            return Result.Failure<UpdateUserStatusOutputDto>(UserErrors.CannotDeactivateSelf);

        // Precondition 2 (Lecturer): không có Course Upcoming/Active.
        if (user.Role == UserRole.Lecturer) {
            var hasActiveCourses = await _courseLookup.HasActiveCoursesByLecturerAsync(
                user.Id, cancellationToken);

            if (hasActiveCourses)
                return Result.Failure<UpdateUserStatusOutputDto>(
                    UserErrors.LecturerHasActiveCourses);
        }

        // Precondition 3 (Student): không có Enrollment trong Course Active.
        if (user.Role == UserRole.Student) {
            var hasActiveEnrollments = await _enrollmentLookup.HasActiveEnrollmentsByStudentAsync(
                user.Id, cancellationToken);

            if (hasActiveEnrollments)
                return Result.Failure<UpdateUserStatusOutputDto>(
                    UserErrors.StudentHasActiveEnrollments);
        }

        var domainResult = user.Deactivate();

        if (domainResult.IsFailure)
            return Result.Failure<UpdateUserStatusOutputDto>(domainResult.Error);

        await _userManager.UpdateAsync(user);

        // Set LockoutEnd = MaxValue để /login tự động reject user bị deactivate.
        await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);

        foreach (var domainEvent in user.DomainEvents)
            await _publisher.Publish(domainEvent, cancellationToken);

        user.ClearDomainEvents();

        return Result.Success(new UpdateUserStatusOutputDto(user.Id, user.IsActive));
    }
}