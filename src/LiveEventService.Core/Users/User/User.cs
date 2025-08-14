using LiveEventService.Core.Registrations.EventRegistration;
using LiveEventService.Core.Common;

namespace LiveEventService.Core.Users.User;

public class User : Entity
{
    public string IdentityId { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string PhoneNumber { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    public virtual ICollection<EventRegistration> EventRegistrations { get; private set; } = new List<EventRegistration>();

    protected User() { } // For EF Core

    public User(string identityId, string email, string firstName, string lastName, string phoneNumber)
    {
        IdentityId = identityId;
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        PhoneNumber = phoneNumber;
    }

    public void UpdateDetails(string email, string firstName, string lastName, string phoneNumber)
    {
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        PhoneNumber = phoneNumber;
    }

    public void UpdateProfile(string firstName, string lastName, string phoneNumber)
    {
        FirstName = firstName;
        LastName = lastName;
        PhoneNumber = phoneNumber;
    }

    public void AddRegistration(EventRegistration registration)
    {
        EventRegistrations.Add(registration);
    }

    public void RemoveRegistration(EventRegistration registration)
    {
        EventRegistrations.Remove(registration);
    }

    public void DeactivateAndAnonymize(string anonymizedEmail)
    {
        IsActive = false;
        Email = anonymizedEmail;
        FirstName = string.Empty;
        LastName = string.Empty;
        PhoneNumber = string.Empty;
    }
}
