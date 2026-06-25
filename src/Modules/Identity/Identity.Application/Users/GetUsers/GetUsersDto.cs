namespace Identity.Application.Users.GetUsers;

public sealed record GetUsersOutputDto(
    Guid UserId,
    string FullName,
    string Email,
    string Role,
    bool IsActive);