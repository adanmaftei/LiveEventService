# Security Enhancements

## Overview

This document outlines critical security improvements for the Live Event Service, focusing on protecting user data, preventing abuse, and ensuring compliance with security standards.

## 🔒 **Current Security Assessment**

### ✅ **Existing Security Measures**
- JWT-based authentication (AWS Cognito configuration; tests replace with test auth)
- Input validation (FluentValidation in Application layer where present)
- Structured logging with correlation IDs (Serilog)

### ⏳ Not Yet Implemented (Planned)
- HTTPS redirection and HSTS in the API middleware pipeline
- Security headers middleware (CSP, X-Frame-Options, X-Content-Type-Options, etc.)

### ⚠️ **Identified Security Gaps**
1. **Rate Limiting**: Not implemented
2. **Input Sanitization**: Limited protection against injection attacks
3. **Audit Logging**: No comprehensive audit trail
4. **Data Encryption**: Sensitive data not encrypted at rest
5. **Security Headers**: Missing
6. **CORS Configuration**: Default policy configured from `AllowedOrigins`; review hardening for prod

## 🚀 **Phase 1: Critical Security Improvements**

### 1. **Rate Limiting Implementation**

**Current Issue:**
- No protection against brute force attacks
- Potential for registration spam
- No protection against API abuse

**Proposed Solution:**
```csharp
public class RateLimitingMiddleware
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RateLimitingMiddleware> _logger;

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var clientId = GetClientIdentifier(context);
        var endpoint = context.Request.Path;
        
        if (await IsRateLimitExceeded(clientId, endpoint))
        {
            context.Response.StatusCode = 429; // Too Many Requests
            await context.Response.WriteAsync("Rate limit exceeded");
            return;
        }
        
        await next(context);
    }
}
```

**Rate Limiting Rules:**
- **Registration Endpoints**: 5 requests per minute per IP
- **Authentication**: 3 attempts per minute per IP
- **General API**: 100 requests per minute per IP
- **Admin Endpoints**: 50 requests per minute per authenticated user

### 2. **Enhanced Input Validation & Sanitization**

**Current Validation:**
```csharp
public class CreateEventCommandValidator : AbstractValidator<CreateEventCommand>
{
    public CreateEventCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}
```

**Enhanced Validation:**
```csharp
public class CreateEventCommandValidator : AbstractValidator<CreateEventCommand>
{
    public CreateEventCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100)
            .Matches(@"^[a-zA-Z0-9\s\-_\.]+$") // Alphanumeric only
            .MustAsync(async (name, cancellation) => 
                !await ContainsProfanity(name)); // Content filtering
        
        RuleFor(x => x.Description)
            .MaximumLength(500)
            .MustAsync(async (desc, cancellation) => 
                !await ContainsProfanity(desc));
        
        RuleFor(x => x.StartDate)
            .GreaterThan(DateTime.UtcNow.AddDays(1)) // Minimum 24h notice
            .LessThan(DateTime.UtcNow.AddYears(1)); // Maximum 1 year ahead
    }
}
```

### 3. **Comprehensive Audit Logging**

**Audit Log Model:**
```csharp
public class AuditLog
{
    public Guid Id { get; set; }
    public string UserId { get; set; }
    public string Action { get; set; }
    public string Resource { get; set; }
    public string ResourceId { get; set; }
    public string IpAddress { get; set; }
    public string UserAgent { get; set; }
    public DateTime Timestamp { get; set; }
    public string RequestId { get; set; }
    public string CorrelationId { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
}
```

**Audit Service:**
```csharp
public interface IAuditService
{
    Task LogAsync(string action, string resource, string resourceId, 
        Dictionary<string, object>? metadata = null);
    Task<List<AuditLog>> GetAuditTrailAsync(string resourceId, DateTime? from = null);
}

public class AuditService : IAuditService
{
    private readonly ILogger<AuditService> _logger;
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public async Task LogAsync(string action, string resource, string resourceId, 
        Dictionary<string, object>? metadata = null)
    {
        var context = _httpContextAccessor.HttpContext;
        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = context?.User?.Identity?.Name ?? "anonymous",
            Action = action,
            Resource = resource,
            ResourceId = resourceId,
            IpAddress = GetClientIpAddress(context),
            UserAgent = context?.Request.Headers["User-Agent"].ToString(),
            Timestamp = DateTime.UtcNow,
            RequestId = context?.TraceIdentifier,
            CorrelationId = context?.Request.Headers["X-Correlation-ID"].ToString(),
            Metadata = metadata ?? new Dictionary<string, object>()
        };

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();
    }
}
```

### 4. **Data Encryption at Rest**

**Encryption Service:**
```csharp
public interface IEncryptionService
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
    byte[] EncryptBytes(byte[] plainBytes);
    byte[] DecryptBytes(byte[] cipherBytes);
}

public class AesEncryptionService : IEncryptionService
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    public AesEncryptionService(IConfiguration configuration)
    {
        _key = Convert.FromBase64String(configuration["Encryption:Key"]);
        _iv = Convert.FromBase64String(configuration["Encryption:IV"]);
    }

    public string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        
        return Convert.ToBase64String(cipherBytes);
    }
}
```

**Entity Configuration for Encrypted Fields:**
```csharp
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(u => u.Email)
            .HasConversion(
                v => _encryptionService.Encrypt(v),
                v => _encryptionService.Decrypt(v));
        
        builder.Property(u => u.PhoneNumber)
            .HasConversion(
                v => v != null ? _encryptionService.Encrypt(v) : null,
                v => v != null ? _encryptionService.Decrypt(v) : null);
    }
}
```

## 🛡️ **Phase 2: Advanced Security Features**

### 1. **Security Headers Implementation**

**Security Headers Middleware:**
```csharp
public class SecurityHeadersMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        // Content Security Policy
        context.Response.Headers.Add("Content-Security-Policy", 
            "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline';");
        
        // X-Frame-Options
        context.Response.Headers.Add("X-Frame-Options", "DENY");
        
        // X-Content-Type-Options
        context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
        
        // X-XSS-Protection
        context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
        
        // Referrer Policy
        context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
        
        // Permissions Policy
        context.Response.Headers.Add("Permissions-Policy", 
            "geolocation=(), microphone=(), camera=()");
        
        await next(context);
    }
}
```

### 2. **CORS Configuration**

**Secure CORS Setup:**
```csharp
public static class CorsExtensions
{
    public static IServiceCollection AddSecureCors(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("SecureCorsPolicy", policy =>
            {
                policy.WithOrigins(configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>())
                      .WithMethods("GET", "POST", "PUT", "DELETE")
                      .WithHeaders("Authorization", "Content-Type", "X-Correlation-ID")
                      .AllowCredentials()
                      .SetPreflightMaxAge(TimeSpan.FromHours(1));
            });
        });
        
        return services;
    }
}
```

### 3. **JWT Token Security Enhancements**

**Enhanced JWT Configuration:**
```csharp
public static class JwtExtensions
{
    public static IServiceCollection AddSecureJwt(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(configuration["Jwt:Key"])),
                    ClockSkew = TimeSpan.Zero, // No clock skew tolerance
                    RequireExpirationTime = true,
                    RequireSignedTokens = true
                };
                
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        // Additional token validation logic
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        // Log authentication failures
                        return Task.CompletedTask;
                    }
                };
            });
        
        return services;
    }
}
```

### 4. **Content Security Policy (CSP)**

**CSP Implementation:**
```csharp
public class ContentSecurityPolicyMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var cspBuilder = new StringBuilder();
        
        // Default source
        cspBuilder.Append("default-src 'self'; ");
        
        // Script sources
        cspBuilder.Append("script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; ");
        
        // Style sources
        cspBuilder.Append("style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; ");
        
        // Font sources
        cspBuilder.Append("font-src 'self' https://fonts.gstatic.com; ");
        
        // Image sources
        cspBuilder.Append("img-src 'self' data: https:; ");
        
        // Connect sources (for API calls)
        cspBuilder.Append("connect-src 'self' https://api.example.com; ");
        
        // Frame ancestors (prevent clickjacking)
        cspBuilder.Append("frame-ancestors 'none'; ");
        
        // Base URI
        cspBuilder.Append("base-uri 'self'; ");
        
        // Form action
        cspBuilder.Append("form-action 'self'; ");
        
        context.Response.Headers.Add("Content-Security-Policy", cspBuilder.ToString());
        
        await next(context);
    }
}
```

## 🔍 **Security Monitoring & Alerting**

### 1. **Security Event Monitoring**

**Security Event Model:**
```csharp
public class SecurityEvent
{
    public Guid Id { get; set; }
    public string EventType { get; set; }
    public string Severity { get; set; }
    public string Source { get; set; }
    public string IpAddress { get; set; }
    public string UserId { get; set; }
    public string Description { get; set; }
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
}
```

**Security Monitoring Service:**
```csharp
public interface ISecurityMonitoringService
{
    Task LogSecurityEventAsync(string eventType, string severity, string description, 
        Dictionary<string, object>? metadata = null);
    Task<List<SecurityEvent>> GetSecurityEventsAsync(DateTime? from = null, string? severity = null);
    Task<bool> IsIpAddressBlockedAsync(string ipAddress);
    Task BlockIpAddressAsync(string ipAddress, TimeSpan duration, string reason);
}
```

### 2. **Automated Threat Detection**

**Threat Detection Rules:**
```csharp
public class ThreatDetectionService
{
    public async Task<bool> DetectSuspiciousActivityAsync(string ipAddress, string userId)
    {
        // Check for multiple failed login attempts
        var failedLogins = await GetFailedLoginAttemptsAsync(ipAddress, TimeSpan.FromMinutes(15));
        if (failedLogins.Count >= 5)
        {
            await _securityMonitoring.LogSecurityEventAsync(
                "MultipleFailedLogins", "High", 
                $"Multiple failed login attempts from IP: {ipAddress}");
            return true;
        }
        
        // Check for rapid registration attempts
        var recentRegistrations = await GetRecentRegistrationsAsync(ipAddress, TimeSpan.FromMinutes(5));
        if (recentRegistrations.Count >= 10)
        {
            await _securityMonitoring.LogSecurityEventAsync(
                "RapidRegistrations", "Medium", 
                $"Rapid registration attempts from IP: {ipAddress}");
            return true;
        }
        
        return false;
    }
}
```

## 📋 **Implementation Plan**

### **Week 1: Foundation Security**
- [ ] Implement rate limiting middleware
- [ ] Add security headers middleware
- [ ] Configure secure CORS policy
- [ ] Set up audit logging infrastructure

### **Week 2: Input Validation & Sanitization**
- [ ] Enhance input validation with content filtering
- [ ] Implement input sanitization
- [ ] Add SQL injection protection
- [ ] Implement XSS protection

### **Week 3: Data Protection**
- [ ] Implement data encryption at rest
- [ ] Add encryption for sensitive fields
- [ ] Implement secure key management
- [ ] Add data masking for logs

### **Week 4: Monitoring & Alerting**
- [ ] Set up security event monitoring
- [ ] Implement automated threat detection
- [ ] Configure security alerting
- [ ] Create security dashboards

## 🔧 **Security Configuration**

### **appsettings.json Security Section:**
```json
{
  "Security": {
    "RateLimiting": {
      "RegistrationEndpoint": {
        "RequestsPerMinute": 5,
        "BurstLimit": 10
      },
      "AuthenticationEndpoint": {
        "RequestsPerMinute": 3,
        "BurstLimit": 5
      }
    },
    "Encryption": {
      "Key": "your-base64-encoded-32-byte-key",
      "IV": "your-base64-encoded-16-byte-iv"
    },
    "Jwt": {
      "Key": "your-secret-key-minimum-32-characters",
      "Issuer": "LiveEventService",
      "Audience": "LiveEventService.API",
      "ExpirationMinutes": 60
    },
    "AllowedOrigins": [
      "https://yourdomain.com",
      "https://admin.yourdomain.com"
    ]
  }
}
```

## 🧪 **Security Testing Strategy**

### **Automated Security Testing**
1. **OWASP ZAP Integration**: Automated vulnerability scanning
2. **Security Unit Tests**: Test security middleware and services
3. **Penetration Testing**: Regular security assessments
4. **Dependency Scanning**: Check for vulnerable packages

### **Security Test Scenarios**
- **Rate Limiting Tests**: Verify rate limiting works correctly
- **Input Validation Tests**: Test various malicious inputs
- **Authentication Tests**: Test JWT token security
- **Authorization Tests**: Verify proper access controls

## 📊 **Security Metrics & KPIs**

### **Key Security Indicators**
- **Failed Authentication Attempts**: < 1% of total attempts
- **Rate Limit Violations**: < 0.1% of requests
- **Security Events**: Track and trend security incidents
- **Vulnerability Scan Results**: Zero critical/high vulnerabilities

### **Security Alerting Rules**
- Multiple failed login attempts from same IP
- Unusual registration patterns
- Security header violations
- Encryption/decryption failures

## 🚀 **Next Steps**

1. **Start with Phase 1**: Implement critical security improvements
2. **Security Assessment**: Conduct initial security audit
3. **Implement Incrementally**: Deploy security features gradually
4. **Monitor & Adjust**: Track security metrics and adjust as needed
5. **Regular Reviews**: Schedule regular security reviews

---

**This comprehensive security enhancement plan will transform the Live Event Service into a secure, enterprise-grade platform that protects user data and prevents security threats!** 🛡️ 