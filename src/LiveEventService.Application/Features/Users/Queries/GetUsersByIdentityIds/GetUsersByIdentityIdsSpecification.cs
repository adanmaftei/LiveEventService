using LiveEventService.Core.Common;
using UserEntity = LiveEventService.Core.Users.User.User;

namespace LiveEventService.Application.Features.Users.Queries.GetUsersByIdentityIds;

public class GetUsersByIdentityIdsSpecification : BaseSpecification<UserEntity>
{
    public GetUsersByIdentityIdsSpecification(IEnumerable<string> identityIds)
    {
        Criteria = u => identityIds.Contains(u.IdentityId);
        ApplyOrderBy(u => u.LastName);
    }
}
