using CourseManagement.Domain.Errors;
using SharedKernel.Abstractions;
using SharedKernel.Primitives;

namespace CourseManagement.Domain.ValueObjects;

public sealed class CourseName : ValueObject {
    public const int MaxLength = 200;

    public string Value { get; }

    private CourseName(string value) => Value = value;

    public static Result<CourseName> Create(string? value) {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Failure<CourseName>(CourseErrors.CourseNameIsRequired);

        if (value.Trim().Length > MaxLength)
            return Result.Failure<CourseName>(CourseErrors.CourseNameTooLong);

        return Result.Success(new CourseName(value.Trim()));
    }

    protected override IEnumerable<object?> GetEqualityComponents() {
        yield return Value;
    }

    public override string ToString() => Value;
}