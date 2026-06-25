using Identity.Domain.Errors;
using SharedKernel.Primitives;

namespace Identity.Domain.ValueObjects;

// Validation wrapper cho plain-text password trước khi hash.
// Không phải storage type — actual storage là IdentityUser.PasswordHash qua UserManager.
public sealed class Password {
    public const int MinLength = 6;
 
    public string Value { get; }
 
    private Password(string value) => Value = value;
 
    public static Result<Password> Create(string? value) {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Failure<Password>(UserErrors.PasswordIsRequired);
 
        if (value.Length < MinLength)
            return Result.Failure<Password>(UserErrors.PasswordTooShort);
 
        return Result.Success(new Password(value));
    }
}