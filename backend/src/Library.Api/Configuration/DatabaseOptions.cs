using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;

namespace Library.Api.Configuration;

public sealed class DatabaseOptions
{
    [Required]
    [ConfigurationKeyName("DB_PROVIDER")]
    public string DbProvider { get; set; } = string.Empty;

    public DatabaseConnectionStrings ConnectionStrings { get; set; } = new();

    public sealed class DatabaseConnectionStrings
    {
        public string? Sqlite { get; set; }
        public string? SqlServer { get; set; }
    }
}


