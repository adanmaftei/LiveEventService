using LiveEventService.Core.Common;
using UserEntity = LiveEventService.Core.Users.User.User;

namespace LiveEventService.Application.Features.Users.Queries.ListUsers;

public class ListUsersSpecification : BaseSpecification<UserEntity>
{
    public ListUsersSpecification(string? searchTerm, bool? isActive)
    {
        Criteria = u =>
            (string.IsNullOrWhiteSpace(searchTerm) ||
                u.FirstName.ToLower().Contains(searchTerm.ToLower()) ||
                u.LastName.ToLower().Contains(searchTerm.ToLower()) ||
                u.Email.ToLower().Contains(searchTerm.ToLower())) &&
            (!isActive.HasValue || u.IsActive == isActive.Value);
        ApplyOrderBy(u => u.LastName);
    }
}
