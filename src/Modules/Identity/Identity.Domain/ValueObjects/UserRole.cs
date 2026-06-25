namespace Identity.Domain.ValueObjects;

/// Represents the role of a user within the system.
/// Each value maps 1-to-1 with a seeded row in AspNetRoles ("Admin", "Lecturer", "Student").
public enum UserRole
{
    Admin = 1,
    Lecturer = 2,
    Student = 3
}