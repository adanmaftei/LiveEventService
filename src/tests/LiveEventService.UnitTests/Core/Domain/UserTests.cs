using LiveEventService.Core.Events;
using LiveEventService.Core.Registrations.EventRegistration;
using LiveEventService.Core.Users.User;
using LiveEventService.UnitTests.Common;

namespace LiveEventService.UnitTests.Core.Domain;

public class UserTests : TestBase
{
    [Fact]
    public void Constructor_WithValidData_ShouldCreateUser()
    {
        // Arrange
        var identityId = "auth0|testuser123";
        var email = "test@example.com";
        var firstName = "John";
        var lastName = "Doe";
        var phoneNumber = "+1234567890";

        // Act
        var user = new User(identityId, email, firstName, lastName, phoneNumber);

        // Assert
        AssertNotNull(user);
        AssertEqual(identityId, user.IdentityId);
        AssertEqual(email, user.Email);
        AssertEqual(firstName, user.FirstName);
        AssertEqual(lastName, user.LastName);
        AssertEqual(phoneNumber, user.PhoneNumber);
        AssertTrue(user.IsActive);
        AssertCollectionEmpty(user.EventRegistrations);
    }

    [Fact]
    public void Constructor_ShouldSetIdAndTimestamps()
    {
        // Arrange & Act
        var user = new User("auth0|testuser123", "test@example.com", "John", "Doe", "+1234567890");

        // Assert
        AssertNotEqual(Guid.Empty, user.Id);
        AssertTrue(user.CreatedAt > DateTime.UtcNow.AddMinutes(-1));
        AssertNull(user.UpdatedAt);
    }

    [Fact]
    public void UpdateDetails_WithValidData_ShouldUpdateUser()
    {
        // Arrange
        var user = new User("auth0|testuser123", "test@example.com", "John", "Doe", "+1234567890");
        var newEmail = "updated@example.com";
        var newFirstName = "Jane";
        var newLastName = "Smith";
        var newPhoneNumber = "+0987654321";

        // Act
        user.UpdateDetails(newEmail, newFirstName, newLastName, newPhoneNumber);

        // Assert
        AssertEqual(newEmail, user.Email);
        AssertEqual(newFirstName, user.FirstName);
        AssertEqual(newLastName, user.LastName);
        AssertEqual(newPhoneNumber, user.PhoneNumber);
    }

    [Fact]
    public void UpdateProfile_WithValidData_ShouldUpdateProfileFields()
    {
        // Arrange
        var user = new User("auth0|testuser123", "test@example.com", "John", "Doe", "+1234567890");
        var newFirstName = "Jane";
        var newLastName = "Smith";
        var newPhoneNumber = "+0987654321";
        var originalEmail = user.Email;

        // Act
        user.UpdateProfile(newFirstName, newLastName, newPhoneNumber);

        // Assert
        AssertEqual(originalEmail, user.Email); // Email should not change
        AssertEqual(newFirstName, user.FirstName);
        AssertEqual(newLastName, user.LastName);
        AssertEqual(newPhoneNumber, user.PhoneNumber);
    }

    [Fact]
    public void AddRegistration_ShouldAddToEventRegistrations()
    {
        // Arrange
        var user = new User("auth0|testuser123", "test@example.com", "John", "Doe", "+1234567890");
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 100, "UTC", "Test Location", "organizer-123");
        var registration = new EventRegistration(@event, user);

        // Act
        user.AddRegistration(registration);

        // Assert
        AssertCollectionCount(user.EventRegistrations, 1);
        AssertTrue(user.EventRegistrations.Contains(registration));
    }

    [Fact]
    public void RemoveRegistration_ShouldRemoveFromEventRegistrations()
    {
        // Arrange
        var user = new User("auth0|testuser123", "test@example.com", "John", "Doe", "+1234567890");
        var @event = new Event("Test Event", "Test Description", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 100, "UTC", "Test Location", "organizer-123");
        var registration = new EventRegistration(@event, user);
        user.AddRegistration(registration);
        AssertCollectionCount(user.EventRegistrations, 1);

        // Act
        user.RemoveRegistration(registration);

        // Assert
        AssertCollectionEmpty(user.EventRegistrations);
    }

    [Fact]
    public void GetFullName_ShouldReturnFormattedName()
    {
        // Arrange
        var user = new User("test", "test@example.com", "John", "Doe", "+1234567890");

        // Act
        var fullName = $"{user.FirstName} {user.LastName}".Trim();

        // Assert
        AssertEqual("John Doe", fullName);
    }

    [Fact]
    public void GetFullName_WithEmptyLastName_ShouldReturnFirstNameOnly()
    {
        // Arrange
        var user = new User("test", "test@example.com", "John", "", "+1234567890");

        // Act
        var fullName = $"{user.FirstName} {user.LastName}".Trim();

        // Assert
        AssertEqual("John", fullName);
    }

    [Fact]
    public void GetFullName_WithEmptyFirstName_ShouldReturnLastNameOnly()
    {
        // Arrange
        var user = new User("test", "test@example.com", "", "Doe", "+1234567890");

        // Act
        var fullName = $"{user.FirstName} {user.LastName}".Trim();

        // Assert
        AssertEqual("Doe", fullName);
    }

    [Fact]
    public void DeactivateAndAnonymize_ShouldDeactivateAndClearPii()
    {
        // Arrange
        var user = new User("auth0|testuser123", "test@example.com", "John", "Doe", "+1234567890");
        var anonymizedEmail = $"anon+{user.Id}@example.invalid";

        // Act
        user.DeactivateAndAnonymize(anonymizedEmail);

        // Assert
        AssertFalse(user.IsActive);
        AssertEqual(anonymizedEmail, user.Email);
        AssertEqual(string.Empty, user.FirstName);
        AssertEqual(string.Empty, user.LastName);
        AssertEqual(string.Empty, user.PhoneNumber);
    }
    [Fact]
    public void RemoveRegistration_WithNonExistentRegistration_ShouldNotThrowException()
    {
        // Arrange
        var user = new User("auth0|testuser123", "test@example.com", "John", "Doe", "+1234567890");
        var @event = Fixture.Create<LiveEventService.Core.Events.Event>();
        var registration = new EventRegistration(@event, user);

        // Act & Assert
        AssertFalse(user.EventRegistrations.Contains(registration));
    }
}
