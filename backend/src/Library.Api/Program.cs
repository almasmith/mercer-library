var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
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

app.UseAuthorization();

app.MapControllers();

app.Run();
