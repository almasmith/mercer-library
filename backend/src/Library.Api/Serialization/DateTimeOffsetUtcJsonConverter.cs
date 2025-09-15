using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Library.Api.Serialization;

/// <summary>
/// System.Text.Json converter for DateTimeOffset that always serializes in UTC with 'Z' suffix
/// and parses incoming ISO-8601 timestamps honoring offsets, returning UTC.
/// </summary>
public sealed class DateTimeOffsetUtcJsonConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            string? stringValue = reader.GetString();
            if (stringValue is null)
            {
                return default;
            }

            // Reject bare YYYY-MM-DD here; B52 will add support later
            if (System.Text.RegularExpressions.Regex.IsMatch(stringValue, @"^\d{4}-\d{2}-\d{2}$"))
            {
                throw new JsonException("Date-only format 'YYYY-MM-DD' is not supported yet.");
            }

            // Parse ISO-8601 strings with offsets and convert to UTC
            if (DateTimeOffset.TryParse(stringValue, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dto))
            {
                return dto.ToUniversalTime();
            }

            throw new JsonException($"Invalid DateTimeOffset format: '{stringValue}'");
        }

        throw new JsonException($"Unexpected token parsing DateTimeOffset. Token: {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        // Always write as UTC with 'Z' by serializing the UTC DateTime (Kind=Utc)
        writer.WriteStringValue(value.UtcDateTime.ToString("O"));
    }
}


