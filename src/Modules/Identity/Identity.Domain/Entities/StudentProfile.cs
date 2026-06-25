namespace Identity.Domain.Entities;

/// Holds student-specific profile data for a user with the Student role.
public class StudentProfile
{
    public Guid UserId { get; private set; }
    public string? Major { get; private set; }

    private StudentProfile() { }

    internal static StudentProfile Create(Guid userId) =>
        new() { UserId = userId, Major = null };

    internal void UpdateMajor(string? major)
    {
        Major = string.IsNullOrWhiteSpace(major) ? null : major.Trim();
    }
}