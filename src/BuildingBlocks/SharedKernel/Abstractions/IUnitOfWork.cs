namespace SharedKernel.Abstractions;

/// Represents a unit of work that wraps a database transaction boundary.
/// Each module's DbContext implements this interface to expose SaveChanges
/// in a way that Application Layer handlers can call without depending on EF Core directly.
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}