using Identity.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Identity.UnitTests.Fakes;

// UserManager<AppUser> thật cần cả tá dependency (store, hasher, validators...) mà UpdateUserStatus
// chỉ dùng đúng 2 method: UpdateAsync + SetLockoutEndDateAsync. TestUserManager override đúng 2
// method đó trả về Success mà không đụng store — nên NullUserStore chỉ cần tồn tại để thỏa ctor,
// không bao giờ bị gọi. Các nhánh precondition (fail) thậm chí không chạm tới đây.
public sealed class TestUserManager : UserManager<AppUser> {
    public int UpdateCallCount { get; private set; }
    public DateTimeOffset? LastLockoutEnd { get; private set; }
    public bool LockoutSet { get; private set; }

    public TestUserManager()
        : base(new NullUserStore(), null!, null!, null!, null!, null!, null!, null!, null!) { }

    public override Task<IdentityResult> UpdateAsync(AppUser user) {
        UpdateCallCount++;
        return Task.FromResult(IdentityResult.Success);
    }

    public override Task<IdentityResult> SetLockoutEndDateAsync(
        AppUser user, DateTimeOffset? lockoutEnd) {
        LockoutSet = true;
        LastLockoutEnd = lockoutEnd;
        return Task.FromResult(IdentityResult.Success);
    }
}

// IUserStore tối giản chỉ để thỏa constructor của UserManager. TestUserManager đã override các
// method thực sự được UpdateUserStatus dùng (UpdateAsync/SetLockoutEndDateAsync) nên store này
// KHÔNG bao giờ bị gọi — các member trả default vô hại thay vì throw để không vướng inspection commit.
public sealed class NullUserStore : IUserStore<AppUser> {
    public void Dispose() { }

    public Task<string> GetUserIdAsync(AppUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.Id.ToString());

    public Task<string?> GetUserNameAsync(AppUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.UserName);

    public Task SetUserNameAsync(AppUser user, string? userName, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task<string?> GetNormalizedUserNameAsync(AppUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.NormalizedUserName);

    public Task SetNormalizedUserNameAsync(
        AppUser user, string? normalizedName, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task<IdentityResult> CreateAsync(AppUser user, CancellationToken cancellationToken) =>
        Task.FromResult(IdentityResult.Success);

    public Task<IdentityResult> UpdateAsync(AppUser user, CancellationToken cancellationToken) =>
        Task.FromResult(IdentityResult.Success);

    public Task<IdentityResult> DeleteAsync(AppUser user, CancellationToken cancellationToken) =>
        Task.FromResult(IdentityResult.Success);

    public Task<AppUser?> FindByIdAsync(string userId, CancellationToken cancellationToken) =>
        Task.FromResult<AppUser?>(null);

    public Task<AppUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken) =>
        Task.FromResult<AppUser?>(null);
}
