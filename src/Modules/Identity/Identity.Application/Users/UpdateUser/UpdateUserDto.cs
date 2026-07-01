using Identity.Application.Users.GetUserById;

namespace Identity.Application.Users.UpdateUser;

public sealed record UpdateUserInputDto(
    string FullName,
    string Email,
    string? Department,
    string? Major);

// Reuse UserProfileDto từ GetUserById — cùng shape, tránh duplicate record.
public sealed record UpdateUserOutputDto(
    Guid UserId,
    string FullName,
    string Email,
    string Role,
    UserProfileDto? Profile);