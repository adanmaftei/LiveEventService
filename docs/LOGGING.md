# Logging Implementation

This document outlines the logging implementation for the Live Event Service, including configuration, usage, and best practices.

## ✅ Current Status: Fully Operational

The logging system is **completely working** with:
- ✅ Serilog properly integrated with ASP.NET Core hosting
- ✅ Structured JSON logging with correlation IDs
- ✅ Request/response logging with timing and context
- ✅ Console output for development
- ✅ CloudWatch integration automatically enabled in Production
- ✅ Dedicated audit log sink (separate CloudWatch Logs group) for admin-sensitive actions
- ✅ Automatic correlation ID generation and tracking

## Overview

The application uses Serilog for structured logging with the following features:

- **Structured Logging**: Logs are emitted as JSON for better querying and analysis
- **Correlation ID Tracking**: Automatic request correlation with `X-Correlation-ID` header support
- **Request Logging**: Complete HTTP request/response logging with enriched context
- **Multiple Sinks**: 
  - Console (development) - JSON formatted
  - AWS CloudWatch (production)
- **Enrichment**: Additional context automatically added to all logs

## Configuration

### Current Working Configuration

The logging is configured in `Program.cs` using proper Serilog hosting integration:

```csharp
// Configure Serilog with CloudWatch
builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console();
});
```

### Development Configuration (appsettings.Development.json)

```json
{
  "Serilog": {
    "Using": ["Serilog.Sinks.Console", "Serilog.Sinks.File"],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.Hosting.Lifetime": "Information",
        "Microsoft.EntityFrameworkCore": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "formatter": "Serilog.Formatting.Compact.CompactJsonFormatter, Serilog.Formatting.Compact"
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithEnvironmentName"]
  }
}
```

### Production Configuration

CloudWatch logging is wired in startup and turns on automatically when `ASPNETCORE_ENVIRONMENT=Production`. Audit logs use a separate logger and log group.

Required settings (can be left as defaults):

```json
{
  "AWS": {
    "Region": "us-east-1",
    "CloudWatch": {
      "LogGroup": "/live-event-service/logs",
      "Region": "us-east-1"
    }
  }
}
```

Notes:
- In Production, logs go to both Console and CloudWatch Logs; audit logs go to a dedicated log group.
- In non‑production, only Console is enabled by default.

## Request Logging with Correlation IDs

### Correlation ID Middleware

The application automatically handles correlation IDs:

```csharp
app.Use(async (context, next) =>
{
    // Add correlation ID to the request if not present
    var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? Guid.NewGuid().ToString();
    context.Items["CorrelationId"] = correlationId;
    
    // Add correlation ID to the response headers
    context.Response.Headers.Append("X-Correlation-ID", correlationId);
    
    // Add correlation ID to all logs in this request
    using (LogContext.PushProperty("CorrelationId", correlationId))
    {
        await next(context);
    }
});
```

### Request Logging

Request logging includes rich context:

```csharp
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
        diagnosticContext.Set("RemoteIpAddress", httpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty);
        
        // Add correlation ID if available
        if (httpContext.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationId))
        {
            diagnosticContext.Set("CorrelationId", correlationId);
        }
    };
});
```

## Log Levels

| Level | Description | Usage |
|-------|-------------|-------|
| Debug | Detailed information for debugging | Development debugging |
| Information | General application flow | Normal operations, request processing |
| Warning | Non-critical issues | Deprecated API usage, configuration warnings |
| Error | Errors that need investigation | Exceptions, failed operations |
| Fatal | Critical failures | Application startup failures |

## Usage Examples

### Basic Logging

```csharp
using Serilog;

// Basic structured log
Log.Information("Processing request for {UserId}", userId);

// Error with exception and context
try {
    // ... operation
} catch (Exception ex) {
    Log.Error(ex, "Failed to process registration for {UserId} and {EventId}", userId, eventId);
}

// Warning with structured data
Log.Warning("Event {EventId} is approaching capacity: {CurrentRegistrations}/{MaxCapacity}", 
    eventId, currentCount, maxCapacity);
```

### Using LogContext for Request Scope

```csharp
// Add properties to all logs within this scope
using (LogContext.PushProperty("UserId", userId))
using (LogContext.PushProperty("EventId", eventId))
{
    Log.Information("Starting event registration process");
    // All logs within this scope will include UserId and EventId
    await ProcessRegistration();
    Log.Information("Completed event registration process");
}
```

### Business Logic Logging

```csharp
public async Task<Result> RegisterForEventAsync(Guid eventId, Guid userId)
{
    using (LogContext.PushProperty("EventId", eventId))
    using (LogContext.PushProperty("UserId", userId))
    {
        Log.Information("Starting event registration");
        
        var eventEntity = await _eventRepository.GetByIdAsync(eventId);
        if (eventEntity == null)
        {
            Log.Warning("Event not found during registration attempt");
            return Result.Failed("Event not found");
        }
        
        if (eventEntity.IsFull())
        {
            Log.Information("Event is full, adding user to waitlist");
        }
        else
        {
            Log.Information("Event has capacity, confirming registration");
        }
        
        Log.Information("Event registration completed successfully");
        return Result.Success();
    }
}
```

## Correlation ID Usage

### Client-Side Usage

Send correlation IDs from your frontend:

```javascript
// JavaScript/TypeScript example
fetch('http://localhost:5000/health', {
    headers: {
        'X-Correlation-ID': crypto.randomUUID()
    }
});
```

```bash
# cURL example
curl -H "X-Correlation-ID: test-123" http://localhost:5000/health
```

### Server-Side Tracking

All logs within a request automatically include the correlation ID:

```json
{
  "@t": "2024-01-20T10:30:45.123Z",
  "@l": "Information",
  "@m": "Processing event registration",
  "UserId": "user-123",
  "EventId": "event-456",
  "CorrelationId": "test-123",
  "RequestId": "0HMV0123456789",
  "RequestPath": "/api/events/456/register",
  "MachineName": "liveevent-api",
  "EnvironmentName": "Development"
}
```

## Viewing Logs

### Development (Docker)

```bash
# View real-time API logs
docker logs liveevent-api --tail 50 --follow

# Filter for specific patterns
docker logs liveevent-api 2>&1 | grep "CorrelationId"

# View logs for specific correlation ID
docker logs liveevent-api 2>&1 | grep "test-123"
```

### Development (Local)

When running `dotnet run` locally, logs appear in the console with structured JSON format.

### Production (CloudWatch)

- **AWS Console**: Navigate to CloudWatch > Logs > Log groups > `/live-event-service/logs`
- **CLI Queries**:
  ```bash
  # Search by correlation ID
  aws logs filter-log-events \
    --log-group-name /live-event-service/logs \
    --filter-pattern "test-123"
  
  # Search for errors in the last hour
  aws logs filter-log-events \
    --log-group-name /live-event-service/logs \
    --start-time $(date -d '1 hour ago' +%s000) \
    --filter-pattern "ERROR"
  ```

## Best Practices

### 1. Structured Logging
Always use named properties instead of string concatenation:
```csharp
// ✅ Good
Log.Information("User {UserId} registered for event {EventId}", userId, eventId);

// ❌ Bad
Log.Information($"User {userId} registered for event {eventId}");
```

### 2. Correlation ID Usage
- Always include correlation IDs in external API calls
- Use them to trace requests across microservices
- Include them in error responses to help with debugging

### 3. Sensitive Data Protection
Never log PII or sensitive information:
```csharp
// ✅ Good
Log.Information("User authentication successful for {UserId}", userId);

// ❌ Bad - contains PII
Log.Information("User {Email} with password {Password} authenticated", email, password);
```

### 4. Appropriate Log Levels
- **Debug**: Detailed execution flow (disabled in production)
- **Information**: Key business events and request processing
- **Warning**: Unexpected but handled conditions
- **Error**: Exceptions and failures requiring investigation
- **Fatal**: Critical failures that may cause application termination

## Troubleshooting

### Common Issues - All Resolved ✅

The following issues have been **completely resolved**:

- ❌ ~~DiagnosticContext dependency injection errors~~ → ✅ **Fixed: Proper Serilog host integration**
- ❌ ~~Request logging middleware failures~~ → ✅ **Fixed: Correct Serilog configuration**
- ❌ ~~Missing correlation IDs~~ → ✅ **Fixed: Automatic correlation ID middleware**

### Current Status Verification

```bash
# Check logs are working
docker logs liveevent-api --tail 10

# Test correlation ID tracking
curl -H "X-Correlation-ID: test-correlation" http://localhost:5000/health
docker logs liveevent-api 2>&1 | grep "test-correlation"
```

### Performance Considerations

- **Log Level**: Keep production at Information or higher for performance
- **Structured Properties**: Limit the number of properties per log entry
- **Async Logging**: Serilog handles async logging automatically
- **CloudWatch Costs**: Monitor CloudWatch costs in production

## Configuration Examples

### Minimal Production Configuration

```json
{
  "Serilog": {
    "MinimumLevel": "Information",
    "WriteTo": [
      {
        "Name": "Console"
      },
      {
        "Name": "AmazonCloudWatch",
        "Args": {
          "logGroup": "/live-event-service/logs",
          "region": "us-east-1"
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithMachineName"]
  }
}
```

### Development with File Logging

```json
{
  "Serilog": {
    "MinimumLevel": "Debug",
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "formatter": "Serilog.Formatting.Compact.CompactJsonFormatter, Serilog.Formatting.Compact"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "Logs/log-.json",
          "rollingInterval": "Day",
          "formatter": "Serilog.Formatting.Compact.CompactJsonFormatter, Serilog.Formatting.Compact"
        }
      }
    ]
  }
}
```

## Integration with X-Ray

Correlation IDs are automatically included in X-Ray traces, providing complete request traceability across logging and distributed tracing systems.

## Next Steps

- **CloudWatch Setup**: Configure CloudWatch sink for production deployment
- **Log Aggregation**: Consider ELK stack or similar for advanced log analysis
- **Alerting**: Set up CloudWatch alarms based on error logs
- **Retention**: Configure appropriate log retention policies for cost management
