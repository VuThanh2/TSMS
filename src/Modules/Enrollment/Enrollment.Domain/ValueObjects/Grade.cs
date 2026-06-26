using Enrollment.Domain.Errors;
using SharedKernel.Primitives;

namespace Enrollment.Domain.ValueObjects;

// Grade không kế thừa ValueObject base class vì GetEqualityComponents()
// với yield return làm cho mọi instance bằng nhau khi yield break.
public sealed class Grade : IEquatable<Grade> {
    public const decimal MinValue = 0m;
    public const decimal MaxValue = 10m;

    public decimal Value { get; }

    private Grade(decimal value) => Value = value;

    // Validate range [0.00, 10.00] — tối đa 2 chữ số thập phân.
    public static Result<Grade> Create(decimal value) {
        if (value < MinValue || value > MaxValue)
            return Result.Failure<Grade>(EnrollmentErrors.GradeOutOfRange);

        var rounded = Math.Round(value, 2, MidpointRounding.AwayFromZero);

        return Result.Success(new Grade(rounded));
    }

    public bool Equals(Grade? other) {
        if (other is null) return false;
        return Value == other.Value;
    }

    public override bool Equals(object? obj) => Equals(obj as Grade);

    public override int GetHashCode() => Value.GetHashCode();

    public static bool operator ==(Grade? left, Grade? right) {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(Grade? left, Grade? right) => !(left == right);

    public override string ToString() => Value.ToString("F2");
}