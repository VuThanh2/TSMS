namespace Identity.Application.Users.CreateUser;

public sealed record CreateUserInputDto(
    string FullName,
    string Email,
    string Role,
    string Password);

// Response không bao gồm profile vì luôn null tại thời điểm tạo
public sealed record CreateUserOutputDto(
    Guid UserId,
    string FullName,
    string Email,
    string Role,
    bool IsActive,
    DateTime CreatedAt);