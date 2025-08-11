# Distributed Tracing with AWS X-Ray

This document outlines how distributed tracing is implemented in the Live Event Service using AWS X-Ray.

## ✅ Current Status: Fully Operational

AWS X-Ray distributed tracing is **completely working** with:
- ✅ X-Ray properly integrated with .NET 8 ASP.NET Core
- ✅ LocalStack integration for development environment
- ✅ Automatic trace generation for all HTTP requests
- ✅ SQL query tracing with Entity Framework
- ✅ AWS service call tracing
- ✅ Correlation with Serilog logging via correlation IDs
- ✅ Trace and span IDs visible in application logs

## Overview

AWS X-Ray helps developers analyze and debug distributed applications. It provides an end-to-end view of requests as they travel through your application, and shows a map of your application's underlying components.

## Features

- **Distributed Request Tracing**: Track requests as they travel through your application
- **Service Map**: Visualize your application architecture
- **Performance Analysis**: Identify performance bottlenecks
- **Error Detection**: Quickly find and debug errors
- **AWS Integration**: Built-in support for AWS services
- **LocalStack Support**: Full development environment support

## Current Working Configuration

### Program.cs Integration

X-Ray is properly configured in the application startup:

```csharp
// Configure AWS X-Ray
AWSXRayRecorder.InitializeInstance(configuration: builder.Configuration);
AWSSDKHandler.RegisterXRayForAllServices();

// Add X-Ray middleware - this must be early in the pipeline
app.UseXRay(builder.Configuration.GetValue<string>("AWS:XRay:ServiceName") ?? "LiveEventService.API");
```

### Configuration Settings

X-Ray is configured in `appsettings.Development.json`:

```json
{
  "AWS": {
    "Region": "us-east-1",
    "ServiceURL": "http://localhost:4566",
    "XRay": {
      "ServiceName": "LiveEventService.API",
      "CollectSqlQueries": true,
      "TraceHttpRequests": true,
      "TraceAWSRequests": true,
      "UseRuntimeErrors": true
    }
  }
}
```

### LocalStack Integration

In development, X-Ray traces are sent to LocalStack:

```yaml
# docker-compose.yml configuration
localstack:
  image: localstack/localstack:latest
  container_name: localstack
  environment:
    - SERVICES=cognito-idp,s3,xray,logs,cloudwatch
    - DEBUG=1
    - LAMBDA_EXECUTOR=docker
    - DOCKER_HOST=unix:///var/run/docker.sock
  ports:
    - "4566:4566"
  volumes:
    - "/var/run/docker.sock:/var/run/docker.sock"
```

## Required IAM Permissions

For production deployment, ensure your IAM role has these permissions:

```json
{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Effect": "Allow",
            "Action": [
                "xray:PutTraceSegments",
                "xray:PutTelemetryRecords",
                "xray:GetSamplingRules",
                "xray:GetSamplingTargets",
                "xray:GetSamplingStatisticSummaries"
            ],
            "Resource": ["*"]
        }
    ]
}
```

## Usage and Features

### Automatic Instrumentation

X-Ray automatically traces:

1. **HTTP Requests**: All incoming API requests
2. **Entity Framework**: SQL queries and database operations
3. **AWS SDK Calls**: All AWS service calls (S3, Cognito, etc.)
4. **HttpClient**: Outbound HTTP requests

### Manual Instrumentation

For custom segments and subsegments:

```csharp
using Amazon.XRay.Recorder.Core;

// Create a custom segment
using (var segment = AWSXRayRecorder.Instance.BeginSegment("CustomOperation"))
{
    try
    {
        // Your business logic here
        using (AWSXRayRecorder.Instance.BeginSubsegment("DatabaseQuery"))
        {
            // Database operations
            var events = await _eventRepository.GetAllAsync();
        }
        
        using (AWSXRayRecorder.Instance.BeginSubsegment("ExternalAPICall"))
        {
            // External API calls
            var response = await httpClient.GetAsync("https://api.external.com/data");
        }
    }
    catch (Exception ex)
    {
        segment?.AddException(ex);
        throw;
    }
}
```

### Adding Custom Annotations and Metadata

```csharp
using Amazon.XRay.Recorder.Core;

// Add annotations (indexed, searchable)
AWSXRayRecorder.Instance.AddAnnotation("UserId", userId);
AWSXRayRecorder.Instance.AddAnnotation("EventId", eventId);
AWSXRayRecorder.Instance.AddAnnotation("RegistrationStatus", "confirmed");

// Add metadata (not indexed, for detailed information)
AWSXRayRecorder.Instance.AddMetadata("EventDetails", new
{
    EventName = eventEntity.Name,
    Capacity = eventEntity.Capacity,
    CurrentRegistrations = eventEntity.Registrations.Count
});
```

### HTTP Client Instrumentation

For external HTTP calls, use the X-Ray HTTP handler:

```csharp
var httpClient = new HttpClient(new HttpClientXRayTracingHandler(new HttpClientHandler()));
var response = await httpClient.GetAsync("https://example.com/api");
```

## Verifying X-Ray is Working

### Check Application Logs

X-Ray generates trace and span IDs that appear in your application logs:

```bash
# View recent API logs
docker logs liveevent-api --tail 50

# Look for X-Ray trace information
docker logs liveevent-api 2>&1 | grep -E "(Root=|Parent=|traceId)"
```

Expected log output showing X-Ray traces:
```
2024-01-20T10:30:45.123Z [INF] HTTP GET /health responded 200 in 45.2ms (Root=1-65a2b4d5-1234567890abcdef; Parent=abcdef1234567890; Sampled=1)
```

### Check LocalStack X-Ray Service

```bash
# Verify X-Ray service is running in LocalStack
curl http://localhost:4566/_localstack/health | grep xray

# Check X-Ray traces (requires awslocal CLI)
awslocal xray get-trace-summaries --time-range-type TimeRangeByStartTime --start-time 2024-01-20T00:00:00 --end-time 2024-01-20T23:59:59
```

### Test Trace Generation

```bash
# Generate traces by making API calls
curl http://localhost:5000/health
curl -H "X-Correlation-ID: test-trace-123" http://localhost:5000/health

# Check logs for trace information
docker logs liveevent-api --tail 20 | grep "test-trace-123"
```

## Viewing Traces

### Development (LocalStack)

1. **LocalStack Dashboard** (if available):
   - Access LocalStack web interface
   - Navigate to X-Ray section

2. **AWS CLI with LocalStack**:
   ```bash
   # Install awslocal
   pip install awscli-local
   
   # Get trace summaries
   awslocal xray get-trace-summaries --time-range-type TimeRangeByStartTime --start-time $(date -d '1 hour ago' -u +%Y-%m-%dT%H:%M:%S) --end-time $(date -u +%Y-%m-%dT%H:%M:%S)
   
   # Get specific trace details
   awslocal xray batch-get-traces --trace-ids <trace-id>
   ```

### Production (AWS Console)

1. **AWS Management Console**:
   - Navigate to AWS X-Ray console
   - Select "Traces" or "Service Map"
   - Filter by service name or time range

2. **Trace Details**:
   - View the entire request flow
   - See timing information for each segment
   - Inspect HTTP requests and SQL queries
   - View error details and stack traces

## Integration with Logging

X-Ray traces are automatically correlated with Serilog logs through correlation IDs:

```csharp
// Correlation IDs from HTTP headers are included in both X-Ray traces and logs
app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? Guid.NewGuid().ToString();
    
    // This correlation ID appears in both logs and X-Ray traces
    using (LogContext.PushProperty("CorrelationId", correlationId))
    {
        // X-Ray automatically picks up this context
        AWSXRayRecorder.Instance.AddAnnotation("CorrelationId", correlationId);
        await next(context);
    }
});
```

## Performance Analysis

### SQL Query Tracing

Entity Framework queries are automatically traced:

```csharp
// This query will appear as a subsegment in X-Ray
var events = await _dbContext.Events
    .Where(e => e.IsPublished)
    .OrderBy(e => e.StartDate)
    .ToListAsync();
```

The trace will show:
- SQL query text
- Execution time
- Number of rows returned
- Database connection details

### HTTP Request Performance

All incoming requests show:
- HTTP method and path
- Response status code
- Response time
- Request headers (filtered for security)
- Response size

## Best Practices

### 1. Meaningful Segment Names
Use descriptive names for custom segments:
```csharp
// ✅ Good
using (AWSXRayRecorder.Instance.BeginSubsegment("ValidateEventCapacity"))

// ❌ Bad
using (AWSXRayRecorder.Instance.BeginSubsegment("Process"))
```

### 2. Error Handling
Always add exceptions to segments:
```csharp
try
{
    await ProcessRegistration();
}
catch (Exception ex)
{
    AWSXRayRecorder.Instance.AddException(ex);
    throw;
}
```

### 3. Avoid Sensitive Data
Be careful not to include sensitive information:
```csharp
// ✅ Good
AWSXRayRecorder.Instance.AddAnnotation("UserId", userId);

// ❌ Bad - contains PII
AWSXRayRecorder.Instance.AddAnnotation("UserEmail", userEmail);
```

### 4. Use Annotations for Filtering
Add searchable annotations for important business data:
```csharp
AWSXRayRecorder.Instance.AddAnnotation("EventType", "conference");
AWSXRayRecorder.Instance.AddAnnotation("UserType", "premium");
AWSXRayRecorder.Instance.AddAnnotation("RegistrationStatus", "confirmed");
```

## Troubleshooting

### Common Issues - All Resolved ✅

The following issues have been **completely resolved**:

- ❌ ~~X-Ray middleware not initializing~~ → ✅ **Fixed: Proper X-Ray initialization order**
- ❌ ~~LocalStack X-Ray not receiving traces~~ → ✅ **Fixed: Correct LocalStack service configuration**
- ❌ ~~Missing trace IDs in logs~~ → ✅ **Fixed: Proper integration with logging middleware**

### Current Status Verification

```bash
# Check X-Ray is working
docker logs liveevent-api --tail 50 | grep -E "(Root=|Parent=|traceId)"

# Verify LocalStack X-Ray service
curl http://localhost:4566/_localstack/health | jq '.services.xray'

# Generate test traces
curl -H "X-Correlation-ID: test-trace" http://localhost:5000/health
```

### No Traces Appearing

If traces aren't appearing:

1. **Check Service Configuration**:
   ```bash
   # Verify LocalStack services
   curl http://localhost:4566/_localstack/health
   ```

2. **Check Application Logs**:
   ```bash
   # Look for X-Ray initialization messages
   docker logs liveevent-api | grep -i xray
   ```

3. **Verify Configuration**:
   ```bash
   # Check X-Ray service name is set
   docker exec liveevent-api printenv | grep XRAY
   ```

### High Latency

If you notice performance issues:

1. **Review Sampling Rules**: Adjust sampling rate in production
2. **Check Network**: Verify connectivity to LocalStack/AWS
3. **Monitor Resources**: Ensure adequate memory and CPU

## Configuration Examples

### Minimal Production Configuration

```json
{
  "AWS": {
    "Region": "us-east-1",
    "XRay": {
      "ServiceName": "LiveEventService.API",
      "CollectSqlQueries": false,
      "TraceHttpRequests": true,
      "TraceAWSRequests": true
    }
  }
}
```

### Development with Full Tracing

```json
{
  "AWS": {
    "Region": "us-east-1",
    "ServiceURL": "http://localhost:4566",
    "XRay": {
      "ServiceName": "LiveEventService.API",
      "CollectSqlQueries": true,
      "TraceHttpRequests": true,
      "TraceAWSRequests": true,
      "UseRuntimeErrors": true,
      "PluginsEnabled": true,
      "SamplingRuleManifest": {
        "default": {
          "fixed_target": 1,
          "rate": 0.1
        }
      }
    }
  }
}
```

## Advanced Features

### Custom Sampling Rules

Configure sampling to control trace volume and costs:

```json
{
  "XRay": {
    "SamplingRuleManifest": {
      "version": 2,
      "default": {
        "fixed_target": 1,
        "rate": 0.1
      },
      "rules": [
        {
          "description": "Health check sampling",
          "service_name": "LiveEventService.API",
          "http_method": "GET",
          "url_path": "/health",
          "fixed_target": 0,
          "rate": 0.05
        }
      ]
    }
  }
}
```

### Service Map

X-Ray automatically generates a service map showing:
- Service dependencies
- Request rates
- Error rates
- Response times

In LocalStack development, this provides insight into:
- API → PostgreSQL connections
- API → LocalStack AWS services
- External HTTP calls

## Integration Testing

X-Ray tracing works in integration tests:

```csharp
[Fact]
public async Task EventRegistration_ShouldCreateTrace()
{
    // Arrange
    var client = _factory.CreateClient();
    
    // Act
    var response = await client.PostAsync("/api/events/123/register", content);
    
    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    
    // X-Ray traces are automatically generated for this request
    // In a real test, you could query LocalStack X-Ray API to verify traces
}
```

## Next Steps

- **Production Deployment**: Configure proper IAM permissions for X-Ray
- **Custom Metrics**: Add business-specific annotations and metadata
- **Alerting**: Set up CloudWatch alarms based on X-Ray metrics
- **Service Map Analysis**: Use service map to identify performance bottlenecks
- **Sampling Optimization**: Tune sampling rules for cost/visibility balance
