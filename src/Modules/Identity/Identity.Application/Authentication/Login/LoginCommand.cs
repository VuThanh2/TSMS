using Identity.Application.Common.Interfaces;
using Identity.Domain.Errors;
using MediatR;
using Microsoft.AspNetCore.Identity;
using SharedKernel.Primitives;

namespace Identity.Application.Authentication.Login;

public sealed record LoginCommand(string Email, string Password) : IRequest<Result<LoginOutputDto>>;

// Không dùng MapIdentityApi vì cần JWT custom claims (role, fullName, isActive).
// SignInManager.CheckPasswordSignInAsync tự xử lý LockoutEnd = MaxValue
// (deactivated user) khi LockoutEnabled = true trên AppUser.
public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginOutputDto>> {
    private readonly UserManager<Domain.Entities.AppUser> _userManager;
    private readonly SignInManager<Domain.Entities.AppUser> _signInManager;
    private readonly ITokenService _tokenService;

    public LoginCommandHandler(
        UserManager<Domain.Entities.AppUser> userManager,
        SignInManager<Domain.Entities.AppUser> signInManager,
        ITokenService tokenService) {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
    }

    public async Task<Result<LoginOutputDto>> Handle(
        LoginCommand request,
        CancellationToken cancellationToken) {
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user is null)
            return Result.Failure<LoginOutputDto>(UserErrors.InvalidCredentials);

        var result = await _signInManager.CheckPasswordSignInAsync(
            user, request.Password, lockoutOnFailure: false);

        if (result.IsLockedOut)
            return Result.Failure<LoginOutputDto>(UserErrors.AccountIsInactive);

        if (!result.Succeeded)
            return Result.Failure<LoginOutputDto>(UserErrors.InvalidCredentials);

        var token = _tokenService.GenerateToken(user);

        return Result.Success(new LoginOutputDto(token));
    }
}