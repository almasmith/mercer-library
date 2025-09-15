using Library.Api.Domain;
using Swashbuckle.AspNetCore.Filters;

namespace Library.Api.Controllers;

public sealed class RegisterRequestExample : IExamplesProvider<RegisterRequest>
{
    public RegisterRequest GetExamples() => new RegisterRequest
    {
        Email = "user@example.com",
        Password = "Passw0rd!"
    };
}

public sealed class LoginRequestExample : IExamplesProvider<LoginRequest>
{
    public LoginRequest GetExamples() => new LoginRequest
    {
        Email = "user@example.com",
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


