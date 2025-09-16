namespace Library.Api.Dtos.Auth;

public sealed record AuthResponse(string AccessToken, int ExpiresIn);
