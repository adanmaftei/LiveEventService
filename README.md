# Live Event Service

A scalable, modular backend microservice for live event sign-ups built with .NET 9, AWS, and modern cloud-native practices.

## ✅ Current Status

This application is operational for local development and testing with:
- ✅ Docker Compose setup (API, Postgres, LocalStack, pgAdmin)
- ✅ PostgreSQL database with automatic migrations  
- ✅ AWS service mocking via LocalStack
- ✅ Serilog structured logging with correlation IDs
- ✅ AWS X-Ray distributed tracing
- ✅ Health checks (PostgreSQL and AWS Cognito configuration)
- ✅ Swagger (Development) and GraphQL endpoint

## Features

- **RESTful API** - Comprehensive HTTP endpoints for all operations
- **GraphQL API** - Flexible query language for efficient data fetching
- **Real-time Subscriptions** - WebSocket support for live updates
- **Event Waitlist Management** - Automatic waitlist handling with position tracking and promotion
- **Authentication & Authorization** - JWT-based authentication with AWS Cognito
- **Containerized Deployment** - Docker and AWS ECS Fargate for scalable hosting
- **Infrastructure as Code** - AWS CDK for reproducible infrastructure
- **CI/CD Pipeline** - GitHub Actions for automated testing and deployment
- **Observability** - Built-in logging, metrics, and tracing

## Tech Stack

- **Backend**: .NET 9, C#
- **Database**: PostgreSQL with Entity Framework Core
- **API**: REST, GraphQL (HotChocolate)
- **Infrastructure**: AWS (ECS, RDS, Cognito, API Gateway, etc.)
- **CI/CD**: GitHub Actions
- **Containerization**: Docker
- **Monitoring**: AWS CloudWatch, X-Ray
- **Testing**: xUnit, Moq, FluentAssertions

## Quick Start - Get Running in 2 Minutes! 🚀

### Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop) installed and **running**
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) (optional, for local development)

### Option 1: Full Docker Setup (Recommended)

```bash
# Clone and start everything
git clone https://github.com/yourusername/live-event-service.git
cd live-event-service
docker-compose up -d

# Verify everything is working
curl http://localhost:5000/health
```

**That's it!** 🎉 Access your services:
- **API Health**: http://localhost:5000/health
- **Swagger UI**: http://localhost:5000/swagger/index.html
- **GraphQL Playground**: http://localhost:5000/graphql
- **Database Admin**: http://localhost:5050 (admin@example.com / admin)

### Option 2: Development Mode

```bash
# Start supporting services
docker-compose up -d db localstack pgadmin

# Run API locally for development
dotnet run --project src/LiveEventService.API
```

## 🌟 Working Services

| Service | Status | URL | Purpose |
|---------|--------|-----|---------|
| **Live Event API** | ✅ Running | http://localhost:5000 | Main API with all endpoints |
| **Swagger UI** | ✅ Running | http://localhost:5000/swagger | API documentation |
| **GraphQL** | ✅ Running | http://localhost:5000/graphql | GraphQL playground |
| **PostgreSQL** | ✅ Running | http://localhost:5432 | Database with test data |
| **pgAdmin** | ✅ Running | http://localhost:5050 | Database management |
| **LocalStack** | ✅ Running | http://localhost:4566 | AWS services mocking |

## Project Structure

```
src/
  LiveEventService.API/         # API project (Minimal APIs + GraphQL)
  LiveEventService.Application/ # Application layer (CQRS, DTOs, validators)
  LiveEventService.Core/        # Domain models, interfaces, exceptions
  LiveEventService.Infrastructure/ # Data access, external services
  infrastructure/               # AWS CDK infrastructure code
tests/
  LiveEventService.Tests/       # Unit and integration tests
docs/                          # Comprehensive documentation
```

## Vertical Slice Architecture

This project uses a vertical slice architecture. Each feature (Events, Users, Registrations) is grouped by feature in each layer:

- **Core**: Domain models, interfaces, and domain events for each feature
- **Application**: Commands, queries, handlers, DTOs, and notifications for each feature
- **Infrastructure**: Repositories and configurations for each feature
- **API**: Endpoints, GraphQL types, queries, mutations, and subscriptions for each feature

This structure makes it easy to find all code for a feature and enables true feature-based development.

## API Documentation

### REST API Endpoints

| Method | Route                                         | Auth Roles         | Description                       |
|--------|-----------------------------------------------|--------------------|-----------------------------------|
| GET    | `/api/events`                                | Anonymous          | List published/upcoming events    |
| GET    | `/api/events/{id}`                           | Anonymous          | Get event by ID                   |
| POST   | `/api/events`                                | Admin              | Create event                      |
| PUT    | `/api/events/{id}`                           | Admin              | Update event                      |
| DELETE | `/api/events/{id}`                           | Admin              | Delete event                      |
| POST   | `/api/events/{eventId}/register`             | Authenticated      | Register for event (auto-waitlist if full) |
| GET    | `/api/events/{eventId}/registrations`        | Admin              | List registrations for an event   |
| GET    | `/api/events/{eventId}/waitlist`             | Admin              | List waitlisted registrations with positions |
| POST   | `/api/events/{eventId}/registrations/{id}/confirm` | Admin        | Promote registration from waitlist |
| POST   | `/api/events/{eventId}/registrations/{id}/cancel`  | Admin        | Cancel registration (admin action) |
| POST   | `/api/events/{eventId}/publish`              | Admin              | Publish event                     |
| POST   | `/api/events/{eventId}/unpublish`            | Admin              | Unpublish event                   |
| GET    | `/api/users`                                 | Admin             | List users                        |
| GET    | `/api/users/me`                              | Authenticated      | Get current user                  |
| GET    | `/api/users/{id}`                            | Admin             | Get user by ID                    |
| POST   | `/api/users`                                 | Admin             | Create user (no public sign-up)   |
| PUT    | `/api/users/{id}`                            | Authenticated      | Update user (self or admin)       |

### Health Checks

- **Endpoint**: `GET /health` - Returns service health status
- **Checks**: PostgreSQL connectivity, AWS Cognito configuration

### Authentication

The API uses JWT tokens from AWS Cognito (mocked via LocalStack in development):

```bash
# Health check (no auth required)
curl http://localhost:5000/health

# With correlation ID for tracing
curl -H "X-Correlation-ID: test-123" http://localhost:5000/health
```

## 🔧 Advanced Features

### Serilog Structured Logging
- JSON formatted logs with correlation IDs
- Request/response logging with timing
- CloudWatch integration ready for production

### AWS X-Ray Distributed Tracing  
- Complete request tracing across all components
- SQL query tracing and performance monitoring
- LocalStack integration for development

### Entity Framework Migrations
- Automatic database schema creation
- Test data seeding in development
- Clean domain models with proper EF configurations

## AWS Deployment

### Prerequisites

1. AWS Account with appropriate permissions
2. AWS CLI configured with credentials
3. AWS CDK bootstrapped in your AWS account

### Deploying the Infrastructure

```bash
cd src/infrastructure
npm install -g aws-cdk
cdk bootstrap
cdk deploy
```

Note: CI/CD workflows are not included in this repository. See `docs/CICD.md` for a proposed workflow.

## Monitoring & Observability

- **Health Checks**: `/health` endpoint with PostgreSQL and AWS Cognito config checks
- **Logging**: Structured Serilog with correlation ID tracking
- **Tracing**: AWS X-Ray distributed tracing for performance monitoring
- **Metrics**: AWS CloudWatch integration ready for production

## Development Workflow

```bash
# Start development environment
docker-compose up -d

# Make changes to your code
# Restart only the API container to see changes
docker-compose restart api

# View logs
docker logs liveevent-api --tail 50

# Reset everything if needed
docker-compose down --volumes && docker-compose up -d
```

## Testing

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/LiveEventService.Tests

# Run with coverage
dotnet test /p:CollectCoverage=true
```

## Documentation

- 📖 **[Local Development Setup](docs/LOCAL_DEVELOPMENT_SETUP.md)** - Complete setup guide
- 🚀 **[CI/CD Pipeline](docs/CICD.md)** - GitHub Actions deployment automation
- 📁 **[Solution Structure](docs/SOLUTION_STRUCTURE.md)** - Project organization guide
- 🧪 **[Integration Testing](docs/INTEGRATION_TESTING.md)** - Testcontainers & authentication testing
- 🎟️ **[Waitlist Functionality](docs/WAITLIST_FUNCTIONALITY.md)** - Event waitlist and auto-promotion guide
- 📊 **[Logging](docs/LOGGING.md)** - Serilog configuration and usage
- 🔍 **[Tracing](docs/TRACING.md)** - AWS X-Ray distributed tracing
- 🏥 **[Monitoring](docs/MONITORING.md)** - Health checks and CloudWatch
- 🔗 **[API Documentation](docs/API_MINIMAL.md)** - Minimal API implementation
- 🛡️ **[Compliance](docs/COMPLIANCE.md)** - GDPR and privacy considerations
- 🔄 **[Backup & DR](docs/BACKUP_AND_DR.md)** - Backup and disaster recovery
 - 🛡️ **[Security Enhancements](docs/SECURITY_ENHANCEMENTS.md)** - Planned security hardening (rate limiting, headers, HTTPS/HSTS)
 - ⚙️ **[Architecture Analysis & Improvements](docs/ARCHITECTURE_ANALYSIS_AND_IMPROVEMENTS.md)** - Current gaps and roadmap
 - 📈 **[Performance & Scalability](docs/PERFORMANCE_AND_SCALABILITY.md)** - Performance bottlenecks and plans
 - 📈 **[Scalability Improvements](docs/SCALABILITY_IMPROVEMENTS.md)** - Scaling strategies
 - 🧭 **[Domain Events & GraphQL](docs/DOMAIN_EVENTS_AND_GRAPHQL.md)** - Event flow and real-time notifications
 - 🧾 **[TODO: Performance Improvements](docs/TODO_PERFORMANCE_IMPROVEMENTS.md)** - Implementation checklist/roadmap
 - 💸 **[Cost Optimization](docs/COST_OPTIMIZATION.md)** - AWS cost-saving strategies
 - 🚢 **[Deployment Optimization](docs/DEPLOYMENT_OPTIMIZATION.md)** - Blue/green, canary, multi-region (future-state)

## Contributing

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

- **Health Status**: http://localhost:5000/health
- **API Documentation**: http://localhost:5000/swagger
- **Issues**: Open a GitHub issue for support

---

🎉 **Ready to go!** Run `docker-compose up -d` and start building amazing event management features!
