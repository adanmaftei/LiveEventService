# Live Event Service

A scalable, modular backend microservice for live event sign-ups built with .NET 9, AWS, and modern cloud-native practices.

## ✅ Current Status

This application is operational for local development and testing with:
- ✅ Docker Compose setup (API, Postgres, LocalStack, pgAdmin)
- ✅ PostgreSQL database with automatic migrations  
- ✅ AWS service mocking via LocalStack
- ✅ Serilog structured logging with correlation IDs
- ✅ OpenTelemetry metrics (Prometheus) and tracing via OTLP → ADOT Collector → AWS X-Ray
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

## What's implemented

- **Deployed on AWS**: Infrastructure as Code with AWS CDK provisioning ECS Fargate (API/Worker), ALB + WAF, RDS PostgreSQL, ElastiCache Redis, SQS, Cognito, CloudWatch, X-Ray. Optional Route 53 failover DNS and Aurora Global.
- **Business logic in C#**: .NET 9 solution with Clean Architecture (Core/Application/Infrastructure/API/Worker), CQRS + MediatR.
- **REST and GraphQL APIs**: Minimal APIs for REST; HotChocolate for GraphQL Queries/Mutations/Subscriptions. Admin endpoints for event management and participant export.
- **Global availability**: Multi‑AZ RDS; optional Route 53 failover DNS; optional Aurora PostgreSQL Global Database with a replica stack for a second region; secondary GitHub Actions workflow for NA region.
- **Privacy & legal (PII)**: Field‑level encryption for PII via EF value converters (AES); secrets sourced from AWS Secrets Manager (KMS‑backed). DSAR export/erasure endpoints; opt‑in retention job for automated anonymization/deletion.
- **CI/CD and Git**: GitHub Actions (lint/format, unit/integration tests, deploy with EF migrations and health smoke test). Secondary region deploy workflow included.
- **Security best practices**: Cognito JWT auth, role‑based authorization, rate limiting, security headers (CSP, HSTS, Referrer‑Policy, Permissions‑Policy), Kestrel hardening, private subnets, VPC endpoints, dedicated audit log sink. Optional CSP report endpoint can be added.
- **Observability**: Serilog structured logs with correlation IDs; OpenTelemetry metrics/traces; ADOT → X‑Ray; Prometheus endpoint; health checks (PostgreSQL, Cognito, S3 when configured).
- **Maintainability/cost**: CDK parameters for capacity; autoscaling for API/Worker; VPC endpoints to reduce NAT costs; deferred AMP/AMG by default; blue/green traffic shifting optional.
- **Scalability/elasticity**: ECS autoscaling (CPU/requests), SQS worker autoscaling based on backlog, Redis cache, output caching.
- **Frontend not in scope**: This repo provides backend services only.

## Tech Stack

- **Backend**: .NET 9, C#
- **Database**: PostgreSQL with Entity Framework Core
- **API**: REST (Minimal APIs) and GraphQL (HotChocolate)
- **Infrastructure**: AWS (ECS Fargate, ALB + WAF, RDS, Cognito, SQS)
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
- **GraphQL Playground**: http://localhost:5000/graphql (Development only)
- **Database Admin**: http://localhost:5050 (admin@example.com / admin)
- **Prometheus**: http://localhost:9090
- **Grafana**: http://localhost:3000 (admin/admin)
- **Jaeger (Tracing UI)**: http://localhost:16686

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
| **Redis** | ✅ Running | redis://localhost:6379 | Distributed cache / backplane |
| **Prometheus** | ✅ Running | http://localhost:9090 | Metrics scraping & queries |
| **Grafana** | ✅ Running | http://localhost:3000 | Dashboards (Prometheus/Loki) |
| **Loki** | ✅ Running | http://localhost:3100 | Logs datastore (queried via Grafana) |
| **Jaeger** | ✅ Running | http://localhost:16686 | Tracing UI |
| **ADOT Collector** | ✅ Running | n/a | OpenTelemetry collector (no UI) |
| **Promtail** | ✅ Running | n/a | Log shipper to Loki (no UI) |

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

The API uses JWT tokens from AWS Cognito (tests use a test auth handler; LocalStack used in dev):

```bash
# Health check (no auth required)
curl http://localhost:5000/health

# With correlation ID for tracing
curl -H "X-Correlation-ID: test-123" http://localhost:5000/health
```

### CORS

- CORS is environment-driven via `Security:Cors:AllowedOrigins`.
- In Development/Testing, or when no allowed origins are configured, the API allows all origins by default.
- In Production, set `Security:Cors:AllowedOrigins` to the list of trusted frontend origins.

## 🔧 Advanced Features

### Serilog Structured Logging
- JSON formatted logs with correlation IDs
- Request/response logging with timing
- CloudWatch integration ready for production

### OpenTelemetry Tracing (via ADOT → X-Ray)
- Request tracing across components (ASP.NET Core + HttpClient instrumentation)
- Prometheus metrics endpoint exposed; Prometheus included in docker-compose
- OTLP exporter configured to ADOT Collector; ADOT exports to AWS X-Ray. In production, container logs flow to CloudWatch via ECS log driver, and audit logs go to a dedicated log group.

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

CI/CD workflows are included. See `docs/CICD.md` for details (lint/format check, unit + integration tests, deploy with EF migrations, health smoke test).

## Monitoring & Observability

- **Health Checks**: `/health` endpoint with PostgreSQL and AWS Cognito config checks
- **Logging**: Structured Serilog with correlation ID tracking
- **Tracing**: AWS X-Ray via ADOT Collector
- **Metrics**: CloudWatch EMF in production; Prometheus locally

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
 - ✅ **[Deployment Checklist](docs/DEPLOYMENT_CHECKLIST.md)** - Exact AWS and GitHub setup steps
- 📁 **[Solution Structure](docs/SOLUTION_STRUCTURE.md)** - Project organization guide
- 🧪 **[Integration Testing](docs/INTEGRATION_TESTING.md)** - Testcontainers & authentication testing
- 🎟️ **[Waitlist Functionality](docs/WAITLIST_FUNCTIONALITY.md)** - Event waitlist and auto-promotion guide
- 📊 **[Logging](docs/LOGGING.md)** - Serilog configuration and usage
- 🔍 **[Tracing](docs/TRACING.md)** - AWS X-Ray distributed tracing
- 🏥 **[Monitoring](docs/MONITORING.md)** - Health checks and CloudWatch
- 🔗 **[API Documentation](docs/API_MINIMAL.md)** - Minimal API implementation
  - 📋 **[Improvement Plan](docs/IMPROVEMENT_PLAN.md)** - Source of truth for improvements and status
- 🛡️ **[Compliance](docs/COMPLIANCE.md)** - GDPR and privacy considerations
- 🔄 **[Backup & DR](docs/BACKUP_AND_DR.md)** - Backup and disaster recovery
 - 🛡️ **[Security Enhancements](docs/SECURITY_ENHANCEMENTS.md)** - Security baseline (HTTPS/HSTS, headers, rate limiting) + next steps
 - 📬 **[Domain Events & Outbox](docs/DOMAIN_EVENTS_AND_GRAPHQL.md)** - Domain events, GraphQL notifications, and transactional outbox
 - ⚙️ **[Architecture Analysis & Improvements](docs/ARCHITECTURE_ANALYSIS_AND_IMPROVEMENTS.md)** - Current gaps and roadmap
 - 📈 **[Performance & Scalability](docs/PERFORMANCE_AND_SCALABILITY.md)** - Performance bottlenecks and plans
 - 📈 **[Scalability Improvements](docs/SCALABILITY_IMPROVEMENTS.md)** - Scaling strategies
 - 🧭 **[Domain Events & GraphQL](docs/DOMAIN_EVENTS_AND_GRAPHQL.md)** - Event flow and real-time notifications
 - 🧾 **[Performance Improvements (Aligned)](docs/TODO_PERFORMANCE_IMPROVEMENTS.md)** - Status-aligned implementation checklist
 - 💸 **[Cost Optimization](docs/COST_OPTIMIZATION.md)** - AWS cost-saving strategies
 - 🚢 **[Deployment Optimization](docs/DEPLOYMENT_OPTIMIZATION.md)** - Blue/green (available), canary (future), multi-region (optional)

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
