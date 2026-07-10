using SharedKernel.Primitives;

namespace CourseManagement.Domain.Errors;

public static class CourseErrors {
    // ── CourseName
    public static readonly Error CourseNameIsRequired =
        Error.Create("Course.CourseNameIsRequired", "Course name must not be empty.");

    public static readonly Error CourseNameTooLong =
        Error.Create("Course.CourseNameTooLong",
            $"Course name must not exceed {ValueObjects.CourseName.MaxLength} characters.");

    // ── DateRange
    public static readonly Error StartDateMustBeInFuture =
        Error.Create("Course.StartDateMustBeInFuture",
            "Start date must be today or a future date.");

    public static readonly Error EndDateMustBeAfterStartDate =
        Error.Create("Course.EndDateMustBeAfterStartDate",
            "End date must be strictly after start date.");

    public static readonly Error EndDateUpdateMustBeInFuture =
        Error.Create("Course.EndDateUpdateMustBeInFuture",
            "The new end date must be a future date.");

    public static readonly Error EndDatePrecedesExistingClassSession =
        Error.Create("Course.EndDatePrecedesExistingClassSession",
            "The new end date cannot precede the date of an existing class session.");

    // ── MaxCapacity
    public static readonly Error MaxCapacityMustBePositive =
        Error.Create("Course.MaxCapacityMustBePositive",
            "Max capacity must be a positive integer.");

    public static readonly Error MaxCapacityBelowEnrolledCount =
        Error.Create("Course.MaxCapacityBelowEnrolledCount",
            "New max capacity cannot be less than the current number of enrolled students.");

    // ── Course lifecycle
    public static readonly Error NotFound =
        Error.Create("Course.NotFound", "Course was not found.");

    public static readonly Error CompletedCourseIsImmutable =
        Error.Create("Course.CompletedCourseIsImmutable",
            "A completed course cannot be modified.");

    public static readonly Error InvalidStatusTransition =
        Error.Create("Course.InvalidStatusTransition",
            "Course status can only transition forward: Upcoming → Active → Completed.");

    // ── Delete
    public static readonly Error OnlyUpcomingCourseCanBeDeleted =
        Error.Create("Course.OnlyUpcomingCourseCanBeDeleted",
            "Only an upcoming course (not yet started) can be deleted.");

    public static readonly Error CourseHasEnrollments =
        Error.Create("Course.CourseHasEnrollments",
            "Cannot delete a course that students are already enrolled in.");

    // ── Lecturer
    public static readonly Error LecturerNotFound =
        Error.Create("Course.LecturerNotFound",
            "The specified lecturer was not found or is not active.");

    public static readonly Error LecturerAlreadyAssigned =
        Error.Create("Course.LecturerAlreadyAssigned",
            "The specified lecturer is already assigned to this course.");

    public static readonly Error LecturerDateRangeOverlap =
        Error.Create("Course.LecturerDateRangeOverlap",
            "The lecturer already has a course during the specified date range.");

    // ── ClassSession
    public static readonly Error ClassSessionNotFound =
        Error.Create("Course.ClassSessionNotFound", "Class session was not found.");

    public static readonly Error DuplicateClassSession =
        Error.Create("Course.DuplicateClassSession",
            "A class session with the same date and session type already exists in this course.");

    public static readonly Error ClassSessionOutsideDateRange =
        Error.Create("Course.ClassSessionOutsideDateRange",
            "Session date must fall within the course's start and end dates.");

    public static readonly Error CannotModifyPastClassSession =
        Error.Create("Course.CannotModifyPastClassSession",
            "A class session that has already passed cannot be modified or deleted.");

    public static readonly Error ClassSessionAlreadyCancelled =
        Error.Create("Course.ClassSessionAlreadyCancelled",
            "This class session has already been cancelled.");

    // ── WeeklySlot
    public static readonly Error DuplicateWeeklySlot =
        Error.Create("Course.DuplicateWeeklySlot",
            "A weekly slot with the same day of week and session type already exists in this course.");

    public static readonly Error WeeklySlotNotFound =
        Error.Create("Course.WeeklySlotNotFound", "Weekly slot was not found.");

    public static readonly Error MinimumWeeklySlotsRequired =
        Error.Create("Course.MinimumWeeklySlotsRequired",
            "A course must have at least 2 weekly slots. Cannot remove the last two.");

    public static readonly Error WeeklySlotInUse =
        Error.Create("Course.WeeklySlotInUse",
            "Cannot remove a weekly slot that students are currently enrolled in.");
}