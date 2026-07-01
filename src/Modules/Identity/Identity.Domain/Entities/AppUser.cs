using Identity.Domain.Errors;
using Identity.Domain.Events;
using Identity.Domain.ValueObjects;
using Microsoft.AspNetCore.Identity;
using SharedKernel.Abstractions;
using SharedKernel.Primitives;

namespace Identity.Domain.Entities;

/// The central domain entity of the Identity Bounded Context.
/// Represents a system user with authentication credentials, a fixed role,
/// and a role-specific profile.
///
/// ── Design decision — ASP.NET Core Identity integration 
/// AppUser inherits IdentityUser&lt;Guid&gt; instead of AggregateRoot. This is a
/// pragmatic trade-off: Identity provides UserManager, SignInManager, and
/// MapIdentityApi — all of which require the entity to be IdentityUser.
/// Attempting to maintain a separate domain entity alongside IdentityUser would
/// require two-way mapping and is not worth the complexity for this scope.
public class AppUser : IdentityUser<Guid>
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// Pending domain events to be published by the Application Layer after
    /// the UserManager operation completes successfully.
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void ClearDomainEvents() => _domainEvents.Clear();

    private void RaiseDomainEvent(IDomainEvent domainEvent) =>
        _domainEvents.Add(domainEvent);

    public string FullName { get; private set; } = string.Empty;
    
    /// Must always be synced with IdentityUser.LockoutEnd by the Application Layer:
    ///   Deactivate → UserManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue)
    ///   Activate   → UserManager.SetLockoutEndDateAsync(user, null)
    public bool IsActive { get; private set; }

    /// The user's role. Assigned at creation and immutable thereafter.
    public UserRole Role { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public LecturerProfile? LecturerProfile { get; private set; }
    public StudentProfile? StudentProfile { get; private set; }


    private AppUser() { }
    
    public static Result<AppUser> Create(
        Guid id,
        string email,
        string fullName,
        UserRole role)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Result.Failure<AppUser>(UserErrors.EmailIsRequired);
        
        if (string.IsNullOrWhiteSpace(fullName))
            return Result.Failure<AppUser>(UserErrors.FullNameIsRequired);

        var user = new AppUser
        {
            Id = id,
            UserName = email.Trim().ToLowerInvariant(),
            Email = email.Trim().ToLowerInvariant(),
            FullName = fullName.Trim(),
            IsActive = true,
            Role = role,
            CreatedAt = DateTime.UtcNow,
            LockoutEnabled = true
        };

        user.AttachProfile();

        user.RaiseDomainEvent(UserCreatedEvent.Create(
            userId: user.Id,
            fullName: user.FullName,
            email: user.Email,
            role: user.Role.ToString()));

        return Result.Success(user);
    }

    // ── Behaviour methods
    
    public Result UpdateInfo(string fullName, string email, string? profileField = null)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Result.Failure(UserErrors.EmailIsRequired);
        
        if (string.IsNullOrWhiteSpace(fullName))
            return Result.Failure(UserErrors.FullNameIsRequired);


        FullName = fullName.Trim();
        Email = email.Trim().ToLowerInvariant();
        UserName = Email;

        UpdateProfileField(profileField);

        RaiseDomainEvent(UserUpdatedEvent.Create(Id, FullName, Email, Role.ToString()));

        return Result.Success();
    }

    /// Activates an inactive account.
    public Result Activate()
    {
        if (IsActive)
            return Result.Failure(UserErrors.AlreadyActive);

        IsActive = true;

        RaiseDomainEvent(UserActivatedEvent.Create(Id));

        return Result.Success();
    }
    
    /// Deactivates an active account.
    public Result Deactivate()
    {
        if (!IsActive)
            return Result.Failure(UserErrors.AlreadyInactive);

        IsActive = false;

        RaiseDomainEvent(UserDeactivatedEvent.Create(Id));

        return Result.Success();
    }

    // ── Private helpers
    private void AttachProfile()
    {
        LecturerProfile = Role == UserRole.Lecturer
            ? LecturerProfile.Create(Id)
            : null;

        StudentProfile = Role == UserRole.Student
            ? StudentProfile.Create(Id)
            : null;
    }
    
    /// Routes the optional profile field to the correct profile type
    /// based on the user's immutable role. Safe to call for any role.
    private void UpdateProfileField(string? value)
    {
        LecturerProfile?.UpdateDepartment(value);
        StudentProfile?.UpdateMajor(value);
    }
}