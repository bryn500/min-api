using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;

namespace MinimalApi.Extensions;

public static class RateLimitExtensions
{
    private static readonly string Policy = "PerUserRatelimit";

    /// <summary>
    /// Add PerUserRatelimit to app
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddRateLimiting(this IServiceCollection services)
    {
        return services.AddRateLimiter(limiterOptions =>
        {
            // Log 429 and return retry after value (this doesn't change over window)
            limiterOptions.OnRejected = (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString(NumberFormatInfo.InvariantInfo);
                }

                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.RequestServices.GetService<ILoggerFactory>()?
                    .CreateLogger("Microsoft.AspNetCore.RateLimitingMiddleware")
                    .LogWarning("OnRejected: {GetUserEndPoint}", GetUserEndPoint(context.HttpContext));

                return new ValueTask();
            };

            // rate limit by user name
            // todo: add this to env variables
            limiterOptions.AddPolicy(Policy, context =>
            {
                var username = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

                return RateLimitPartition.GetTokenBucketLimiter(username, key =>
                {
                    return new()
                    {
                        AutoReplenishment = true,
                        ReplenishmentPeriod = TimeSpan.FromSeconds(10), // time between replenishments
                        TokenLimit = 1, // maximum number of concurrent requests
                        TokensPerPeriod = 1, // how many get restored in the timeframe
                        QueueLimit = 0 // how many requests will queue up before being rejected 
                    };
                });
            });
        });
    }

    /// <summary>
    /// Requires rate limiting for an endpoint
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static IEndpointConventionBuilder RequirePerUserRateLimit(this IEndpointConventionBuilder builder)
    {
        return builder.RequireRateLimiting(Policy);
    }

    /// <summary>
    /// Returns the Username, endpoint and IP of a request from the HttpContext
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    static string GetUserEndPoint(HttpContext context) =>
       $"User {context.User.Identity?.Name ?? "Anonymous"} endpoint:{context.Request.Path}" + $" {context.Connection.RemoteIpAddress}";
}
