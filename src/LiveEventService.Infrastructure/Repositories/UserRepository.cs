using UserEntity = LiveEventService.Core.Users.User.User;
using IUserRepository = LiveEventService.Core.Users.User.IUserRepository;
using LiveEventService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LiveEventService.Infrastructure.Users;

/// <summary>
/// Repository implementation for <see cref="UserEntity"/> providing lookup helpers
/// by identity id and email, along with simple text search utilities.
/// </summary>
public class UserRepository : RepositoryBase<UserEntity>, IUserRepository
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserRepository"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    public UserRepository(LiveEventDbContext dbContext) : base(dbContext)
    {
    }

    /// <summary>
    /// Gets a user by their external identity provider ID.
    /// </summary>
    /// <param name="identityId">External identity provider user id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>User if found; otherwise null.</returns>
    public Task<UserEntity?> GetByIdentityIdAsync(string identityId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.IdentityId == identityId, cancellationToken);
    }

    /// <summary>
    /// Gets a user by their email address.
    /// </summary>
    /// <param name="email">User email address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>User if found; otherwise null.</returns>
    public Task<UserEntity?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    /// <summary>
    /// Searches for users by email, first name, or last name.
    /// </summary>
    /// <param name="searchTerm">Term to search in email, first name, or last name.</param>
    /// <param name="skip">Number of items to skip.</param>
    /// <param name="take">Number of items to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Matching users as a read-only list.</returns>
    public async Task<IReadOnlyList<UserEntity>> SearchUsersAsync(string searchTerm, int skip, int take, CancellationToken cancellationToken = default)
    {
        var normalizedSearchTerm = searchTerm.ToLower();

        return await _dbSet
            .AsNoTracking()
            .Where(u => u.Email.ToLower().Contains(normalizedSearchTerm) ||
                       u.FirstName.ToLower().Contains(normalizedSearchTerm) ||
                       u.LastName.ToLower().Contains(normalizedSearchTerm))
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Checks if an email address is unique among all users.
    /// </summary>
    /// <param name="email">Email to validate for uniqueness.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if no user exists with the specified email.</returns>
    public async Task<bool> IsEmailUniqueAsync(string email, CancellationToken cancellationToken = default)
    {
        return !await _dbSet.AnyAsync(u => u.Email == email, cancellationToken);
    }
}
