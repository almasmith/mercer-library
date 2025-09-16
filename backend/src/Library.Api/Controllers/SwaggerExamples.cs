using Library.Api.Domain;
using Library.Api.Dtos.Analytics;
using Swashbuckle.AspNetCore.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Controllers;

public sealed class RegisterRequestExample : IExamplesProvider<RegisterRequest>
{
    public RegisterRequest GetExamples() => new RegisterRequest
    {
        Email = "test@example.com",
        Password = "Passw0rd!"
    };
}

public sealed class LoginRequestExample : IExamplesProvider<LoginRequest>
{
    public LoginRequest GetExamples() => new LoginRequest
    {
        Email = "test@example.com",
        Password = "Passw0rd!"
    };
}

public sealed class AuthResponseExample : IExamplesProvider<AuthResponse>
{
    public AuthResponse GetExamples() => new AuthResponse(
        AccessToken: "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
        ExpiresIn: 3600
    );
}

public sealed class RegisterValidationProblemExample : IExamplesProvider<ValidationProblemDetails>
{
    public ValidationProblemDetails GetExamples()
    {
        var details = new ValidationProblemDetails
        {
            Title = "One or more validation errors occurred.",
            Status = StatusCodes.Status400BadRequest,
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
        };

        details.Errors.Add("email", new[] { "A user with that email already exists." });
        return details;
    }
}


public sealed class AvgRatingResponseExample : IExamplesProvider<IReadOnlyList<AvgRatingBucketDto>>
{
    public IReadOnlyList<AvgRatingBucketDto> GetExamples() => new List<AvgRatingBucketDto>
    {
        new AvgRatingBucketDto("2025-08", 4.25),
        new AvgRatingBucketDto("2025-09", 3.8)
    };
}

public sealed class MostReadGenresResponseExample : IExamplesProvider<IReadOnlyList<MostReadGenreDto>>
{
    public IReadOnlyList<MostReadGenreDto> GetExamples() => new List<MostReadGenreDto>
    {
        new MostReadGenreDto("Fantasy", 12),
        new MostReadGenreDto("Mystery", 12),
        new MostReadGenreDto("Classics", 5)
    };
}


