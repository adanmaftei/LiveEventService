# API Documentation - Live Event Service

## ✅ Current Status

The Live Event Service API includes:
- ✅ Minimal API endpoints for Events, Registrations, and Users
- ✅ Admin endpoints (confirm, cancel, publish, unpublish)
- ✅ Swagger UI (Development)
- ✅ Health checks (PostgreSQL, AWS Cognito, and AWS S3 when configured)
- ✅ JWT authentication wiring (Cognito config; tests use test auth)
- ✅ CORS policy sourced from `Security:Cors:AllowedOrigins` (allow-all in Development/Testing when unset)
- ✅ Serilog structured logging with correlation IDs
- ✅ OpenTelemetry metrics (Prometheus) and tracing via OTLP → OTel Collector → Jaeger (local); ADOT/X-Ray (prod)
- ✅ Field-level encryption for PII via EF value converters; keys sourced from AWS Secrets Manager (KMS-backed)

## Overview

The Events REST API is implemented using .NET 9 Minimal APIs directly in `Program.cs`. This approach:
- Reduces boilerplate code
- Improves performance
- Provides better testability
- Aligns with modern .NET patterns

## Quick Access

- **API Base URL**: http://localhost:5000
- **Swagger UI** (Development): http://localhost:5000/
- **Swagger JSON**: http://localhost:5000/swagger/v1/swagger.json
- **Health Check**: http://localhost:5000/health
 - **GraphQL Endpoint**: http://localhost:5000/graphql
 - **GraphQL Playground** (Development): http://localhost:5000/graphql/playground
 
### Security & Privacy
- PII fields are encrypted at rest using AES; secrets are supplied via `Security:Encryption:Key` and `Security:Encryption:IV` injected from Secrets Manager. In dev/test without secrets, converters pass through for ease of setup.

## REST API Endpoints

### Events Management

| Method | Route                                         | Auth Roles         | Description                       | Status |
|--------|-----------------------------------------------|--------------------|-----------------------------------|--------|
| GET    | `/api/events`                                | Anonymous          | List published/upcoming events    | ✅ Working |
| GET    | `/api/events/{id}`                           | Anonymous          | Get event by ID                   | ✅ Working |
| POST   | `/api/events`                                | Admin              | Create event                      | ✅ Working |
| PUT    | `/api/events/{id}`                           | Admin              | Update event                      | ✅ Working |
| DELETE | `/api/events/{id}`                           | Admin              | Delete event                      | ✅ Working |
| POST   | `/api/events/{eventId}/register`             | Authenticated      | Register for event                | ✅ Working |
| GET    | `/api/events/{eventId}/registrations`        | Admin              | List registrations for an event   | ✅ Working |
| GET    | `/api/events/{eventId}/registrations/export` | Admin              | Export registrations as CSV       | ✅ Working |
| GET    | `/api/events/{eventId}/waitlist`             | Admin              | List waitlisted registrations     | ✅ Working |
| POST   | `/api/events/{eventId}/registrations/{registrationId}/confirm` | Admin | Confirm registration (promote from waitlist) | ✅ Working |
| POST   | `/api/events/{eventId}/registrations/{registrationId}/cancel` | Admin | Cancel registration (admin action) | ✅ Working |
| POST   | `/api/events/{eventId}/publish`              | Admin              | Publish event                     | ✅ Working |
| POST   | `/api/events/{eventId}/unpublish`            | Admin              | Unpublish event                   | ✅ Working |

### User Management

| Method | Route                                         | Auth Roles         | Description                       | Status |
|--------|-----------------------------------------------|--------------------|-----------------------------------|--------|
| GET    | `/api/users`                                 | Admin             | List users                        | ✅ Working |
| GET    | `/api/users/me`                              | Authenticated      | Get current user                  | ✅ Working |
| GET    | `/api/users/{id}`                            | Admin             | Get user by ID                    | ✅ Working |
| POST   | `/api/users`                                 | Admin             | Create user (no public sign-up)   | ✅ Working |
| PUT    | `/api/users/{id}`                            | Authenticated      | Update user (self or admin)       | ✅ Working |
| GET    | `/api/users/{id}/export`                     | Self, Admin        | Export user data (JSON file)      | ✅ Working |
| DELETE | `/api/users/{id}`                            | Admin              | Erase/anonymize user              | ✅ Working |

### System Endpoints

| Method | Route                | Description                           | Status |
|--------|----------------------|---------------------------------------|--------|
| GET    | `/health`           | Health check with service status     | ✅ Working |
| GET    | `/`          | API documentation (Swagger UI in Dev)       | ✅ Working |
| GET    | `/graphql`          | GraphQL endpoint (playground in development) | ✅ Working |

## Health Check Details

### Endpoint
```
GET /health
```

### Response Format
```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0234567",
  "entries": {
    "PostgreSQL (RDS)": {
      "status": "Healthy",
      "duration": "00:00:00.0123456",
      "data": {}
    },
    "AWS Cognito": {
      "status": "Healthy",
      "duration": "00:00:00.0001234",
      "data": {}
    }
  }
}
```

### Health Check Components
- **PostgreSQL (RDS)**: Database connectivity and responsiveness
- **AWS Cognito**: Configuration presence validation (User Pool, Region)
- **AWS S3**: Bucket reachability when `AWS:S3BucketName` is set (LocalStack supported)

### Testing Health Checks
```bash
# Basic health check
curl http://localhost:5000/health

# Health check with correlation ID
curl -H "X-Correlation-ID: health-test-123" http://localhost:5000/health

# Pretty print JSON response
curl -s http://localhost:5000/health | jq
```

## API Usage Examples

### Events API

#### List Events
```bash
# Get all events
curl http://localhost:5000/api/events

# With authentication (when implemented)
curl -H "Authorization: Bearer <jwt-token>" http://localhost:5000/api/events
```

#### Get Specific Event
```bash
curl http://localhost:5000/api/events/{event-id}
```

#### Create Event (Admin Only)
```bash
curl -X POST http://localhost:5000/api/events \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <admin-jwt-token>" \
  -d '{
    "name": "Tech Conference 2024",
    "description": "Annual technology conference",
    "startDate": "2024-06-15T09:00:00Z",
    "endDate": "2024-06-15T17:00:00Z",
    "capacity": 100,
    "timeZone": "UTC",
    "location": "Conference Center"
  }'
```

#### Register for Event
```bash
curl -X POST http://localhost:5000/api/events/{event-id}/register \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <user-jwt-token>" \
  -d '{
    "notes": "Looking forward to this event"
  }'
```

#### Get Event Waitlist (Admin)
```bash
curl -H "Authorization: Bearer <admin-jwt-token>" \
  "http://localhost:5000/api/events/{event-id}/waitlist?pageNumber=1&pageSize=10"
```

#### Promote from Waitlist (Admin)
```bash
curl -X POST http://localhost:5000/api/events/{event-id}/registrations/{registration-id}/confirm \
  -H "Authorization: Bearer <admin-jwt-token>"
```

#### Cancel Registration (Admin)
```bash
curl -X POST http://localhost:5000/api/events/{event-id}/registrations/{registration-id}/cancel \
  -H "Authorization: Bearer <admin-jwt-token>"
```

### Users API

#### Get Current User
```bash
curl -H "Authorization: Bearer <jwt-token>" http://localhost:5000/api/users/me
```

#### List Users (Admin Only)
```bash
curl -H "Authorization: Bearer <admin-jwt-token>" http://localhost:5000/api/users
```

## Clean Architecture Mapping

### API Layer (Program.cs)
- Defines endpoints and handles HTTP concerns
- Authentication and authorization
- Request/response validation
- HTTP status codes and error handling

### Application Layer (CQRS/MediatR)
- Business logic implementation
- Command and query handlers
- Data transfer objects (DTOs)
- Validation rules

### Core Layer (Domain)
- Domain models and entities
- Business rules and domain events
- Interfaces and abstractions

### Infrastructure Layer
- Data access with Entity Framework
- External service integrations (AWS)
- Repository implementations

## Authentication & Authorization

### JWT Token Support
The API supports JWT tokens from AWS Cognito:

```bash
# Include JWT token in Authorization header
curl -H "Authorization: Bearer <your-jwt-token>" http://localhost:5000/api/events
```

### Role-Based Access Control
- **Admin**: Full access to event and user management endpoints (create/update/delete events, publish/unpublish, list registrations and waitlist, confirm/cancel registrations)
- **Participant**: Authenticate to register for events and manage own profile; read published events
- **Anonymous**: Public reads (health check, published events and details)

### Development & Tests
- Local development uses Cognito config; integration tests replace auth with a test scheme.

## Error Handling

### Standard HTTP Status Codes
- `200 OK`: Successful request
- `201 Created`: Resource created successfully
- `400 Bad Request`: Invalid request data
- `401 Unauthorized`: Authentication required
- `403 Forbidden`: Insufficient permissions
- `404 Not Found`: Resource not found
- `500 Internal Server Error`: Server error

### Error Response Format
```json
{
  "message": "An unexpected error occurred.",
  "errors": ["... details (optional) ..."]
}
```
For validation failures (HTTP 400), the response is:
```json
{
  "message": "Validation failed",
  "errors": ["<validation error 1>", "<validation error 2>"]
}
```

## Swagger/OpenAPI Documentation

### Access Swagger UI
Navigate to: http://localhost:5000/

### Features
- **Interactive API Testing**: Test endpoints directly from the browser
- **JWT Authentication**: Configure Bearer token for testing protected endpoints
- **Request/Response Examples**: See sample data formats
- **Schema Documentation**: Detailed model definitions

### Testing with Swagger
1. Open http://localhost:5000/
2. Click "Authorize" to add JWT token (when available)
3. Select an endpoint to test
4. Fill in required parameters
5. Click "Execute" to send the request

## GraphQL Integration

### GraphQL Playground
Access endpoint: http://localhost:5000/graphql
Playground (Dev): http://localhost:5000/graphql/playground

### Available Operations
- **Queries**: Fetch events, users, registrations
- **Mutations**: Create, update, delete operations
- **Subscriptions**: Real-time updates (when implemented)
  - **Performance**: Execution timeout (10s), strict validation; DataLoader eliminates N+1 for organizer name resolution

## Domain Events and Async Processing (Implemented)

- In-process background processing is enabled by default in development/testing via `DomainEventBackgroundService`.
- In production, SQS-backed processing is enabled by setting `AWS:SQS:UseSqsForDomainEvents=true`; the API publishes, and the `LiveEventService.Worker` service consumes.
- A transactional outbox runs alongside the API (`OutboxProcessorBackgroundService`) and publishes to SNS topics for cross-service fan-out.

Configuration flags:

- `Performance:BackgroundProcessing:UseInProcess` (default true in dev)
- `AWS:SQS:UseSqsForDomainEvents` (set to true in CDK for prod)

## Output Caching (REST)

- Output caching is enabled for public event reads:
  - Event list: policy `EventListPublic` (60s; varies by query params; anonymous-safe)
  - Event detail: policy `EventDetailPublic` (120s; varies by id; anonymous-safe)
- Cache tags are not required for these short TTLs; data-level cache invalidation remains for stronger consistency on mutations.

### Example GraphQL Query
```graphql
query GetEvents {
  events {
    id
    name
    description
    startDate
    endDate
    capacity
    registrations {
      id
      user {
        name
        email
      }
      status
    }
  }
}
```

## CORS Configuration

### Allowed Origins
Configured via `Security:Cors:AllowedOrigins`.
- Development/Testing: if unset, all origins are allowed by default to simplify local development.
- Production: set explicit origins, for example `https://app.example.com`.

### CORS Headers
- `Access-Control-Allow-Origin`
- `Access-Control-Allow-Methods`
- `Access-Control-Allow-Headers`

## Performance & Monitoring

### Request Logging and Caching
All requests are logged with:
- HTTP method and path
- Response status and timing
- Correlation ID for tracing
- User agent and IP address

Public GETs include short Cache-Control headers to enable browser/CDN caching:
- Event list: `Cache-Control: public, max-age=60`
- Event detail: `Cache-Control: public, max-age=120`

### Distributed Tracing
- **Local**: Traces exported via OTLP to OTel Collector and visible in Jaeger
- **Production**: ADOT Collector exports traces to X-Ray
- **Correlation IDs**: Track requests across services
- **SQL Query Tracing**: Database operations monitored

### Metrics Available
- Request count and duration
- Error rates by endpoint
- Database query performance
- Health check status

## Security Notes

- JWT authentication configured; integration tests use a test auth handler.
- CORS default policy is configured from `AllowedOrigins`.
- HTTPS redirection (non-dev) and HSTS (prod) enabled.
- Security headers middleware adds X-Frame-Options, X-Content-Type-Options, Referrer-Policy, Permissions-Policy, and CSP (non-dev).
- Rate limiting: general policy (100 req/min per IP) and registration policy (5 req/min per user/IP). Disabled in Testing.
  - Applied policies:
    - Events and Users endpoints: "general"
    - Registration endpoints: "registration"
    - GraphQL (`/graphql`): "general"
    - Health endpoints (`/health`, `/health/ready`, `/health/live`): not rate-limited

## Development Workflow

### Testing Endpoints
```bash
# Start the application
docker-compose up -d

# Test health check
curl http://localhost:5000/health

# Access Swagger UI
open http://localhost:5000/

# Test GraphQL
open http://localhost:5000/graphql
```

### Adding New Endpoints
1. Define endpoint in `Program.cs` using minimal API syntax
2. Create corresponding command/query in Application layer
3. Implement handler with business logic
4. Add appropriate authorization requirements
5. Update Swagger documentation with XML comments

## Benefits

- **Performance**: Minimal overhead compared to controller-based APIs
- **Simplicity**: Less boilerplate code and configuration
- **Testability**: Easy to unit test individual endpoints
- **Modern**: Aligns with .NET 9 best practices

## Next Steps

### Immediate Development
1. **Frontend Integration**: Connect React/Vue.js app to API
2. **Authentication Testing**: Test with real JWT tokens
3. **Data Validation**: Expand input validation coverage
4. **API Versioning**: Implement versioning strategy as the API surface grows

### Production Readiness
1. **Caching**: Evaluate response caching where appropriate
2. **Documentation**: Generate OpenAPI spec for client generation

## Support Resources

- **API Health**: http://localhost:5000/health
- **API Documentation**: http://localhost:5000/swagger/index.html
- **GraphQL Playground**: http://localhost:5000/graphql
- **Local Development Guide**: [LOCAL_DEVELOPMENT_SETUP.md](./LOCAL_DEVELOPMENT_SETUP.md) 