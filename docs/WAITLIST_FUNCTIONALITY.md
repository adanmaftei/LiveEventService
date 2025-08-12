# Event Waitlist Functionality

## Overview

The Live Event Service provides comprehensive waitlist functionality that automatically manages event registrations when events reach capacity. The system handles waitlist queuing, position tracking, automatic promotion, and real-time notifications through domain events.

## ✅ Current Status: Fully Operational

- ✅ **Automatic Waitlist Management** - Users are automatically waitlisted when events are full
- ✅ **Position Tracking** - Waitlist positions start from 1 and are maintained accurately
- ✅ **Automatic Promotion** - When spots become available, waitlisted users are promoted automatically
- ✅ **Domain Event Driven** - All waitlist operations are handled through domain events for consistency
- ✅ **Real-time Notifications** - GraphQL subscriptions notify about waitlist changes
- ✅ **Role-based Access Control** - Different operations require different user roles

## Role-Based Operations

### Admin Role (`RoleNames.Admin`)

**Event Management:**
- ✅ Create events
- ✅ Update event details (including capacity changes)
- ✅ Delete events
- ✅ Publish/unpublish events
- ✅ View all event registrations
- ✅ View waitlist for any event

**Registration Management:**
- ✅ View all registrations for any event
- ✅ Cancel any registration (triggers automatic waitlist promotion)
- ✅ Manually confirm waitlisted registrations
- ✅ Promote users from waitlist to confirmed status

**Waitlist Management:**
- ✅ View waitlist positions and details
- ✅ Manually adjust waitlist positions
- ✅ Remove users from waitlist
- ✅ Monitor waitlist statistics

### Participant Role (`RoleNames.Participant`)

**Event Interaction:**
- ✅ View published events
- ✅ Register for events (automatic waitlist if full)
- ✅ View own registrations
- ✅ Cancel own registrations

**Waitlist Interaction:**
- ✅ View own waitlist position
- ✅ Receive notifications about position changes
- ✅ Receive notifications when promoted from waitlist

### Anonymous Users

**Event Discovery:**
- ✅ View published events
- ✅ View event details
- ❌ Cannot register (requires authentication)

## Business Rules

### Registration Flow

1. **Event has capacity**: User gets **Confirmed** status immediately
2. **Event is full**: User gets **Waitlisted** status with position starting from 1
3. **Position calculation**: Position 1 = first in line, Position 2 = second in line, etc.

### Automatic Promotion

When a confirmed registration is cancelled:
1. First waitlisted user (position 1) is automatically promoted to **Confirmed**
2. All remaining waitlisted users have their positions decremented by 1
3. Domain events are raised for both the promotion and position updates

### Waitlist Position Logic

```
Event Capacity: 2

Registration 1 → Confirmed (no position)
Registration 2 → Confirmed (no position)  
Registration 3 → Waitlisted, Position 1 (first in queue)
Registration 4 → Waitlisted, Position 2 (second in queue)
Registration 5 → Waitlisted, Position 3 (third in queue)

If Registration 1 cancels:
- Registration 3 → Promoted to Confirmed  
- Registration 4 → Position becomes 1
- Registration 5 → Position becomes 2
```

## API Endpoints

### Public Endpoints (No Authentication Required)

```http
GET /api/events                    # List published events
GET /api/events/{id}               # Get event details
```

### Participant Endpoints (Requires Authentication)

```http
POST /api/events/{eventId}/register    # Register for event (auto-waitlist if full)
GET /api/users/me                       # Get current user profile
PUT /api/users/{id}                     # Update own profile
```

### Admin Endpoints (Requires Admin Role)

```http
# Event Management
POST /api/events                        # Create event
PUT /api/events/{id}                    # Update event (including capacity)
DELETE /api/events/{id}                 # Delete event
POST /api/events/{id}/publish           # Publish event
POST /api/events/{id}/unpublish         # Unpublish event

# Registration Management
GET /api/events/{id}/registrations      # View all registrations
GET /api/events/{id}/waitlist           # View waitlist
POST /api/events/{id}/registrations/{registrationId}/confirm    # Manually confirm
POST /api/events/{id}/registrations/{registrationId}/cancel     # Cancel registration

# User Management
GET /api/users                          # List all users
GET /api/users/{id}                     # Get user details
POST /api/users                         # Create user
PUT /api/users/{id}                     # Update user
```

## Domain Events & Real-time Notifications

### Domain Events Raised

| Event | Trigger | Handler | Purpose |
|-------|---------|---------|---------|
| `EventRegistrationCreatedDomainEvent` | User registers (confirmed or waitlisted) | `EventRegistrationCreatedDomainEventHandler` | Send real-time notifications |
| `EventRegistrationPromotedDomainEvent` | Waitlisted user promoted to confirmed | `EventRegistrationPromotedDomainEventHandler` | Notify about promotions |
| `EventRegistrationCancelledDomainEvent` | Registration cancelled | `EventRegistrationCancelledDomainEventHandler` | Handle waitlist promotion + notifications |
| `RegistrationWaitlistedDomainEvent` | Registration added to waitlist | `RegistrationWaitlistedDomainEventHandler` | Calculate waitlist position |
| `WaitlistPositionChangedDomainEvent` | Waitlist position changes | `WaitlistPositionChangedDomainEventHandler` | Log position changes |
| `WaitlistRemovalDomainEvent` | Registration removed from waitlist | `WaitlistRemovalDomainEventHandler` | Update remaining positions |
| `EventCapacityIncreasedDomainEvent` | Event capacity increased | `EventCapacityIncreasedDomainEventHandler` | Promote waitlisted users |

### Event Flow Architecture

1. **Domain Event Creation**: Events are raised in the domain model using `AddDomainEvent()`
2. **Event Dispatch**: After `SaveChangesAsync()`, events are dispatched via MediatR
3. **Notification Adapters**: Domain events are wrapped in notification adapters
4. **Handler Processing**: Handlers process events asynchronously

### GraphQL Subscriptions

Subscribe to real-time waitlist updates:

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

**Actions include:**
- `created` - New registration (confirmed or waitlisted)
- `promoted` - Waitlisted user promoted to confirmed
- `cancelled` - Registration cancelled

## Technical Implementation

### Core Domain Model

```csharp
public class EventRegistration : Entity
{
    public RegistrationStatus Status { get; private set; }
    public int? PositionInQueue { get; private set; }
    
    // Automatic status determination in constructor
    public EventRegistration(Event @event, User user, string? notes = null)
    {
        if (@event.IsFull())
        {
            Status = RegistrationStatus.Waitlisted;
            PositionInQueue = null; // Set by domain event handler
        }
        else
        {
            Status = RegistrationStatus.Confirmed;
            PositionInQueue = null;
        }
        
        AddDomainEvent(new EventRegistrationCreatedDomainEvent(this));
    }
    
    // Business logic methods
    public void Confirm() // Promotes from waitlist
    public void Cancel() // Cancels registration
    public void UpdateWaitlistPosition(int? position) // Updates position
    public void RemoveFromWaitlist(string reason = null) // Removes from waitlist
}
```

### Registration Status Flow

```
┌─────────────┐    Event Full    ┌─────────────┐
│   Pending   │ ───────────────▶ │ Waitlisted  │
└─────────────┘                  └─────────────┘
       │                                 │
       │ Event has space                 │ Spot opens/Admin promotes
       ▼                                 ▼
┌─────────────┐                  ┌─────────────┐
│  Confirmed  │ ◀────────────────│  Confirmed  │
└─────────────┘                  └─────────────┘
       │                                 │
       │ Event day                       │ Event day
       ▼                                 ▼
┌─────────────┐                  ┌─────────────┐
│  Attended   │                  │   NoShow    │
└─────────────┘                  └─────────────┘
```

### Domain Event Handlers

Handlers live in the Application layer and publish GraphQL notifications via an `IEventRegistrationNotifier` implemented in the API layer:
- `EventRegistrationCreatedDomainEventHandler`
- `EventRegistrationPromotedDomainEventHandler`
- `EventRegistrationCancelledDomainEventHandler`

## Testing

### Integration Tests

The system includes comprehensive integration tests covering:

- ✅ **Basic waitlist functionality** - Registration when event is full
- ✅ **Position calculation** - Correct position assignment and ordering
- ✅ **Automatic promotion** - Promotion when spots become available
- ✅ **Concurrency handling** - Multiple simultaneous registrations
- ✅ **Admin operations** - Manual promotion and cancellation
- ✅ **Domain events** - Event raising and handling
- ✅ **GraphQL subscriptions** - Real-time notifications

### Test Examples

```csharp
[Fact]
public async Task RegisterForEvent_ShouldAddToWaitlist_WhenEventIsFull()
{
    // Arrange: Create event with capacity 1
    var eventId = await CreateEventWithCapacity(1);
    await RegisterUserForEvent(eventId, "user1@test.com");
    
    // Act: Register second user
    var result = await RegisterUserForEvent(eventId, "user2@test.com");
    
    // Assert: Should be waitlisted at position 1
    result.Status.Should().Be("Waitlisted");
    result.PositionInQueue.Should().Be(1);
}
```

## Security & Authorization

### Role-Based Access Control

The system implements role-based access control using JWT tokens:

```csharp
// Admin operations require Admin role
[Authorize(RoleNames.Admin)]
public async Task<IActionResult> CancelRegistration(Guid registrationId)

// Participant operations require authentication
[Authorize]
public async Task<IActionResult> RegisterForEvent(Guid eventId)

// Public operations require no authentication
[AllowAnonymous]
public async Task<IActionResult> GetEvent(Guid eventId)
```

### Authorization Rules

1. **Event Creation/Management**: Admin only
2. **Registration Management**: Admin can manage all, users can manage own
3. **Waitlist Operations**: Admin only
4. **Event Viewing**: Public for published events
5. **User Profile**: Users can update own, admins can update any

## Error Handling

### Common Error Scenarios

| Scenario | HTTP Status | Response | Required Role |
|----------|-------------|----------|---------------|
| User already registered | 400 Bad Request | "User is already registered for this event" | Participant |
| Event not found | 404 Not Found | "Event not found" | Any |
| Event not published | 400 Bad Request | "Cannot register for unpublished event" | Participant |
| Registration deadline passed | 400 Bad Request | "Registration deadline has passed" | Participant |
| Invalid registration ID for promotion | 404 Not Found | "Registration not found" | Admin |
| Attempting to promote confirmed registration | 400 Bad Request | "Registration is already confirmed" | Admin |
| Unauthorized operation | 403 Forbidden | "Access denied" | Admin required |

## Performance Considerations

### Database Indexes

The following indexes are implemented to optimize waitlist performance:

```sql
-- For waitlist position calculations
CREATE INDEX IX_EventRegistrations_EventId_Status_CreatedAt 
ON EventRegistrations (EventId, Status, CreatedAt);

-- For waitlist queries with pagination
CREATE INDEX IX_EventRegistrations_EventId_Status_PositionInQueue 
ON EventRegistrations (EventId, Status, PositionInQueue);

-- For user-specific queries
CREATE INDEX IX_EventRegistrations_UserId_Status 
ON EventRegistrations (UserId, Status);
```

### Caching Strategy

- No caching layer is implemented yet. Planned caching will avoid caching waitlist positions due to frequent changes.

## Best Practices

### For Developers

1. **Always check Status and PositionInQueue together** when determining if a user is actively waitlisted
2. **Use domain events** for side effects like notifications - don't handle them in the main flow
3. **Position calculation should be atomic** - use database-driven calculations for concurrency
4. **Test concurrent scenarios** - waitlists are prone to race conditions
5. **Follow role-based authorization** - always check user permissions before operations

### For Frontend Integration

1. **Subscribe to GraphQL events** for real-time waitlist updates
2. **Display position clearly** - "You are #3 in line" is better than "Position: 3"
3. **Handle position changes** - Users can move up when others cancel
4. **Show promotion notifications** - Celebrate when users get promoted!
5. **Implement proper error handling** - Show appropriate messages for different error types

### For Operations

1. **Monitor waitlist lengths** - Long waitlists might indicate need for larger venues
2. **Set up alerts** for failed promotions or stuck waitlists
3. **Use admin endpoints** for manual intervention when needed
4. **Monitor domain event processing** - Ensure events are being handled correctly

## Future Enhancements

Potential future features to consider:

- 📧 **Email notifications** for waitlist position changes
- ⏰ **Waitlist expiry** - Automatic removal after specified time
- 🎯 **Priority waitlists** - Different queues based on user priority
- 📊 **Waitlist analytics** - Conversion rates and optimization insights
- 🔄 **Waitlist transfers** - Allow users to transfer their position
- 🔔 **SMS notifications** - Real-time SMS alerts for promotions
- 📱 **Mobile push notifications** - Native app notifications

---

The waitlist functionality provides a complete solution for managing event capacity with a focus on user experience, reliability, real-time updates, and proper role-based access control. All components are production-ready and thoroughly tested.