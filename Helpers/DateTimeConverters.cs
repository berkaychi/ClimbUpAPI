using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClimbUpAPI.Helpers
{
    public class IsoUtcDateTimeConverter : JsonConverter<DateTime>
    {
        private const string Format = "yyyy-MM-ddTHH:mm:ss.fffZ";

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var dateString = reader.GetString();
                if (string.IsNullOrEmpty(dateString))
                {
                    throw new JsonException("DateTime string cannot be null or empty.");
                }

                if (DateTime.TryParseExact(dateString, Format, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var specificFormatValue))
                {
                    return specificFormatValue;
                }

                if (DateTime.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var generalIsoValue))
                {
                    return DateTime.SpecifyKind(generalIsoValue, DateTimeKind.Utc);
                }

                throw new JsonException($"Unable to convert \"{dateString}\" to DateTime. Expected format: {Format} or a compatible ISO 8601 UTC format.");
            }
            throw new JsonException($"Unexpected token type {reader.TokenType} when parsing DateTime.");
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            DateTime utcValue = value;
            if (value.Kind == DateTimeKind.Unspecified)
            {
                utcValue = DateTime.SpecifyKind(value, DateTimeKind.Utc);
            }
            else if (value.Kind == DateTimeKind.Local)
            {
                utcValue = value.ToUniversalTime();
            }
            writer.WriteStringValue(utcValue.ToString(Format, CultureInfo.InvariantCulture));
        }
    }

    public class NullableIsoUtcDateTimeConverter : JsonConverter<DateTime?>
    {
        private readonly IsoUtcDateTimeConverter _innerConverter = new IsoUtcDateTimeConverter();

        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }
            return _innerConverter.Read(ref reader, typeof(DateTime), options);
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
            {
                _innerConverter.Write(writer, value.Value, options);
            }
            else
            {
                writer.WriteNullValue();
            }
        }
    }
}