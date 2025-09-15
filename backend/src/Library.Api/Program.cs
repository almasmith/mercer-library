using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Library.Api.Domain;
using Library.Api.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Library.Api.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    o.JsonSerializerOptions.Converters.Add(new Library.Api.Serialization.DateTimeOffsetUtcJsonConverter());
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
builder.Services.AddAuthorization();

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
