using MinimalApi.Conts;
using MinimalApi.Extensions;

namespace MinimalApi.TestRoutes;

internal static class TestRoutes
{
    public static RouteGroupBuilder MapTestRoutes(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/auth");

        // all routes to require DefaulPolicy
        group.RequireAuthorization(Consts.DefaulPolicy);

        // Rate limit all of the routes
        group.RequirePerUserRateLimit();

        // add auth details to swagger
        group.AddOpenApiSecurityRequirement();

        group.MapGet("/test", () => new { foo = "default" });

        group.MapGet("/admin", () => new { foo = "admin" })
            .RequireAuthorization(Consts.AdminPolicy);

        return group;
    }
}