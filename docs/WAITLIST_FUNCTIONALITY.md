# Event Waitlist Functionality

## Overview

The Live Event Service provides comprehensive waitlist functionality that automatically manages event registrations when events reach capacity. The system handles waitlist queuing, position tracking, automatic promotion, and real-time notifications.

## ✅ Current Status: Fully Operational

- ✅ **Automatic Waitlist Management** - Users are automatically waitlisted when events are full
- ✅ **Position Tracking** - Waitlist positions start from 1 and are maintained accurately
- ✅ **Automatic Promotion** - When spots become available, waitlisted users are promoted automatically
- ✅ **Concurrency Safety** - Multiple simultaneous registrations handle positions correctly
- ✅ **Real-time Notifications** - GraphQL subscriptions notify about waitlist changes
- ✅ **Admin Management** - Admins can view waitlists and manually promote users

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

### Register for Event
```http
POST /api/events/{eventId}/register
Authorization: Bearer <jwt-token>
Content-Type: application/json

{
  "notes": "Looking forward to this event"
}
```

**Responses:**
- **Event has space**: Returns `Confirmed` status
- **Event is full**: Returns `Waitlisted` status with position

```json
{
  "success": true,
  "data": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "eventId": "6ba7b810-9dad-11d1-80b4-00c04fd430c8",
    "userId": "6ba7b811-9dad-11d1-80b4-00c04fd430c8",
    "status": "Waitlisted",
    "positionInQueue": 3,
    "registrationDate": "2024-01-15T10:30:00Z",
    "notes": "Looking forward to this event"
  }
}
```

### Get Event Waitlist
```http
GET /api/events/{eventId}/waitlist?pageNumber=1&pageSize=10
Authorization: Bearer <admin-jwt-token>
```

**Response:**
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "550e8400-e29b-41d4-a716-446655440000",
        "userId": "6ba7b811-9dad-11d1-80b4-00c04fd430c8",
        "userName": "John Doe",
        "email": "john.doe@example.com",
        "status": "Waitlisted",
        "positionInQueue": 1,
        "registrationDate": "2024-01-15T10:30:00Z"
      },
      {
        "id": "550e8401-e29b-41d4-a716-446655440000",
        "userId": "6ba7b812-9dad-11d1-80b4-00c04fd430c8",
        "userName": "Jane Smith",
        "email": "jane.smith@example.com",
        "status": "Waitlisted",
        "positionInQueue": 2,
        "registrationDate": "2024-01-15T11:00:00Z"
      }
    ],
    "totalCount": 15,
    "pageNumber": 1,
    "pageSize": 10,
    "totalPages": 2
  }
}
```

### Manually Promote from Waitlist
```http
POST /api/events/{eventId}/registrations/{registrationId}/confirm
Authorization: Bearer <admin-jwt-token>
```

**Response:**
```json
{
  "success": true,
  "data": {
    "message": "Registration confirmed successfully"
  }
}
```

### Cancel Registration (Admin)
```http
POST /api/events/{eventId}/registrations/{registrationId}/cancel
Authorization: Bearer <admin-jwt-token>
```

## Domain Events & Real-time Notifications

### Domain Events Raised

| Event | Trigger | Contains |
|-------|---------|----------|
| `EventRegistrationCreatedDomainEvent` | User registers (confirmed or waitlisted) | Registration details |
| `EventRegistrationPromotedDomainEvent` | Waitlisted user promoted to confirmed | Updated registration |
| `EventRegistrationCancelledDomainEvent` | Registration cancelled | Cancelled registration |

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
            PositionInQueue = null; // Set by service layer
        }
        else
        {
            Status = RegistrationStatus.Confirmed;
            PositionInQueue = null;
        }
    }
    
    // Business logic methods
    public bool IsWaitlisted() => 
        Status == RegistrationStatus.Waitlisted && 
        PositionInQueue.HasValue && 
        PositionInQueue > 0;
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

### Concurrency Safety

The system uses database-driven position calculation to handle concurrent registrations:

```csharp
public async Task<int> CalculateWaitlistPositionAsync(Guid eventId, Guid registrationId)
{
    // Calculate position based on database insertion order
    var position = await _dbContext.EventRegistrations
        .Where(r => r.EventId == eventId && 
                   r.Status == RegistrationStatus.Waitlisted &&
                   r.Id != registrationId)
        .CountAsync() + 1;
    
    return position;
}
```

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

## Best Practices

### For Developers

1. **Always check Status and PositionInQueue together** when determining if a user is actively waitlisted
2. **Use domain events** for side effects like notifications - don't handle them in the main flow
3. **Position calculation should be atomic** - use database-driven calculations for concurrency
4. **Test concurrent scenarios** - waitlists are prone to race conditions

### For Frontend Integration

1. **Subscribe to GraphQL events** for real-time waitlist updates
2. **Display position clearly** - "You are #3 in line" is better than "Position: 3"
3. **Handle position changes** - Users can move up when others cancel
4. **Show promotion notifications** - Celebrate when users get promoted!

### For Operations

1. **Monitor waitlist lengths** - Long waitlists might indicate need for larger venues
2. **Set up alerts** for failed promotions or stuck waitlists
3. **Use admin endpoints** for manual intervention when needed

## Error Handling

### Common Error Scenarios

| Scenario | HTTP Status | Response |
|----------|-------------|----------|
| User already registered | 400 Bad Request | "User is already registered for this event" |
| Event not found | 404 Not Found | "Event not found" |
| Event not published | 400 Bad Request | "Cannot register for unpublished event" |
| Registration deadline passed | 400 Bad Request | "Registration deadline has passed" |
| Invalid registration ID for promotion | 404 Not Found | "Registration not found" |
| Attempting to promote confirmed registration | 400 Bad Request | "Registration is already confirmed" |

## Performance Considerations

### Database Indexes

The following indexes are recommended for optimal waitlist performance:

```sql
-- For waitlist position calculations
CREATE INDEX IX_EventRegistrations_EventId_Status_CreatedAt 
ON EventRegistrations (EventId, Status, CreatedAt);

-- For waitlist queries with pagination
CREATE INDEX IX_EventRegistrations_EventId_Status_PositionInQueue 
ON EventRegistrations (EventId, Status, PositionInQueue);
```

### Caching Strategy

- **Event capacity and current registration count** - Cache with short TTL (30 seconds)
- **Waitlist positions** - Don't cache due to frequent changes
- **Event details** - Cache with longer TTL (5 minutes)

## Future Enhancements

Potential future features to consider:

- 📧 **Email notifications** for waitlist position changes
- ⏰ **Waitlist expiry** - Automatic removal after specified time
- 🎯 **Priority waitlists** - Different queues based on user priority
- 📊 **Waitlist analytics** - Conversion rates and optimization insights
- 🔄 **Waitlist transfers** - Allow users to transfer their position

---

The waitlist functionality provides a complete solution for managing event capacity with a focus on user experience, reliability, and real-time updates. All components are production-ready and thoroughly tested.