using Bogus;
using LiveEventService.Application.Features.Events.Event;
using LiveEventService.Application.Features.Events.Event.Create;
using LiveEventService.Application.Features.Events.EventRegistration.Register;
using LiveEventService.Application.Features.Users.User;
using LiveEventService.Application.Features.Users.User.Create;
using LiveEventService.Core.Events;
using LiveEventService.Core.Registrations.EventRegistration;
using LiveEventService.Core.Users.User;

namespace LiveEventService.IntegrationTests.Infrastructure;

public static class TestDataBuilder
{
    private static readonly Faker _faker = new();

    public static User CreateUser(string? identityId = null, string? email = null, string? firstName = null, string? lastName = null, string? phoneNumber = null)
    {
        var user = new User(
            identityId ?? Guid.NewGuid().ToString(),
            email ?? _faker.Internet.Email(),
            firstName ?? _faker.Name.FirstName(),
            lastName ?? _faker.Name.LastName(),
            phoneNumber ?? _faker.Phone.PhoneNumber("###-###-####") // Ensure phone number fits in 20 char limit
        );

        return user;
    }

    public static Event CreateEvent(
        string? name = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int? capacity = null,
        bool isPublished = true,
        string? description = null,
        string? location = null,
        string? timeZone = null,
        string? organizerId = null)
    {
        var start = startDate ?? DateTime.SpecifyKind(_faker.Date.Future(1), DateTimeKind.Utc);
        var eventObj = new Event(
            name ?? _faker.Lorem.Sentence(3, 5),
            description ?? _faker.Lorem.Paragraph(3),
            start,
            endDate ?? start.AddHours(_faker.Random.Int(1, 8)),
            capacity ?? _faker.Random.Int(10, 500),
            timeZone ?? "UTC",
            location ?? _faker.Address.FullAddress(),
            organizerId ?? "admin-user"
        );

        if (isPublished)
        {
            eventObj.Publish();
        }

        return eventObj;
    }

    public static EventRegistration CreateEventRegistration(Event eventObj, User user, string? notes = null)
    {
        var registration = new EventRegistration(eventObj, user, notes);
        return registration;
    }

    public static List<Event> CreateEventsList(int count = 5, bool mixPublishedStatus = true)
    {
        var events = new List<Event>();
        for (int i = 0; i < count; i++)
        {
            var isPublished = mixPublishedStatus ? _faker.Random.Bool() : true;
            events.Add(CreateEvent(isPublished: isPublished));
        }
        return events;
    }

    public static List<User> CreateUsersList(int count = 10)
    {
        var users = new List<User>();
        for (int i = 0; i < count; i++)
        {
            users.Add(CreateUser());
        }
        return users;
    }

    public static (Event Event, List<User> Users, List<EventRegistration> Registrations) CreateEventWithRegistrations(
        int registrationCount = 5,
        int capacity = 10,
        bool includeWaitlist = false)
    {
        var eventObj = CreateEvent(capacity: capacity);
        var users = CreateUsersList(registrationCount);
        var registrations = new List<EventRegistration>();

        for (int i = 0; i < registrationCount; i++)
        {
            registrations.Add(CreateEventRegistration(eventObj, users[i]));
        }

        return (eventObj, users, registrations);
    }

    public static class Commands
    {
        public static CreateUserCommand CreateUserCommand(string? identityId = null, string? email = null, string? firstName = null, string? lastName = null, DateTime? dateOfBirth = null)
        {
            return new CreateUserCommand
            {
                User = new CreateUserDto
                {
                    IdentityId = identityId ?? Guid.NewGuid().ToString(),
                    Email = email ?? _faker.Internet.Email(),
                    FirstName = firstName ?? _faker.Name.FirstName(),
                    LastName = lastName ?? _faker.Name.LastName(),
                    PhoneNumber = _faker.Phone.PhoneNumber("##########") ?? "5551234567"
                }
            };
        }

        public static CreateEventCommand CreateEventCommand(
            string? name = null,
            string? description = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? location = null,
            int? capacity = null)
        {
            var start = startDate ?? DateTime.SpecifyKind(_faker.Date.Future(1), DateTimeKind.Utc);
            return new CreateEventCommand
            {
                Event = new CreateEventDto
                {
                    Title = name ?? _faker.Lorem.Sentence(3, 5),
                    Description = description ?? _faker.Lorem.Paragraph(3),
                    StartDateTime = start,
                    EndDateTime = endDate ?? start.AddHours(_faker.Random.Int(1, 8)),
                    Location = location ?? _faker.Address.FullAddress(),
                    Capacity = capacity ?? _faker.Random.Int(10, 500),
                    TimeZone = "UTC"
                },
                OrganizerId = "admin-user" // Default organizer for tests
            };
        }

        public static CreateEventCommand CreateEventCommandWithInvalidData(
            string? name = null,
            string? description = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? location = null,
            int? capacity = null)
        {
            var start = startDate ?? DateTime.SpecifyKind(_faker.Date.Future(1), DateTimeKind.Utc);
            var end = endDate ?? start.AddHours(_faker.Random.Int(1, 8));
            return new CreateEventCommand
            {
                Event = new CreateEventDto
                {
                    Title = name!, // Don't use fallback for validation tests - suppress warning for test purposes
                    Description = description ?? _faker.Lorem.Paragraph(3),
                    StartDateTime = start,
                    EndDateTime = end,
                    Location = location ?? _faker.Address.FullAddress(),
                    Capacity = capacity ?? _faker.Random.Int(10, 500),
                    TimeZone = "UTC"
                },
                OrganizerId = "admin-user" // Default organizer for tests
            };
        }

        public static RegisterForEventCommand RegisterForEventCommand(Guid eventId, string userId)
        {
            return new RegisterForEventCommand
            {
                EventId = eventId,
                UserId = userId
            };
        }
    }

    public static class GraphQL
    {
        public static string CreateUserMutation(string? email = null, string? firstName = null, string? lastName = null, string? identityId = null)
        {
            var userEmail = email ?? _faker.Internet.Email();
            var userFirstName = firstName ?? _faker.Name.FirstName();
            var userLastName = lastName ?? _faker.Name.LastName();
            var userIdentityId = identityId ?? Guid.NewGuid().ToString();
            var phoneNumber = _faker.Phone.PhoneNumber("##########");

            return $@"
                mutation {{
                    createUser(input: {{
                        identityId: ""{userIdentityId}"",
                        email: ""{userEmail}"",
                        firstName: ""{userFirstName}"",
                        lastName: ""{userLastName}"",
                        phoneNumber: ""{phoneNumber}""
                    }}) {{
                        id
                        identityId
                        email
                        firstName
                        lastName
                        phoneNumber
                    }}
                }}";
        }

        public static string CreateEventMutation(
            string? title = null,
            string? description = null,
            DateTime? startDateTime = null,
            DateTime? endDateTime = null,
            string? location = null,
            int? capacity = null,
            string? timeZone = null)
        {
            var start = startDateTime ?? DateTime.UtcNow.AddDays(30); // Ensure future date
            var end = endDateTime ?? start.AddHours(_faker.Random.Int(1, 8));
            var tz = timeZone ?? "UTC";

            return $@"
                mutation {{
                    createEvent(input: {{
                        title: ""{title ?? _faker.Lorem.Sentence(3, 5)}"",
                        description: ""{description ?? _faker.Lorem.Paragraph(3)}"",
                        startDateTime: ""{start:yyyy-MM-ddTHH:mm:ssZ}"",
                        endDateTime: ""{end:yyyy-MM-ddTHH:mm:ssZ}"",
                        timeZone: ""{tz}"",
                        location: ""{location ?? _faker.Address.FullAddress()}"",
                        capacity: {capacity ?? _faker.Random.Int(10, 500)}
                    }}) {{
                        id
                        title
                        description
                        startDateTime
                        endDateTime
                        timeZone
                        location
                        address
                        capacity
                        isPublished
                        organizerId
                        organizerName
                    }}
                }}";
        }

        public static string GetEventsQuery(bool? isPublished = null, int? pageSize = null)
        {
            var filter = isPublished.HasValue ? $"isPublished: {isPublished.Value.ToString().ToLower()}" : "";
            var pagination = pageSize.HasValue ? $"pageSize: {pageSize}" : "";
            var args = string.Join(", ", new[] { filter, pagination }.Where(s => !string.IsNullOrEmpty(s)));

            return $@"
                query {{
                    events({args}) {{
                        items {{
                            id
                            title
                            description
                            startDateTime
                            endDateTime
                            location
                            capacity
                            isPublished
                        }}
                        totalCount
                        pageNumber
                        pageSize
                        totalPages
                    }}
                }}";
        }

        public static string RegisterForEventMutation(Guid eventId, string userId)
        {
            return $@"
                mutation {{
                    registerForEvent(input: {{
                        eventId: ""{eventId}""
                    }}) {{
                        id
                        status
                        positionInQueue
                    }}
                }}";
        }
    }
}
