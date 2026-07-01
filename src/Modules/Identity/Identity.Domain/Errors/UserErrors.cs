using SharedKernel.Primitives;

namespace Identity.Domain.Errors;

/// Centralises all domain-level errors for the Identity Bounded Context.
public static class UserErrors
{
    // ── FullName 
    public static readonly Error FullNameIsRequired =
        Error.Create("User.FullNameIsRequired", "Full name must not be empty.");

    public static readonly Error FullNameTooLong =
        Error.Create("User.FullNameTooLong",
            $"Full name must not exceed {ValueObjects.FullName.MaxLength} characters.");

    // ── Email
    public static readonly Error EmailIsRequired =
        Error.Create("User.EmailIsRequired", "Email must not be empty.");

    public static readonly Error EmailAlreadyInUse =
        Error.Create("User.EmailAlreadyInUse", "This email address is already in use.");

    // ── Password
    public static readonly Error PasswordIsRequired =
        Error.Create("User.PasswordIsRequired", "Password must not be empty.");

    public static readonly Error PasswordTooShort =
        Error.Create("User.PasswordTooShort",
            $"Password must be at least {ValueObjects.Password.MinLength} characters.");
    
    public static readonly Error PasswordResetFailed =
        Error.Create("User.PasswordResetFailed",
            "Failed to reset password. Please try again.");
    
    // ── Authentication
    public static readonly Error InvalidCredentials =
        Error.Create("User.InvalidCredentials", "Email hoặc mật khẩu không đúng.");

    // ── User lifecycle 
    public static readonly Error NotFound =
        Error.Create("User.NotFound", "User was not found.");

    public static readonly Error AlreadyActive =
        Error.Create("User.AlreadyActive", "User account is already active.");

    public static readonly Error AlreadyInactive =
        Error.Create("User.AlreadyInactive", "User account is already inactive.");

    public static readonly Error AccountIsInactive =
        Error.Create("User.AccountIsInactive",
            "This account is inactive and cannot perform this action.");

    // ── Deactivation preconditions (checked at Application Layer)
    public static readonly Error CannotDeactivateSelf =
        Error.Create("User.CannotDeactivateSelf",
            "An admin cannot deactivate their own account.");

    public static readonly Error LecturerHasActiveCourses =
        Error.Create("User.LecturerHasActiveCourses",
            "Lecturer cannot be deactivated while responsible for courses " +
            "in Upcoming or Active status.");

    public static readonly Error StudentHasActiveEnrollments =
        Error.Create("User.StudentHasActiveEnrollments",
            "Student cannot be deactivated while enrolled in a course with Active status.");

    // ── Role
    public static readonly Error InvalidRole =
        Error.Create("User.InvalidRole", "The specified role is not valid.");

    // ── CSV Import 
    public static readonly Error CsvFileTooLarge =
        Error.Create("User.CsvFileTooLarge", "The uploaded CSV file exceeds the maximum allowed size.");

    public static readonly Error CsvInvalidFormat =
        Error.Create("User.CsvInvalidFormat", "The CSV file format is invalid or missing required columns.");
}