using LiveEventService.Core.Common;
using UserEntity = LiveEventService.Core.Users.User.User;

namespace LiveEventService.Application.Features.Users.Queries.ListUsers;

/// <summary>
/// Specification to filter users by search term and active status, ordered by last name.
/// </summary>
public class ListUsersSpecification : BaseSpecification<UserEntity>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListUsersSpecification"/> class.
    /// Creates a specification that filters by search term across name/email and active flag.
    /// </summary>
    /// <param name="searchTerm">Optional search term to filter by first name, last name, or email.</param>
    /// <param name="isActive">Optional flag to filter by active status.</param>
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
