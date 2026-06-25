namespace Identity.Application.Users.GetUserById;

// profile là null nếu Role = Admin,
// { Department } nếu Lecturer, { Major } nếu Student.
public sealed record GetUserByIdOutputDto(
    Guid UserId,
    string FullName,
    string Email,
    string Role,
    bool IsActive,
    DateTime CreatedAt,
    UserProfileDto? Profile);

public sealed record UserProfileDto(string? Department, string? Major);