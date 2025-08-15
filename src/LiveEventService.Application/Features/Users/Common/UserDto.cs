namespace LiveEventService.Application.Features.Users.User;

/// <summary>
/// Represents a user profile exposed by the API.
/// </summary>
public class UserDto
{
    /// <summary>Gets or sets internal user identifier.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets user email address.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Gets or sets first name.</summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>Gets or sets last name.</summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>Gets or sets optional phone number.</summary>
    public string? PhoneNumber { get; set; }

    /// <summary>Gets or sets a value indicating whether whether the user is active.</summary>
    public bool IsActive { get; set; }

    /// <summary>Gets convenience property combining first and last name.</summary>
    public string FullName => $"{FirstName} {LastName}".Trim();

    /// <summary>Gets or sets external identity provider subject/identity.</summary>
    public string IdentityId { get; set; } = string.Empty;

    /// <summary>Gets or sets uTC timestamp when the record was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets uTC timestamp when the record was last updated.</summary>
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Request payload to create a new user.
/// </summary>
public class CreateUserDto
{
    /// <summary>Gets or sets external identity provider subject/identity.</summary>
    public string IdentityId { get; set; } = string.Empty;

    /// <summary>Gets or sets email address.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Gets or sets first name.</summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>Gets or sets last name.</summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>Gets or sets optional phone number.</summary>
    public string? PhoneNumber { get; set; }
}

/// <summary>
/// Request payload to update an existing user.
/// </summary>
public class UpdateUserDto
{
    /// <summary>Gets or sets internal user identifier.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets first name.</summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>Gets or sets last name.</summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>Gets or sets optional phone number.</summary>
    public string? PhoneNumber { get; set; }
}

/// <summary>
/// Paginated list of users with metadata.
/// </summary>
public class UserListDto
{
    /// <summary>Gets or sets page of user items.</summary>
    public IEnumerable<UserDto> Items { get; set; } = new List<UserDto>();

    /// <summary>Gets or sets total number of users matching the query.</summary>
    public int TotalCount { get; set; }

    /// <summary>Gets or sets current page number (1-based).</summary>
    public int PageNumber { get; set; }

    /// <summary>Gets or sets number of items per page.</summary>
    public int PageSize { get; set; }

    /// <summary>Gets total number of pages.</summary>
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
