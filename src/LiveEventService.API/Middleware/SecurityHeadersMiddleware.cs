using System.Globalization;
using LiveEventService.API.Constants;
using Microsoft.Extensions.Configuration;

namespace LiveEventService.API.Middleware;

public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public SecurityHeadersMiddleware(RequestDelegate next, IWebHostEnvironment environment, IConfiguration configuration)
    {
        _next = next;
        _environment = environment;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;

            // Clickjacking and MIME sniffing protections
            headers.TryAdd(SecurityHeaderNames.XFrameOptions, "DENY");
            headers.TryAdd(SecurityHeaderNames.XContentTypeOptions, "nosniff");
            headers.TryAdd(SecurityHeaderNames.ReferrerPolicy, "strict-origin-when-cross-origin");

            // A conservative default Permissions-Policy for API-only service
            headers.TryAdd(SecurityHeaderNames.PermissionsPolicy, "geolocation=(), microphone=(), camera=()");

            // Content-Security-Policy: configurable. Defaults to strict policy when enabled.
            var cspSection = _configuration.GetSection("Security:Csp");
            var cspEnabled = cspSection.GetValue<bool?>("Enabled") ?? !_environment.IsDevelopment();
            if (cspEnabled)
            {
                var policy = cspSection["Policy"] ?? "default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'";
                headers[SecurityHeaderNames.ContentSecurityPolicy] = policy;
            }

            return Task.CompletedTask;
        });

        await _next(context);
    }
}


