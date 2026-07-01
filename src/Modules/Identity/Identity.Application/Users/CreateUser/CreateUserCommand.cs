using Identity.Domain.Entities;
using Identity.Domain.Errors;
using Identity.Domain.Repositories;
using Identity.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Identity;
using SharedKernel.Primitives;

namespace Identity.Application.Users.CreateUser;

public sealed record CreateUserCommand(
    string FullName,
    string Email,
    string Role,
    string Password) : IRequest<Result<CreateUserOutputDto>>;

public sealed class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Result<CreateUserOutputDto>> {
    private readonly UserManager<AppUser> _userManager;
    private readonly IUserRepository _userRepository;
    private readonly IPublisher _publisher;

    public CreateUserCommandHandler(
        UserManager<AppUser> userManager,
        IUserRepository userRepository,
        IPublisher publisher) {
        _userManager = userManager;
        _userRepository = userRepository;
        _publisher = publisher;
    }

    public async Task<Result<CreateUserOutputDto>> Handle(
        CreateUserCommand request,
        CancellationToken cancellationToken) {
        var role = Enum.Parse<UserRole>(request.Role, ignoreCase: true);

        var emailExists = await _userRepository.ExistsByEmailAsync(
            request.Email, cancellationToken: cancellationToken);

        if (emailExists)
            return Result.Failure<CreateUserOutputDto>(UserErrors.EmailAlreadyInUse);

        // Domain factory: validate fullName, khởi tạo User + AttachProfile tương ứng.
        var createResult = AppUser.Create(
            id: Guid.NewGuid(),
            email: request.Email,
            fullName: request.FullName,
            role: role);

        if (createResult.IsFailure)
            return Result.Failure<CreateUserOutputDto>(createResult.Error);

        var user = createResult.Value;

        // UserManager.CreateAsync: hash password + persist User và Profile (cascade).
        // Nếu thất bại (password policy vi phạm...) trả lỗi từ Identity.
        var identityResult = await _userManager.CreateAsync(user, request.Password);

        if (!identityResult.Succeeded) {
            var description = identityResult.Errors.FirstOrDefault()?.Description
                ?? "Tạo tài khoản thất bại.";
            return Result.Failure<CreateUserOutputDto>(
                Error.Create("User.CreateFailed", description));
        }

        // Gán role vào AspNetUserRoles để [Authorize(Roles="...")] hoạt động đúng.
        await _userManager.AddToRoleAsync(user, role.ToString());

        // Publish event sau khi persist thành công
        foreach (var domainEvent in user.DomainEvents)
            await _publisher.Publish(domainEvent, cancellationToken);

        user.ClearDomainEvents();

        return Result.Success(new CreateUserOutputDto(
            UserId: user.Id,
            FullName: user.FullName,
            Email: user.Email!,
            Role: user.Role.ToString(),
            IsActive: user.IsActive,
            CreatedAt: user.CreatedAt));
    }
}