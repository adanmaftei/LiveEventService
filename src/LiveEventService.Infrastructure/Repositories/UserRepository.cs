using UserEntity = LiveEventService.Core.Users.User.User;
using IUserRepository = LiveEventService.Core.Users.User.IUserRepository;
using LiveEventService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LiveEventService.Infrastructure.Users;

public class UserRepository : RepositoryBase<UserEntity>, IUserRepository
{
    public UserRepository(LiveEventDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<UserEntity?> GetByIdentityIdAsync(string identityId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users.FirstOrDefaultAsync(u => u.IdentityId == identityId, cancellationToken);
    }

    public async Task<UserEntity?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<IReadOnlyList<UserEntity>> SearchUsersAsync(string searchTerm, int skip, int take, CancellationToken cancellationToken = default)
    {
        var normalizedSearchTerm = searchTerm.ToLower();
        
        return await _dbSet
            .Where(u => u.Email.ToLower().Contains(normalizedSearchTerm) ||
                       u.FirstName.ToLower().Contains(normalizedSearchTerm) ||
                       u.LastName.ToLower().Contains(normalizedSearchTerm))
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsEmailUniqueAsync(string email, CancellationToken cancellationToken = default)
    {
        return !await _dbSet.AnyAsync(u => u.Email == email, cancellationToken);
    }
}
