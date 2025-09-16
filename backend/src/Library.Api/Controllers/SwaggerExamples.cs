using Library.Api.Domain;
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


