using CourseManagement.Domain.Errors;
using SharedKernel.Abstractions;
using SharedKernel.Primitives;

namespace CourseManagement.Domain.ValueObjects;

public sealed class DateRange : ValueObject {
    public DateOnly StartDate { get; }
    public DateOnly EndDate { get; }

    private DateRange(DateOnly startDate, DateOnly endDate) {
        StartDate = startDate;
        EndDate = endDate;
    }

    /// Full creation — validates both StartDate and EndDate.
    public static Result<DateRange> Create(DateOnly startDate, DateOnly endDate) {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (startDate < today)
            return Result.Failure<DateRange>(CourseErrors.StartDateMustBeInFuture);

        if (endDate <= startDate)
            return Result.Failure<DateRange>(CourseErrors.EndDateMustBeAfterStartDate);

        return Result.Success(new DateRange(startDate, endDate));
    }

    /// Produces a new DateRange keeping the existing StartDate, replacing EndDate.
    /// Caller (Domain) must also check EndDate >= latest ClassSession date before calling this.
    public static Result<DateRange> WithNewEndDate(DateOnly existingStartDate, DateOnly newEndDate) {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (newEndDate <= today)
            return Result.Failure<DateRange>(CourseErrors.EndDateUpdateMustBeInFuture);

        if (newEndDate <= existingStartDate)
            return Result.Failure<DateRange>(CourseErrors.EndDateMustBeAfterStartDate);

        return Result.Success(new DateRange(existingStartDate, newEndDate));
    }

    protected override IEnumerable<object?> GetEqualityComponents() {
        yield return StartDate;
        yield return EndDate;
    }
}