# Distributed Tracing (OpenTelemetry + ADOT Collector with AWS X-Ray Export)

This document describes the distributed tracing implementation using OpenTelemetry in the Live Event Service. Traces are exported via OTLP to the AWS Distro for OpenTelemetry (ADOT) Collector, which sends them to AWS X-Ray.

## Overview

The Live Event Service implements comprehensive distributed tracing using AWS X-Ray to provide visibility into request flows, performance bottlenecks, and system dependencies.

## Current Status

- ✅ OpenTelemetry SDK integrated for ASP.NET Core and HttpClient
- ✅ Metrics: Prometheus endpoint exposed for scraping
- ✅ Tracing: OTLP exporter configured (endpoint provided via environment, e.g. ADOT Collector)
- ✅ Local development: Prometheus metrics available; traces sent to local/remote collector when configured

## Program.cs Integration

OpenTelemetry is configured in startup for metrics and tracing:

```csharp
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddProcessInstrumentation()
            .AddPrometheusExporter();
    })
    .WithTracing(tracing =>
    {
        tracing
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("LiveEventService"))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter();
    });
```

## Configuration Settings (Environment)

Typical environment variables for ADOT Collector + OTLP:

```bash
OTEL_SERVICE_NAME=LiveEventService
OTEL_EXPORTER_OTLP_ENDPOINT=http://adot-collector:4317
OTEL_RESOURCE_ATTRIBUTES=service.namespace=live-event-service,service.version=1.0.0
```

## Required IAM Permissions (Collector to X-Ray)

For production deployment, ensure the ADOT Collector task/role has these permissions:

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

OpenTelemetry automatically traces:

1. **HTTP Requests** (ASP.NET Core)
2. **HttpClient** outbound requests
3. You can add EF Core tracing via community instrumentation if needed

### Manual Instrumentation

For custom spans, use OpenTelemetry API:

```csharp
using OpenTelemetry.Trace;
using System.Diagnostics;

var activitySource = new ActivitySource("LiveEventService.Business");
using var activity = activitySource.StartActivity("CustomOperation");
// business logic
```

### Adding Custom Attributes (OpenTelemetry)

```csharp
using System.Diagnostics;

var activitySource = new ActivitySource("LiveEventService.Business");
using var activity = activitySource.StartActivity("ProcessRegistration");
activity?.SetTag("user.id", userId);
activity?.SetTag("event.id", eventId);
activity?.SetTag("registration.status", "confirmed");
```

### HTTP Client Instrumentation

HttpClient is instrumented via OpenTelemetry’s HttpClient instrumentation automatically.

## Verifying Tracing is Working

### Check Application Logs

X-Ray generates trace and span IDs that appear in your application logs:

```bash
# View recent API logs
docker logs liveevent-api --tail 50

# Look for OTLP exporter or trace activity
docker logs liveevent-api 2>&1 | grep -i otlp
```

Expected log output showing X-Ray traces:
```
2024-01-20T10:30:45.123Z [INF] HTTP GET /health responded 200 in 45.2ms (Root=1-65a2b4d5-1234567890abcdef; Parent=abcdef1234567890; Sampled=1)
```

### Check ADOT Collector

```bash
# Verify Collector health
curl http://adot-collector:13133/healthz

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
# Check OTLP exporter activity
docker logs liveevent-api --tail 50 | grep -i otlp

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
# Check OTEL variables are set
docker exec liveevent-api printenv | grep OTEL
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
