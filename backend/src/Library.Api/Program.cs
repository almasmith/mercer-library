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

var app = builder.Build();
// Log on successful startup to evidence options validation passed
app.Lifetime.ApplicationStarted.Register(() =>
{
    app.Logger.LogInformation("Configuration options validated successfully");
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// CORS: must be placed before authentication/authorization
app.UseCors("SpaCors");

app.UseAuthorization();

app.MapControllers();

app.Run();
