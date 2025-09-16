using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Library.Api.Serialization;

/// <summary>
/// Converter that accepts date-only strings ("YYYY-MM-DD") for DateTimeOffset and coerces them to
/// midnight UTC ("YYYY-MM-DDT00:00:00Z"). For other strings, falls back to DateTimeOffset parsing
/// honoring provided offsets, returning UTC.
/// </summary>
public sealed class DateOnlyStringToDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
    private static readonly System.Text.RegularExpressions.Regex DateOnlyRegex =
        new(pattern: @"^\d{4}-\d{2}-\d{2}$", options: System.Text.RegularExpressions.RegexOptions.Compiled);

    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Unexpected token parsing DateTimeOffset. Token: {reader.TokenType}");
        }

        string? stringValue = reader.GetString();
        if (stringValue is null)
        {
            return default;
        }

        // If value is a bare date (YYYY-MM-DD), coerce to midnight UTC
        if (DateOnlyRegex.IsMatch(stringValue))
        {
            // Parse as DateOnly, create DateTime at midnight (Unspecified), then specify UTC
            if (!DateOnly.TryParseExact(stringValue, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var dateOnly))
            {
                throw new JsonException($"Invalid date-only format: '{stringValue}'");
            }

            var dateTimeUnspecified = dateOnly.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
            // Interpret the unspecified local as UTC midnight for the given date
            var utcDateTime = DateTime.SpecifyKind(dateTimeUnspecified, DateTimeKind.Utc);
            return new DateTimeOffset(utcDateTime);
        }

        // Otherwise parse as ISO-8601 honoring offset and normalize to UTC
        if (DateTimeOffset.TryParse(stringValue, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dto))
        {
            return dto.ToUniversalTime();
        }

        throw new JsonException($"Invalid DateTimeOffset format: '{stringValue}'");
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        // Keep symmetry with existing UTC converter: write as UTC with 'Z' using round-trip format
        writer.WriteStringValue(value.UtcDateTime.ToString("O"));
    }
}


