using Identity.Domain.Errors;
using SharedKernel.Abstractions;
using SharedKernel.Primitives;

namespace Identity.Domain.ValueObjects;

/// Represents the full name of a user.
/// Enforces non-empty and maximum length invariants at construction time.
public sealed class FullName : ValueObject
{
    public const int MaxLength = 100;

    public string Value { get; }

    private FullName(string value) => Value = value;

    public static Result<FullName> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Failure<FullName>(UserErrors.FullNameIsRequired);

        if (value.Trim().Length > MaxLength)
            return Result.Failure<FullName>(UserErrors.FullNameTooLong);

        return Result.Success(new FullName(value.Trim()));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}