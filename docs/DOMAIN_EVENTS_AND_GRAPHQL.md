# Domain Events and GraphQL Subscriptions

## Overview

The Live Event Service uses domain events to decouple business logic and enable real-time notifications. Domain events are raised in the domain model (Core layer) and dispatched via MediatR after database changes, allowing for clean separation of concerns and enabling features like real-time notifications.

## Domain Events

The system uses domain events to decouple business logic and enable real-time notifications. Domain events are raised in the domain model (Core layer) and dispatched via MediatR after database changes. Handlers can perform side effects, such as sending notifications.

### Events

| Event                                 | Raised When                                      | Handled By (API)                        | GraphQL Action |
|----------------------------------------|--------------------------------------------------|------------------------------------------|---------------|
| EventRegistrationCreatedDomainEvent    | A user registers for an event (confirmed/waitlist)| Publishes EventRegistrationNotification  | created       |
| EventRegistrationPromotedDomainEvent   | A waitlisted registration is promoted to confirmed| Publishes EventRegistrationNotification  | promoted      |
| EventRegistrationCancelledDomainEvent  | A registration is cancelled                      | Publishes EventRegistrationNotification  | cancelled     |

## How Events Are Raised

- In the domain model (e.g., `EventRegistration`), after a business action (register, promote, cancel), the entity calls `AddDomainEvent(new EventRegistration...DomainEvent(this))`.
- After `SaveChangesAsync`, the DbContext collects all domain events and dispatches them via MediatR.
- Events are processed in the same transaction as the main operation, ensuring consistency.

## How Events Are Handled

- In the Application project, MediatR `INotificationHandler`s listen for these events and use an `IEventRegistrationNotifier` abstraction.
- The API project implements `IEventRegistrationNotifier` (`EventRegistrationNotifier`) to publish notifications to the appropriate HotChocolate topic (e.g., `eventRegistration_{eventId}`).
- The notification includes the event ID, user info, action, and timestamp.

## GraphQL Subscriptions

- Clients can subscribe to `eventRegistration_{eventId}` topics to receive real-time updates about registrations for a specific event.
- Actions include: `created`, `promoted`, `cancelled`.

### Subscription Schema

```graphql
type Subscription {
  onEventRegistration(eventId: ID!): EventRegistrationNotification!
}

type EventRegistrationNotification {
  eventId: ID!
  eventTitle: String!
  userId: String!
  userName: String!
  action: String!
  timestamp: DateTime!
}
```

### Subscription Example

```graphql
subscription {
  onEventRegistration(eventId: "6ba7b810-9dad-11d1-80b4-00c04fd430c8") {
    eventId
    eventTitle
    userId
    userName
    action
    timestamp
  }
}
```

## Detailed Waitlist Scenarios

### Scenario 1: User Registers for Full Event

**Flow:**
1. User attempts to register for event with capacity 2 (already has 2 confirmed registrations)
2. `EventRegistrationCreatedDomainEvent` is raised with status `Waitlisted`
3. GraphQL notification sent with action `created`

**Domain Event:**
```csharp
public EventRegistration(Event @event, User user, string? notes = null)
{
    // ... initialization logic
    
    if (@event.IsFull())
    {
        Status = RegistrationStatus.Waitlisted;
        AddDomainEvent(new EventRegistrationCreatedDomainEvent(this));
    }
}
```

**GraphQL Notification:**
```json
{
  "eventId": "6ba7b810-9dad-11d1-80b4-00c04fd430c8",
  "eventTitle": "Tech Conference 2024",
  "userId": "user123",
  "userName": "John Doe",
  "action": "created",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

### Scenario 2: Automatic Promotion from Waitlist

**Flow:**
1. A confirmed registration is cancelled
2. First waitlisted user (position 1) is automatically promoted
3. `EventRegistrationPromotedDomainEvent` is raised
4. GraphQL notification sent with action `promoted`
5. Remaining waitlisted users have positions updated

**Domain Event:**
```csharp
public void Confirm()
{
    if (Status == RegistrationStatus.Waitlisted)
    {
        Status = RegistrationStatus.Confirmed;
        PositionInQueue = null;
        AddDomainEvent(new EventRegistrationPromotedDomainEvent(this));
    }
}
```

**GraphQL Notification:**
```json
{
  "eventId": "6ba7b810-9dad-11d1-80b4-00c04fd430c8",
  "eventTitle": "Tech Conference 2024", 
  "userId": "user456",
  "userName": "Jane Smith",
  "action": "promoted",
  "timestamp": "2024-01-15T11:00:00Z"
}
```

### Scenario 3: Registration Cancellation

**Flow:**
1. User or admin cancels a registration
2. `EventRegistrationCancelledDomainEvent` is raised
3. If cancelled registration was confirmed, automatic promotion logic triggers
4. GraphQL notification sent with action `cancelled`

**Domain Event:**
```csharp
public void Cancel()
{
    Status = RegistrationStatus.Cancelled;
    AddDomainEvent(new EventRegistrationCancelledDomainEvent(this));
}
```

**GraphQL Notification:**
```json
{
  "eventId": "6ba7b810-9dad-11d1-80b4-00c04fd430c8",
  "eventTitle": "Tech Conference 2024",
  "userId": "user789", 
  "userName": "Bob Wilson",
  "action": "cancelled",
  "timestamp": "2024-01-15T12:00:00Z"
}
```

## Event Handler Implementation

### Example Handler

```csharp
public class EventRegistrationCreatedHandler : INotificationHandler<EventRegistrationCreatedDomainEvent>
{
    private readonly IEventRegistrationNotifier _notifier;
    
    public async Task Handle(EventRegistrationCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        // Determine action based on registration status
        var action = notification.Registration.Status == RegistrationStatus.Confirmed 
            ? "created" 
            : "created"; // Same action for both confirmed and waitlisted
            
        await _notifier.NotifyAsync(notification.Registration, action, cancellationToken);
    }
}
```

## Real-time Frontend Integration

### JavaScript/TypeScript Example

```typescript
// Subscribe to event registration updates
const subscription = `
  subscription EventUpdates($eventId: ID!) {
    onEventRegistration(eventId: $eventId) {
      eventId
      eventTitle
      userId
      userName
      action
      timestamp
    }
  }
`;

// Handle real-time updates
client.subscribe({
  query: subscription,
  variables: { eventId: "event-123" }
}).subscribe({
  next: (data) => {
    const notification = data.data.onEventRegistration;
    
    switch (notification.action) {
      case 'created':
        showNotification(`${notification.userName} registered for ${notification.eventTitle}`);
        updateRegistrationList();
        break;
        
      case 'promoted':
        showSuccessNotification(`${notification.userName} was promoted from waitlist!`);
        updateWaitlistPositions();
        break;
        
      case 'cancelled':
        showNotification(`${notification.userName} cancelled their registration`);
        updateRegistrationList();
        break;
    }
  }
});
```

## Testing Domain Events

### Integration Test Example

```csharp
[Fact]
public async Task RegisterForFullEvent_ShouldRaiseDomainEvent()
{
    // Arrange: Create event with capacity 1
    var eventId = await CreateEventWithCapacity(1);
    await RegisterUserForEvent(eventId, "user1@test.com");
    
    // Act: Register second user (should be waitlisted)
    var result = await RegisterUserForEvent(eventId, "user2@test.com");
    
    // Assert: Domain event should be raised
    // This is tested implicitly through GraphQL notifications
    result.Status.Should().Be("Waitlisted");
    result.PositionInQueue.Should().Be(1);
}
```

## Best Practices

### For Domain Events

1. **Keep events simple** - Events should contain just the data needed for notifications
2. **Make events immutable** - Events should not be modified after creation
3. **Handle events in separate handlers** - Don't mix business logic with event handling
4. **Events should be side-effect free** - Events describe what happened, not what should happen

### For GraphQL Subscriptions

1. **Use specific topics** - `eventRegistration_{eventId}` allows targeted subscriptions
2. **Include context in notifications** - Event title, user name, etc. for better UX
3. **Handle connection failures** - Implement reconnection logic in client applications
4. **Filter notifications client-side** - If needed for performance with many events

## Troubleshooting

### Common Issues

1. **Events not firing** - Check that domain entities inherit from `Entity` base class
2. **Subscriptions not receiving data** - Verify GraphQL endpoint configuration
3. **Performance issues** - Consider batching events for high-volume scenarios
4. **Missing notifications** - Ensure event handlers are registered in DI container

The domain event system provides reliable, real-time communication between the backend and frontend, enabling rich user experiences with waitlist updates and registration changes. 