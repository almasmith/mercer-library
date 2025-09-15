using System.ComponentModel.DataAnnotations;

namespace Library.Api.Configuration;

public sealed class RateLimitOptions
{
    [Range(1, 10000)]
    public int AuthPerMinute { get; set; }

    [Range(1, 10000)]
    public int ApiPerMinute { get; set; }
}


