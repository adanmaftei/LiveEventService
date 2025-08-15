using LiveEventService.Core.Common;
using UserEntity = LiveEventService.Core.Users.User.User;

namespace LiveEventService.Application.Features.Users.Queries.GetUsersByIdentityIds;

/// <summary>
/// Specification to fetch users whose identity IDs are in the provided collection.
/// </summary>
public class GetUsersByIdentityIdsSpecification : BaseSpecification<UserEntity>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetUsersByIdentityIdsSpecification"/> class.
    /// Initializes the specification using the provided identity ID set.
    /// </summary>
    /// <param name="identityIds">Collection of identity IDs to filter users by.</param>
    public GetUsersByIdentityIdsSpecification(IEnumerable<string> identityIds)
    {
        Criteria = u => identityIds.Contains(u.IdentityId);
        ApplyOrderBy(u => u.LastName);
    }
}
