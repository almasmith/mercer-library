using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Library.Api.Domain
{
    public sealed class ApplicationUser : IdentityUser<Guid>
    {
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }

    public sealed record RegisterRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; init; } = string.Empty;

        [Required]
        [MinLength(8)]
        public string Password { get; init; } = string.Empty;
    }

    public sealed record LoginRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; init; } = string.Empty;

        [Required]
        public string Password { get; init; } = string.Empty;
    }

    public sealed record AuthResponse(string AccessToken, int ExpiresIn);
}


