using SharedKernel.Primitives;

namespace Reporting.Domain.Errors;

public static class ReportingErrors {
    public static readonly Error CourseNotFound =
        Error.Create("Reporting.CourseNotFound",
            "No report data found for the specified course.");

    public static readonly Error CourseStatisticsNotFound =
        Error.Create("Reporting.CourseStatisticsNotFound",
            "Course statistics projection has not been initialized for this course.");
    
    public static readonly Error NotCourseOwner =
        Error.Create("Reporting.NotCourseOwner",
            "Lecturer is not the owner of this course.");
}