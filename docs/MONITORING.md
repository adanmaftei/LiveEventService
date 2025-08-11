# Monitoring and Health Checks Guide

This document outlines the monitoring and health check implementation for the Live Event Service.

## ✅ Current Status: Fully Operational

The monitoring and health check system is **completely working** with:
- ✅ Comprehensive health checks for all critical services
- ✅ Real-time health status reporting via `/health` endpoint
- ✅ PostgreSQL database connectivity monitoring
- ✅ AWS Cognito configuration validation
- ✅ S3 health check (conditionally enabled for production)
- ✅ Structured logging integration for health events
- ✅ X-Ray distributed tracing for health check requests

## Overview

The monitoring solution provides:
- Real-time health status via HTTP endpoint
- Comprehensive service dependency checks
- Performance metrics and response timing
- Integration with structured logging and tracing
- Production-ready CloudWatch integration capability

## Health Check Endpoint

### Endpoint Details
```
GET /health
HTTP/1.1 200 OK
Content-Type: application/json
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
      "data": {},
      "description": null,
      "exception": null,
      "tags": ["db", "rds", "postgresql"]
    },
    "AWS Cognito": {
      "status": "Healthy",
      "duration": "00:00:00.0001234",
      "data": {},
      "description": null,
      "exception": null,
      "tags": ["aws", "cognito"]
    }
  }
}
```

### Health Check Components

#### 1. PostgreSQL (RDS) Health Check
- **Purpose**: Validates database connectivity and responsiveness
- **Implementation**: Direct connection test using Npgsql
- **Configuration**: Uses `DefaultConnection` connection string
- **Tags**: `["db", "rds", "postgresql"]`

```csharp
// Configuration in Program.cs
healthChecksBuilder.AddNpgSql(
    defaultConnection,
    name: "PostgreSQL (RDS)",
    tags: new[] { "db", "rds", "postgresql" });
```

#### 2. AWS Cognito Health Check
- **Purpose**: Validates AWS Cognito configuration
- **Implementation**: Configuration presence validation
- **Checks**: Region and UserPoolId configuration
- **Tags**: `["aws", "cognito"]`

```csharp
// Configuration in Program.cs
healthChecksBuilder.AddCheck("AWS Cognito", () =>
{
    var region = builder.Configuration["AWS:Region"]!;
    var userPoolId = builder.Configuration["AWS:UserPoolId"]!;
    return !string.IsNullOrEmpty(region) && !string.IsNullOrEmpty(userPoolId)
        ? HealthCheckResult.Healthy()
        : HealthCheckResult.Unhealthy("Cognito config missing");
}, tags: new[] { "aws", "cognito" });
```

#### 3. S3 Health Check (Production Only)
- **Purpose**: Validates S3 bucket accessibility
- **Implementation**: Conditionally enabled for production environments
- **Development**: Disabled due to LocalStack compatibility requirements
- **Tags**: `["aws", "s3"]`

```csharp
// Conditional configuration for production
if (!builder.Environment.IsDevelopment())
{
    healthChecksBuilder.AddS3(options =>
    {
        options.BucketName = builder.Configuration["AWS:S3BucketName"] ?? string.Empty;
    },
    name: "AWS S3",
    tags: new[] { "aws", "s3" });
}
```

## Health Check Usage

### Testing Health Checks

```bash
# Basic health check
curl http://localhost:5000/health

# Health check with correlation ID for tracing
curl -H "X-Correlation-ID: health-test-123" http://localhost:5000/health

# Pretty print JSON response
curl -s http://localhost:5000/health | jq

# Check specific timing
curl -w "Total time: %{time_total}s\n" -s http://localhost:5000/health > /dev/null
```

### Expected Responses

#### Healthy Response
```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0234567",
  "entries": {
    "PostgreSQL (RDS)": {
      "status": "Healthy",
      "duration": "00:00:00.0123456"
    },
    "AWS Cognito": {
      "status": "Healthy",
      "duration": "00:00:00.0001234"
    }
  }
}
```

#### Unhealthy Response
```json
{
  "status": "Unhealthy",
  "totalDuration": "00:00:00.1234567",
  "entries": {
    "PostgreSQL (RDS)": {
      "status": "Unhealthy",
      "duration": "00:00:00.1000000",
      "exception": "Npgsql.PostgresException: Connection refused",
      "description": "Database connection failed"
    },
    "AWS Cognito": {
      "status": "Healthy",
      "duration": "00:00:00.0001234"
    }
  }
}
```

## Integration with Observability

### Serilog Integration
Health check requests are automatically logged with:
- Request path and method
- Response status and timing
- Correlation ID for tracing
- Health check results

```bash
# View health check logs
docker logs liveevent-api 2>&1 | grep "GET /health"
```

### X-Ray Tracing
Health check requests generate X-Ray traces including:
- Overall request timing
- Individual health check execution
- Database query performance
- AWS service call timing

```bash
# View health check traces
docker logs liveevent-api 2>&1 | grep -E "(GET /health|Root=|Parent=)"
```

## Monitoring in Development

### Docker Environment Monitoring

```bash
# Check all container health
docker-compose ps

# Monitor API container specifically
docker logs liveevent-api --tail 50 --follow

# Check database connectivity
docker exec liveevent-db psql -U postgres -d LiveEventDB -c "SELECT 1;"

# Verify LocalStack services
curl http://localhost:4566/_localstack/health
```

### Health Check Automation

```bash
# Continuous health monitoring
while true; do
  status=$(curl -s http://localhost:5000/health | jq -r '.status')
  echo "$(date): Health Status = $status"
  sleep 30
done

# Health check with alerting
if ! curl -f -s http://localhost:5000/health > /dev/null; then
  echo "ALERT: Health check failed!"
  # Add notification logic here
fi
```

## Production Monitoring with CloudWatch

### Dashboards

#### API Health Dashboard
- **Health Check Success Rate**: Percentage of successful health checks
- **Health Check Response Time**: p50, p95, p99 response times
- **Individual Service Health**: Status of PostgreSQL, Cognito, S3
- **Error Rate**: Failed health checks over time

#### System Performance Dashboard
- **Request Count**: Total API requests per minute
- **Response Times**: API endpoint performance
- **Error Rates**: 4XX and 5XX error percentages
- **Database Performance**: Connection count, query timing

### CloudWatch Metrics

#### Custom Health Metrics
```csharp
// Example: Publishing custom health metrics
var client = new AmazonCloudWatchClient();
await client.PutMetricDataAsync(new PutMetricDataRequest
{
    Namespace = "LiveEventService/Health",
    MetricData = new List<MetricDatum>
    {
        new MetricDatum
        {
            MetricName = "HealthCheckDuration",
            Value = healthCheckDuration.TotalMilliseconds,
            Unit = StandardUnit.Milliseconds,
            TimestampUtc = DateTime.UtcNow,
            Dimensions = new List<Dimension>
            {
                new Dimension { Name = "ServiceName", Value = "PostgreSQL" }
            }
        }
    }
});
```

#### Built-in Metrics
- `AspNetCore.Requests.Count`: Total request count
- `AspNetCore.Requests.Duration`: Request duration percentiles
- `AspNetCore.Requests.Rate`: Requests per second
- `Health.Check.Duration`: Health check execution time

## Alerting

### Critical Alerts (P0)
- **Health Check Failure**: Any health check reports unhealthy for > 2 minutes
- **Database Connectivity**: PostgreSQL health check fails
- **High Response Time**: Health check takes > 5 seconds

### Warning Alerts (P1)
- **Slow Health Checks**: Health check duration > 1 second
- **Intermittent Failures**: Health check success rate < 95% over 15 minutes

### Alert Configuration

#### CloudWatch Alarms
```json
{
  "AlarmName": "LiveEventService-HealthCheck-Failure",
  "MetricName": "HealthCheck",
  "Namespace": "LiveEventService/Health",
  "Statistic": "Average",
  "Period": 60,
  "EvaluationPeriods": 2,
  "Threshold": 1,
  "ComparisonOperator": "LessThanThreshold",
  "TreatMissingData": "breaching"
}
```

#### SNS Integration
```bash
# Configure SNS topic for alerts
aws sns create-topic --name LiveEventService-Alerts

# Subscribe email to alerts
aws sns subscribe \
  --topic-arn arn:aws:sns:us-east-1:123456789012:LiveEventService-Alerts \
  --protocol email \
  --notification-endpoint admin@yourcompany.com
```

## Health Check Best Practices

### Implementation Guidelines
1. **Fast Execution**: Keep health checks under 1 second
2. **Dependency Testing**: Test actual connectivity, not just configuration
3. **Graceful Degradation**: Continue serving if non-critical services fail
4. **Detailed Responses**: Include timing and error details for debugging

### Configuration Best Practices
1. **Environment-Specific**: Different checks for dev/staging/production
2. **Timeout Configuration**: Set appropriate timeouts for each check
3. **Retry Logic**: Handle transient failures gracefully
4. **Security**: Don't expose sensitive information in health responses

## Troubleshooting

### Common Issues - All Resolved ✅

The following issues have been **completely resolved**:

- ❌ ~~S3 health check failures in development~~ → ✅ **Fixed: Conditional S3 health checks**
- ❌ ~~Database connectivity timeouts~~ → ✅ **Fixed: Proper connection string configuration**
- ❌ ~~Missing health check middleware~~ → ✅ **Fixed: Health checks properly configured**

### Debugging Health Check Issues

```bash
# Check individual service health
docker exec liveevent-db psql -U postgres -d LiveEventDB -c "SELECT version();"

# Verify configuration
docker exec liveevent-api printenv | grep -E "(CONNECTION|AWS)"

# Test connectivity manually
docker exec liveevent-api curl -s http://localhost:5000/health
```

### Health Check Failure Analysis

```bash
# View detailed logs
docker logs liveevent-api --tail 100 | grep -i health

# Check specific health check timing
curl -w "@curl-format.txt" -s http://localhost:5000/health

# Monitor health over time
watch -n 5 'curl -s http://localhost:5000/health | jq ".status"'
```

## Performance Considerations

### Health Check Optimization
- **Connection Pooling**: Reuse database connections for health checks
- **Caching**: Cache health check results for short periods if needed
- **Parallel Execution**: Health checks run in parallel for better performance
- **Circuit Breaker**: Implement circuit breaker pattern for external dependencies

### Resource Usage
- **Memory**: Health checks have minimal memory footprint
- **CPU**: Designed for low CPU overhead
- **Network**: Minimal network traffic for checks
- **Database**: Uses lightweight queries for connectivity testing

## Integration Testing

### Health Check Testing
```csharp
[Fact]
public async Task HealthCheck_ShouldReturnHealthy()
{
    // Arrange
    var client = _factory.CreateClient();
    
    // Act
    var response = await client.GetAsync("/health");
    
    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var content = await response.Content.ReadAsStringAsync();
    var healthResult = JsonSerializer.Deserialize<HealthCheckResult>(content);
    healthResult.Status.Should().Be("Healthy");
}
```

### Automated Health Monitoring
```bash
#!/bin/bash
# health-monitor.sh
while true; do
  if ! curl -f -s http://localhost:5000/health > /dev/null; then
    echo "$(date): HEALTH CHECK FAILED"
    docker logs liveevent-api --tail 20
  else
    echo "$(date): Health check passed"
  fi
  sleep 60
done
```

## Next Steps

### Development Environment
- **Custom Health Checks**: Add business-specific health indicators
- **Health Dashboard**: Create visual health monitoring dashboard
- **Load Testing**: Verify health check performance under load

### Production Environment
- **CloudWatch Integration**: Full metrics and alerting setup
- **Application Insights**: Enhanced application performance monitoring
- **Synthetic Monitoring**: External health check monitoring
- **SLA Monitoring**: Track and alert on SLA compliance

## Support Resources

- **Health Status**: http://localhost:5000/health
- **Application Logs**: `docker logs liveevent-api`
- **Database Health**: Direct PostgreSQL connection testing
- **LocalStack Status**: http://localhost:4566/_localstack/health
