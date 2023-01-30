using MinimalApi.Conts;
using MinimalApi.Extensions;
using MinimalApi.TestRoutes;
using Swashbuckle.AspNetCore.SwaggerGen;

// builder services [order does not matter]
var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.AddServerHeader = false;
});

// Configure auth
// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/security?view=aspnetcore-7.0
builder.Services.AddAuthentication()
    .AddJwtBearer(options => {
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Consts.DefaulPolicy, policy =>
        policy
            .RequireRole(Consts.UserRole))
    .AddPolicy(Consts.AdminPolicy, policy =>
        policy
            .RequireRole(Consts.AdminRole));
//.RequireScope(Consts.ApiScope))

// Configure Open API
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.Configure<SwaggerGeneratorOptions>(o => o.InferSecuritySchemes = true);

// Configure rate limiting
builder.Services.AddRateLimiting();

// App pipeline [order matters]
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// rate limit after auth
app.UseRateLimiter(); 

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    app.Map("/", () => Results.Redirect("/swagger"));

    app.MapGet("/info", () => $"dotnet user-jwts create --role \"{Consts.UserRole}\" --role \"{Consts.AdminRole}\""); //--scope \"{Consts.ApiScope}\" 

    app.MapGet("/hello", () => "Hello World!")
        .RequireAuthorization()
        .RequirePerUserRateLimit()
        .AddOpenApiSecurityRequirement();
}

// setup routes held in external file
app.MapTestRoutes();

app.Run();
