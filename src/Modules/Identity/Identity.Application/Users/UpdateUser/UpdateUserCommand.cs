using Identity.Application.Common.Mappers;
using Identity.Domain.Errors;
using Identity.Domain.Repositories;
using Identity.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Identity;
using SharedKernel.Primitives;

namespace Identity.Application.Users.UpdateUser;

public sealed record UpdateUserCommand(
    Guid UserId,
    string FullName,
    string Email,
    string? Department,
    string? Major) : IRequest<Result<UpdateUserOutputDto>>;

public sealed class UpdateUserCommandHandler
    : IRequestHandler<UpdateUserCommand, Result<UpdateUserOutputDto>> {
    private readonly IUserRepository _userRepository;
    private readonly UserManager<Domain.Entities.AppUser> _userManager;
    private readonly IPublisher _publisher;

    public UpdateUserCommandHandler(
        IUserRepository userRepository,
        UserManager<Domain.Entities.AppUser> userManager,
        IPublisher publisher) {
        _userRepository = userRepository;
        _userManager = userManager;
        _publisher = publisher;
    }

    public async Task<Result<UpdateUserOutputDto>> Handle(
        UpdateUserCommand request,
        CancellationToken cancellationToken) {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
            return Result.Failure<UpdateUserOutputDto>(UserErrors.NotFound);

        // Email uniqueness check — loại trừ chính user đang được cập nhật.
        var emailTaken = await _userRepository.ExistsByEmailAsync(
            request.Email,
            excludeUserId: request.UserId,
            cancellationToken: cancellationToken);

        if (emailTaken)
            return Result.Failure<UpdateUserOutputDto>(UserErrors.EmailAlreadyInUse);

        // Domain method tự route profileField đúng theo Role:
        //   Lecturer → UpdateDepartment, Student → UpdateMajor, Admin → no-op.
        var profileField = user.Role switch {
            UserRole.Lecturer => request.Department,
            UserRole.Student  => request.Major,
            _                                     => null
        };

        var updateResult = user.UpdateInfo(request.FullName, request.Email, profileField);

        if (updateResult.IsFailure)
            return Result.Failure<UpdateUserOutputDto>(updateResult.Error);

        // UserManager.UpdateAsync sync lại Email, NormalizedEmail, UserName,
        // NormalizedUserName vào Identity tables — không dùng _userRepository.Update
        // vì Identity cần quản lý concurrency stamp.
        var identityResult = await _userManager.UpdateAsync(user);

        if (!identityResult.Succeeded) {
            var description = identityResult.Errors.FirstOrDefault()?.Description
                ?? "Cập nhật tài khoản thất bại.";
            return Result.Failure<UpdateUserOutputDto>(
                Error.Create("User.UpdateFailed", description));
        }

        foreach (var domainEvent in user.DomainEvents)
            await _publisher.Publish(domainEvent, cancellationToken);

        user.ClearDomainEvents();

        return Result.Success(UserMapper.ToUpdateUserOutputDto(user));
    }
}