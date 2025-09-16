using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using FluentValidation;
using FluentValidation.AspNetCore;
using Library.Api.Domain;
using Library.Api.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Library.Api.Configuration;
using Swashbuckle.AspNetCore.Filters;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddJsonConsole();

// Add services to the container.

builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    // B52: Accept YYYY-MM-DD for DateTimeOffset by placing this converter before the UTC converter
    o.JsonSerializerOptions.Converters.Add(new Library.Api.Serialization.DateOnlyStringToDateTimeOffsetConverter());
    o.JsonSerializerOptions.Converters.Add(new Library.Api.Serialization.DateTimeOffsetUtcJsonConverter());
})
.ConfigureApiBehaviorOptions(o =>
{
    o.InvalidModelStateResponseFactory = ctx =>
    {
        var correlationId = ctx.HttpContext.Response.Headers["X-Correlation-ID"].ToString();
        var vpd = new ValidationProblemDetails(ctx.ModelState)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred.",
            Instance = $"{ctx.HttpContext.Request.Path}?cid={correlationId}"
        };
        return new BadRequestObjectResult(vpd) { ContentTypes = { "application/problem+json" } };
    };
});
// FluentValidation: automatic model validation and validator discovery
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Library.Api.Validation.CreateBookRequestValidator>();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.EnableAnnotations();
    c.ExampleFilters();
    // JWT Bearer security definition for Swagger UI "Authorize" button
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter only the token value.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[] { }
        }
    });
});
builder.Services.AddSwaggerExamplesFromAssemblyOf<Program>();

// AutoMapper registration
builder.Services.AddAutoMapper(typeof(Program));

// SignalR for realtime updates
builder.Services.AddSignalR();

// Options binding with validation and ValidateOnStart
builder.Services
    .AddOptions<Library.Api.Configuration.JwtOptions>()
    .Bind(builder.Configuration.GetSection("JWT"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// JWT authentication
var jwtOptions = builder.Configuration.GetSection("JWT").Get<JwtOptions>() ?? throw new InvalidOperationException("JWT configuration is missing.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.RequireHttpsMetadata = false;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization(o =>
{
	o.FallbackPolicy = new AuthorizationPolicyBuilder()
		.RequireAuthenticatedUser()
		.Build();
});

builder.Services
    .AddOptions<Library.Api.Configuration.CorsOptions>()
    .Bind(builder.Configuration.GetSection("CORS"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// CORS: register SpaCors policy reading allowed origins from configuration
var allowedOrigins = builder.Configuration.GetSection("CORS").GetValue<string>("AllowedOrigins") ?? string.Empty;
var allowedOriginsArray = allowedOrigins
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    .Where(o => !string.IsNullOrWhiteSpace(o))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

builder.Services.AddCors(options =>
{
    options.AddPolicy("SpaCors", policy =>
        policy
            .WithOrigins(allowedOriginsArray)
            .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
            .WithHeaders("Authorization", "Content-Type", "If-Match", "If-None-Match", "X-Correlation-ID")
            .WithExposedHeaders("ETag", "X-Correlation-ID"));
});

builder.Services
    .AddOptions<Library.Api.Configuration.DatabaseOptions>()
    .Bind(builder.Configuration)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<Library.Api.Configuration.RateLimitOptions>()
    .Bind(builder.Configuration.GetSection("RateLimiting"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Database provider selection and DbContext registration
{
    var dbOptions = new Library.Api.Configuration.DatabaseOptions();
    builder.Configuration.Bind(dbOptions);

    var provider = dbOptions.DbProvider?.Trim().ToLowerInvariant();

    if (string.IsNullOrWhiteSpace(provider))
    {
        throw new InvalidOperationException("DB provider is not configured. Set DB_PROVIDER to 'sqlite' or 'sqlserver'.");
    }

    var connectionStrings = dbOptions.ConnectionStrings ?? new Library.Api.Configuration.DatabaseOptions.DatabaseConnectionStrings();

    builder.Services.AddDbContext<Library.Api.Data.LibraryDbContext>(options =>
    {
        switch (provider)
        {
            case "sqlite":
                if (string.IsNullOrWhiteSpace(connectionStrings.Sqlite))
                {
                    throw new InvalidOperationException("Sqlite connection string is not configured under ConnectionStrings:Sqlite.");
                }
                options.UseSqlite(connectionStrings.Sqlite);
                break;
            case "sqlserver":
                if (string.IsNullOrWhiteSpace(connectionStrings.SqlServer))
                {
                    throw new InvalidOperationException("SqlServer connection string is not configured under ConnectionStrings:SqlServer.");
                }
                options.UseSqlServer(connectionStrings.SqlServer);
                break;
            default:
                throw new InvalidOperationException($"Unsupported DB provider '{dbOptions.DbProvider}'. Supported providers: sqlite, sqlserver.");
        }
    });
}

// Identity (for UserManager used by DevSeeder)
builder.Services.AddIdentityCore<ApplicationUser>(o =>
{
    o.User.RequireUniqueEmail = true;
    o.Password.RequiredLength = 8;
    o.Password.RequireDigit = true;
    o.Password.RequireNonAlphanumeric = true;
    o.Password.RequireUppercase = true;
    o.Password.RequireLowercase = true;
    o.Lockout.MaxFailedAccessAttempts = 5;
})
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<LibraryDbContext>()
    .AddSignInManager();

// Register JWT token service
builder.Services.AddScoped<Library.Api.Services.IJwtTokenService, Library.Api.Services.JwtTokenService>();

// Register Book service
builder.Services.AddScoped<Library.Api.Services.IBookService, Library.Api.Services.BookService>();

// Register Stats version service
builder.Services.AddScoped<Library.Api.Services.Stats.IStatsVersionService, Library.Api.Services.Stats.StatsVersionService>();

// Register Favorites service
builder.Services.AddScoped<Library.Api.Services.Favorites.IFavoritesService, Library.Api.Services.Favorites.FavoritesService>();

// Register Analytics service
builder.Services.AddScoped<Library.Api.Services.Analytics.IAnalyticsService, Library.Api.Services.Analytics.AnalyticsService>();

// Realtime publisher
builder.Services.AddScoped<Library.Api.Hubs.IRealtimePublisher, Library.Api.Hubs.RealtimePublisher>();

// Health checks: liveness and readiness (DB)
builder.Services.AddHealthChecks()
    .AddDbContextCheck<LibraryDbContext>("db", tags: new[] { "ready" });

// HTTP request/response logging with safe fields only
builder.Services.AddHttpLogging(o =>
{
    o.LoggingFields = HttpLoggingFields.RequestPath
        | HttpLoggingFields.RequestMethod
        | HttpLoggingFields.ResponseStatusCode
        | HttpLoggingFields.Duration;
    // Do not log Authorization headers or bodies
});

var app = builder.Build();
// Log on successful startup to evidence options validation passed
app.Lifetime.ApplicationStarted.Register(() =>
{
    app.Logger.LogInformation("Configuration options validated successfully");
});

// Also log selected DB provider
{
    var dbOptions = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<Library.Api.Configuration.DatabaseOptions>>().Value;
    app.Logger.LogInformation("Using database provider: {Provider}", dbOptions.DbProvider);
}

// Configure the HTTP request pipeline.
// Correlation ID must be first to capture/propagate for all following middleware (including Swagger & seeding)
app.UseMiddleware<Library.Api.Middleware.CorrelationIdMiddleware>();

// HTTP logging (requests/responses) - after correlation id so scope includes it
app.UseHttpLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Development data seeding (idempotent)
    await DevSeeder.SeedAsync(app.Services, CancellationToken.None);
}

app.UseHttpsRedirection();

// CORS: must be placed before authentication/authorization
app.UseCors("SpaCors");

app.UseMiddleware<Library.Api.Middleware.ExceptionHandlingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

// Health check endpoints
app.MapHealthChecks("/health").AllowAnonymous();
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = r => r.Tags.Contains("ready") }).AllowAnonymous();

app.MapControllers();

// SignalR hub endpoint
app.MapHub<Library.Api.Hubs.LibraryHub>("/hubs/library");

app.Run();
