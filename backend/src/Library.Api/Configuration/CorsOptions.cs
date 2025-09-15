using System.ComponentModel.DataAnnotations;

namespace Library.Api.Configuration;

public sealed class CorsOptions
{
    [Required]
    public string AllowedOrigins { get; set; } = string.Empty;
}


