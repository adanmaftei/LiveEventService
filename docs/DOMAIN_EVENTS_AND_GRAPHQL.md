# Domain Events and GraphQL Subscriptions

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

## How Events Are Handled
- In the API project, MediatR `INotificationHandler`s listen for these events.
- For each event, a handler publishes a `EventRegistrationNotification` to the appropriate HotChocolate GraphQL topic (e.g., `eventRegistration_{eventId}`).
- The notification includes the event ID, user info, action, and timestamp.

## GraphQL Subscriptions
- Clients can subscribe to `eventRegistration_{eventId}` topics to receive real-time updates about registrations for a specific event.
- Actions include: `created`, `promoted`, `cancelled`.

## Example
When a user registers for a full event:
- `EventRegistrationCreatedDomainEvent` is raised (waitlisted).
- If a spot opens and they're promoted, `EventRegistrationPromotedDomainEvent` is raised.
- If they cancel, `EventRegistrationCancelledDomainEvent` is raised.
- Each event triggers a real-time GraphQL notification to subscribers. 