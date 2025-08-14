using System.Globalization;
using LiveEventService.API.Constants;
using LiveEventService.API.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace LiveEventService.API.Middleware;

public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate next;
    private readonly IWebHostEnvironment environment;
    private readonly IConfiguration configuration;
    private readonly SecurityOptions securityOptions;

    public SecurityHeadersMiddleware(RequestDelegate next, IWebHostEnvironment environment, IConfiguration configuration, IOptions<SecurityOptions> options)
    {
        this.next = next;
        this.environment = environment;
        this.configuration = configuration;
        this.securityOptions = options.Value;
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
            var cspSection = configuration.GetSection("Security:Csp");
            var cspEnabled = securityOptions.Csp.Enabled || (cspSection.GetValue<bool?>("Enabled") ?? !environment.IsDevelopment());
            if (cspEnabled)
            {
                var policy = securityOptions.Csp.Policy ?? cspSection["Policy"] ?? "default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'";
                headers[SecurityHeaderNames.ContentSecurityPolicy] = policy;
            }

            return Task.CompletedTask;
        });

        await next(context);
    }
}


