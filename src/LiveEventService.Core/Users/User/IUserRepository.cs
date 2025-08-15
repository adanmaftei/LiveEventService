using LiveEventService.Core.Common;

namespace LiveEventService.Core.Users.User;

/// <summary>
/// Repository abstraction for users with identity-based queries.
/// </summary>
public interface IUserRepository : IRepository<User>
{
    /// <summary>Gets a user by external identity identifier.</summary>
    /// <param name="identityId">The external identity identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<User?> GetByIdentityIdAsync(string identityId, CancellationToken cancellationToken = default);

    /// <summary>Gets a user by email address.</summary>
    /// <param name="email">The email address to search for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
}
