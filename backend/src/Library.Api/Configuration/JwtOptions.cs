using System.ComponentModel.DataAnnotations;

namespace Library.Api.Configuration;

public sealed class JwtOptions
{
    [Required]
    [MinLength(16)]
    public string Secret { get; set; } = string.Empty;

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    [Range(1, 1440)]
    public int ExpiresMinutes { get; set; }
}


