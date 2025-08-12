using System.Globalization;

namespace LiveEventService.API.Middleware;

public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _environment;

    public SecurityHeadersMiddleware(RequestDelegate next, IWebHostEnvironment environment)
    {
        _next = next;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;

            // Clickjacking and MIME sniffing protections
            headers.TryAdd("X-Frame-Options", "DENY");
            headers.TryAdd("X-Content-Type-Options", "nosniff");
            headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");

            // A conservative default Permissions-Policy for API-only service
            headers.TryAdd("Permissions-Policy", "geolocation=(), microphone=(), camera=()");

            // Content-Security-Policy is applied only in non-development to avoid interfering with dev tooling
            if (!_environment.IsDevelopment())
            {
                // API does not serve HTML; lock down by default
                headers.TryAdd("Content-Security-Policy", "default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'");
            }

            return Task.CompletedTask;
        });

        await _next(context);
    }
}


