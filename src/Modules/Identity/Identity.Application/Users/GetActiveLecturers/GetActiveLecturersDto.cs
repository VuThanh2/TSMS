namespace Identity.Application.Users.GetActiveLecturers;

public sealed record LecturerOptionDto(
    Guid UserId,
    string FullName,
    string Email);