namespace Identity.Domain.Entities;

/// Holds lecturer-specific profile data for a user with the Lecturer role.
public class LecturerProfile
{
    public Guid UserId { get; private set; }
    public string? Department { get; private set; }
    
    private LecturerProfile() { }
    
    internal static LecturerProfile Create(Guid userId) =>
        new() { UserId = userId, Department = null };
    
    internal void UpdateDepartment(string? department)
    {
        Department = string.IsNullOrWhiteSpace(department) ? null : department.Trim();
    }
}