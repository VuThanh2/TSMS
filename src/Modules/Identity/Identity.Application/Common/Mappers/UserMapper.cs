using Identity.Application.Users.GetUserById;
using Identity.Application.Users.GetUsers;
using Identity.Application.Users.UpdateUser;
using Identity.Domain.Entities;
using Identity.Domain.ValueObjects;

namespace Identity.Application.Common.Mappers;

public static class UserMapper {
    public static GetUsersOutputDto ToGetUsersOutputDto(AppUser user) =>
        new(
            UserId: user.Id,
            FullName: user.FullName,
            Email: user.Email!,
            Role: user.Role.ToString(),
            IsActive: user.IsActive);

    public static GetUserByIdOutputDto ToGetUserByIdOutputDto(AppUser user) =>
        new(
            UserId: user.Id,
            FullName: user.FullName,
            Email: user.Email!,
            Role: user.Role.ToString(),
            IsActive: user.IsActive,
            CreatedAt: user.CreatedAt,
            Profile: BuildProfile(user));
    
    public static UpdateUserOutputDto ToUpdateUserOutputDto(AppUser user) =>
        new(
            UserId: user.Id,
            FullName: user.FullName,
            Email: user.Email!,
            Role: user.Role.ToString(),
            Profile: BuildProfile(user));

    // profile null nếu Admin, có giá trị nếu Lecturer hoặc Student.
    private static UserProfileDto? BuildProfile(AppUser user) =>
        user.Role switch {
            UserRole.Lecturer => new UserProfileDto(
                Department: user.LecturerProfile?.Department,
                Major: null),
            UserRole.Student => new UserProfileDto(
                Department: null,
                Major: user.StudentProfile?.Major),
            _ => null
        };
}