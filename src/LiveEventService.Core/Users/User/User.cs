using LiveEventService.Core.Registrations.EventRegistration;
using LiveEventService.Core.Common;

namespace LiveEventService.Core.Users.User;

/// <summary>
/// Aggregate representing an application user and their registrations.
/// </summary>
public class User : Entity
{
    /// <summary>Gets external identity provider identifier.</summary>
    public string IdentityId { get; private set; } = string.Empty;

    /// <summary>Gets user email address.</summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>Gets given name.</summary>
    public string FirstName { get; private set; } = string.Empty;

    /// <summary>Gets surname.</summary>
    public string LastName { get; private set; } = string.Empty;

    /// <summary>Gets contact phone number.</summary>
    public string PhoneNumber { get; private set; } = string.Empty;

    /// <summary>Gets a value indicating whether whether the user account is active.</summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>Gets registrations associated with this user.</summary>
    public virtual ICollection<EventRegistration> EventRegistrations { get; private set; } = new List<EventRegistration>();

    protected User() { } // For EF Core

    /// <summary>
    /// Initializes a new instance of the <see cref="User"/> class.
    /// Creates a new user aggregate.
    /// </summary>
    /// <param name="identityId">The external identity provider identifier for the user.</param>
    /// <param name="email">The email address of the user.</param>
    /// <param name="firstName">The given name of the user.</param>
    /// <param name="lastName">The surname of the user.</param>
    /// <param name="phoneNumber">The contact phone number of the user.</param>
    public User(string identityId, string email, string firstName, string lastName, string phoneNumber)
    {
        IdentityId = identityId;
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        PhoneNumber = phoneNumber;
    }

    /// <summary>
    /// Updates the user's contact details.
    /// </summary>
    /// <param name="email">The email address of the user.</param>
    /// <param name="firstName">The given name of the user.</param>
    /// <param name="lastName">The surname of the user.</param>
    /// <param name="phoneNumber">The contact phone number of the user.</param>
    public void UpdateDetails(string email, string firstName, string lastName, string phoneNumber)
    {
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        PhoneNumber = phoneNumber;
    }

    /// <summary>
    /// Updates profile information excluding email.
    /// </summary>
    /// <param name="firstName">The given name of the user.</param>
    /// <param name="lastName">The surname of the user.</param>
    /// <param name="phoneNumber">The contact phone number of the user.</param>
    public void UpdateProfile(string firstName, string lastName, string phoneNumber)
    {
        FirstName = firstName;
        LastName = lastName;
        PhoneNumber = phoneNumber;
    }

    /// <summary>
    /// Adds a registration association.
    /// </summary>
    /// <param name="registration">The registration to be added to the user's associated registrations.</param>
    public void AddRegistration(EventRegistration registration)
    {
        EventRegistrations.Add(registration);
    }

    /// <summary>
    /// Removes a registration association.
    /// </summary>
    /// <param name="registration">The registration to be removed from the user's associated registrations.</param>
    public void RemoveRegistration(EventRegistration registration)
    {
        EventRegistrations.Remove(registration);
    }

    /// <summary>
    /// Deactivates the user account and anonymizes personally identifiable information.
    /// </summary>
    /// <param name="anonymizedEmail">The anonymized email address to replace the user's current email.</param>
    public void DeactivateAndAnonymize(string anonymizedEmail)
    {
        IsActive = false;
        Email = anonymizedEmail;
        FirstName = string.Empty;
        LastName = string.Empty;
        PhoneNumber = string.Empty;
    }
}
