using LiveEventService.API.Constants;
using LiveEventService.API.Configuration;
using Microsoft.Extensions.Options;

namespace LiveEventService.API.Middleware;

/// <summary>
/// Adds a baseline set of security headers (X-Frame-Options, X-Content-Type-Options, Referrer-Policy),
/// a conservative Permissions-Policy, and an optional Content-Security-Policy (CSP) based on configuration.
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate next;
    private readonly IWebHostEnvironment environment;
    private readonly IConfiguration configuration;
    private readonly SecurityOptions securityOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecurityHeadersMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="environment">The hosting environment.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <param name="options">Security options bound from configuration.</param>
    public SecurityHeadersMiddleware(RequestDelegate next, IWebHostEnvironment environment, IConfiguration configuration, IOptions<SecurityOptions> options)
    {
        this.next = next;
        this.environment = environment;
        this.configuration = configuration;
        this.securityOptions = options.Value;
    }

    /// <summary>
    /// Applies headers on response start, then continues the pipeline.
    /// </summary>
    /// <param name="context">The HTTP context for the current request.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task InvokeAsync(HttpContext context)
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

        return next(context);
    }
}
