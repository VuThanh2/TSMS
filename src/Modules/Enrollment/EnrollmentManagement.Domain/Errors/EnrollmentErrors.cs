using SharedKernel.Primitives;

namespace EnrollmentManagement.Domain.Errors;

public static class EnrollmentErrors {
    // ── Enrollment
    public static readonly Error NotFound =
        Error.Create("Enrollment.NotFound", "Enrollment was not found.");
    
    public static readonly Error StudentNotActive =
        Error.Create("Enrollment.StudentNotActive",
            "Student account is not active.");

    public static readonly Error AlreadyEnrolled =
        Error.Create("Enrollment.AlreadyEnrolled",
            "Student is already enrolled in this course.");

    public static readonly Error CourseIsFull =
        Error.Create("Enrollment.CourseIsFull",
            "This course has reached its maximum capacity.");

    public static readonly Error CourseNotEnrollable =
        Error.Create("Enrollment.CourseNotEnrollable",
            "Only courses with Upcoming status are open for enrollment.");

    public static readonly Error InvalidSessionCount =
        Error.Create("Enrollment.InvalidSessionCount",
            "Enrollment requires exactly 2 class sessions.");

    public static readonly Error DuplicateSessionType =
        Error.Create("Enrollment.DuplicateSessionType",
            "The 2 enrolled sessions must have different session types (Morning and Afternoon).");

    public static readonly Error SessionNotInCourse =
        Error.Create("Enrollment.SessionNotInCourse",
            "One or more selected class sessions do not belong to this course.");

    public static readonly Error SessionAlreadyEnrolled =
        Error.Create("Enrollment.SessionAlreadyEnrolled",
            "The selected class session is already part of this enrollment.");

    // ── Grade
    public static readonly Error GradeOutOfRange =
        Error.Create("Enrollment.GradeOutOfRange",
            $"Grade must be between {ValueObjects.Grade.MinValue} and {ValueObjects.Grade.MaxValue}.");

    public static readonly Error AlreadyGraded =
        Error.Create("Enrollment.AlreadyGraded",
            "This enrollment already has a grade. Use update to change it.");

    public static readonly Error NotYetGraded =
        Error.Create("Enrollment.NotYetGraded",
            "This enrollment has not been graded yet.");

    // ── AdjustSession
    public static readonly Error AdjustSessionNotFound =
        Error.Create("Enrollment.AdjustSessionNotFound",
            "The session to be replaced was not found in this enrollment.");
 
    public static readonly Error AdjustSessionTypeDuplicate =
        Error.Create("Enrollment.AdjustSessionTypeDuplicate",
            "Adjusting this session would result in two sessions of the same type.");
 
    public static readonly Error CourseAlreadyCompleted =
        Error.Create("Enrollment.CourseAlreadyCompleted",
            "Session adjustment is not allowed when the course is already completed.");
 
    // ── Grading authorization
    public static readonly Error NotCourseOwner =
        Error.Create("Enrollment.NotCourseOwner",
            "Lecturer is not the owner of this course.");
 
    public static readonly Error CourseNotGradeable =
        Error.Create("Enrollment.CourseNotGradeable",
            "Grading is only allowed when the course is Active or Completed.");
 
    // ── Attendance
    public static readonly Error AttendanceNotFound =
        Error.Create("Enrollment.AttendanceNotFound",
            "Attendance record was not found.");
}