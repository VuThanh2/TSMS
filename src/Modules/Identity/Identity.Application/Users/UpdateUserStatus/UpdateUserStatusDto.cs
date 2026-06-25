namespace Identity.Application.Users.UpdateUserStatus;

public sealed record UpdateUserStatusInputDto(bool IsActive);

public sealed record UpdateUserStatusOutputDto(Guid UserId, bool IsActive);