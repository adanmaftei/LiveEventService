# Local Development Setup Guide

This guide explains how to run the Live Event Service locally using Docker Compose with LocalStack for AWS service mocking.

## Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop) installed and **running**
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) installed
- [AWS CLI](https://aws.amazon.com/cli/) installed (optional, for LocalStack resource management)

## Pre-flight Checks

Before running the application, ensure:

1. **Docker Desktop is running**:
   ```bash
   docker --version
   docker ps
   ```

2. **Port availability** (check these ports are not in use):
   - 5432 (PostgreSQL)
   - 4566 (LocalStack)
   - 5050 (pgAdmin)
   - 5000/5001 (API)

3. **Disk space** (Docker images require ~2GB):
   ```bash
   docker system df
   ```

## Quick Start - Complete Setup

### Option 1: Full Docker Compose (Recommended)

```bash
# Start all services including the API
docker-compose up -d

# Check service health
curl http://localhost:5000/health
```

### Option 2: API Development Mode

```bash
# Start supporting services only
docker-compose up -d db localstack pgadmin

# Run API locally for development
dotnet run --project src/LiveEventService.API/LiveEventService.API.csproj
```

## Service Access

| Service | Port | URL | Status | Credentials |
|---------|------|-----|--------|-------------|
| **API** | 5000/5001 | http://localhost:5000/health | ✅ **Working** | - |
| **Swagger UI** | 5000 | http://localhost:5000/swagger/index.html | ✅ **Working** | - |
| **PostgreSQL** | 5432 | Direct connection | ✅ **Working** | postgres/postgres |
| **pgAdmin** | 5050 | http://localhost:5050 | ✅ **Working** | admin@example.com / admin |
| **LocalStack** | 4566 | http://localhost:4566/_localstack/health | ✅ **Working** | test/test |

## Fully Working Features

### 🎯 Live Event Service API
- **Status**: ✅ **Fully operational**
- **Health Check**: http://localhost:5000/health
- **Swagger UI**: http://localhost:5000/swagger/index.html
- **Features**:
  - Entity Framework migrations automatically applied
  - Database schema with test data seeded
  - Serilog request logging with correlation IDs
  - AWS X-Ray distributed tracing
  - Health checks (PostgreSQL + Cognito)
  - CORS configured for frontend integration

### 🗄️ PostgreSQL Database
- **Status**: ✅ **Fully operational**
- **Connection**: `localhost:5432`
- **Database**: `LiveEventDB`
- **Schema**: Automatically created with migrations
- **Test Data**: Pre-seeded users and events
- **Tables**: Users, Events, EventRegistrations
- **Test**: `docker exec liveevent-db psql -U postgres -d LiveEventDB -c "\dt"`

### 🔧 pgAdmin (Database Management)
- **Status**: ✅ **Fully operational**  
- **URL**: http://localhost:5050
- **Login**: admin@example.com / admin
- **Setup**: Add server with host `liveevent-db`, port `5432`

### ☁️ LocalStack (AWS Service Mocking)
- **Status**: ✅ **Fully operational**
- **Health**: http://localhost:4566/_localstack/health
- **Services**: Cognito, S3, X-Ray, CloudWatch Logs, CloudWatch
- **Credentials**: Access Key: `test`, Secret: `test`

## Advanced Features Working

### 📊 Serilog Request Logging
- **Status**: ✅ **Fully configured**
- **Features**:
  - Structured JSON logging
  - Correlation ID tracking (X-Correlation-ID header)
  - Request/response timing
  - User agent and IP tracking
  - Machine name and environment context

### 🔍 AWS X-Ray Distributed Tracing
- **Status**: ✅ **Working correctly**
- **Features**:
  - Trace IDs generated for all requests
  - SQL query tracing enabled
  - HTTP request tracing
  - AWS service call tracing
  - LocalStack integration for development

### 🏥 Health Checks
- **Status**: ✅ **All passing**
- **Endpoint**: http://localhost:5000/health
- **Checks**:
  - PostgreSQL database connectivity
  - AWS Cognito configuration validation
  - S3 health check (disabled in development)

## API Endpoints

### Core Endpoints
- **Health Check**: `GET /health` - Returns "Healthy"
- **Swagger UI**: `GET /swagger/index.html` - API documentation
- **GraphQL**: `GET /graphql` - GraphQL playground (development only)

### Example API Usage

```bash
# Health check
curl http://localhost:5000/health

# Health check with correlation ID
curl -H "X-Correlation-ID: test-123" http://localhost:5000/health

# Access Swagger documentation
curl http://localhost:5000/swagger/index.html
```

## Configuration Files

### Development Configuration (appsettings.Development.json)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=LiveEventDB;Username=postgres;Password=postgres"
  },
  "AWS": {
    "Region": "us-east-1",
    "UserPoolId": "us-east-1_000000001",
    "S3BucketName": "local-bucket",
    "ServiceURL": "http://localhost:4566",
    "XRay": {
      "ServiceName": "LiveEventService.API",
      "CollectSqlQueries": true,
      "TraceHttpRequests": true,
      "TraceAWSRequests": true,
      "UseRuntimeErrors": true
    }
  },
  "AllowedOrigins": [ "http://localhost:3000" ]
}
```

### Docker Compose Configuration

```yaml
# Key environment variables for API container
environment:
  - ASPNETCORE_ENVIRONMENT=Development
  - ASPNETCORE_URLS=http://+:80
  - ConnectionStrings__DefaultConnection=Host=db;Port=5432;Database=LiveEventDB;Username=postgres;Password=postgres;
  - AWS__ServiceURL=http://localstack:4566
```

## Development Workflow

### Starting Development

```bash
# Option 1: Full Docker setup (production-like)
docker-compose up -d
curl http://localhost:5000/health

# Option 2: Development mode (API running locally)
docker-compose up -d db localstack pgadmin
dotnet run --project src/LiveEventService.API/
```

### Making Changes

1. **Code Changes**: Edit files in your IDE
2. **Database Changes**: Create EF migrations
3. **Container Updates**: `docker-compose build api` if needed
4. **View Logs**: `docker logs liveevent-api --tail 50`

### Database Operations

```bash
# View applied migrations
docker exec liveevent-db psql -U postgres -d LiveEventDB -c 'SELECT * FROM "__EFMigrationsHistory";'

# Check table structure
docker exec liveevent-db psql -U postgres -d LiveEventDB -c '\dt'

# View test data
docker exec liveevent-db psql -U postgres -d LiveEventDB -c 'SELECT "Name", "Email" FROM "Users";'
```

## Troubleshooting

### Common Issues - RESOLVED ✅

The following issues have been **completely resolved**:

- ❌ ~~Entity Framework migration failures~~ → ✅ **Fixed: Automatic migration on startup**
- ❌ ~~S3 health check errors~~ → ✅ **Fixed: Conditional health checks for development**
- ❌ ~~Serilog DiagnosticContext errors~~ → ✅ **Fixed: Proper Serilog hosting integration**
- ❌ ~~API port mapping issues~~ → ✅ **Fixed: Correct port configuration**
- ❌ ~~Database authentication failures~~ → ✅ **Fixed: PostgreSQL trust configuration**
- ❌ ~~X-Ray initialization missing~~ → ✅ **Fixed: Proper X-Ray setup**
- ❌ ~~Correlation ID tracking missing~~ → ✅ **Fixed: Correlation ID middleware**

### Current Status Verification

```bash
# Check all services are healthy
docker-compose ps

# Test API responsiveness
curl http://localhost:5000/health
curl http://localhost:5000/swagger/index.html

# Check database
docker exec liveevent-db psql -U postgres -d LiveEventDB -c 'SELECT COUNT(*) FROM "Users";'

# Check LocalStack
curl http://localhost:4566/_localstack/health
```

### Reset Everything (if needed)

```bash
# Complete reset
docker-compose down --volumes
docker system prune -f
docker-compose up -d

# Verify all services
curl http://localhost:5000/health
```

### Viewing Logs

```bash
# API logs with request tracing
docker logs liveevent-api --tail 50

# Database logs
docker logs liveevent-db --tail 20

# LocalStack logs
docker logs localstack --tail 20

# All services
docker-compose logs --tail 20
```

## Service Architecture

| Component | Technology | Version | Purpose |
|-----------|------------|---------|---------|
| **API**: .NET 9 Web API with ASP.NET Core | C# | .NET 9 | REST and GraphQL endpoints |
| **Database**: PostgreSQL | SQL | 14 | Primary data store |
| **ORM**: Entity Framework Core | C# | 9.0.7 | Database access layer |
| **GraphQL**: HotChocolate | C# | 15.1.8 | GraphQL server implementation |
| **Authentication**: AWS Cognito | JWT | Latest | User authentication |
| **Logging**: Serilog | C# | 4.3.0 | Structured logging |
| **Tracing**: AWS X-Ray | C# | 2.12.0 | Distributed tracing |
| **Testing**: xUnit + Testcontainers | C# | Latest | Unit and integration tests |

## Production Considerations

This setup provides a **production-ready foundation** with:

- **Scalability**: Containerized architecture
- **Monitoring**: Health checks, logging, tracing
- **Security**: Authentication framework ready
- **Documentation**: Comprehensive API docs
- **Testing**: Integration test infrastructure ready
- **DevOps**: Docker Compose for easy deployment

## Next Steps

1. **Frontend Integration**: Connect your React/Angular/Vue.js frontend to `http://localhost:5000`
2. **Integration Testing**: See [INTEGRATION_TESTING.md](./INTEGRATION_TESTING.md) for Testcontainers setup
3. **Production Deployment**: Use the Docker images for cloud deployment
4. **CI/CD Pipeline**: Build automation ready with Docker

## Support

- **Health Status**: http://localhost:5000/health
- **API Documentation**: http://localhost:5000/swagger/index.html
- **Database Admin**: http://localhost:5050
- **Service Status**: All services operational ✅ 