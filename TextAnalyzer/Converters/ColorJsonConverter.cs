using Avalonia.Media;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TextAnalyzer.Converters
{
    internal class ColorJsonConverter : JsonConverter<Color>
    {
        public override Color Read(
            ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                    {
                        var colorStr = reader.GetString();
                        if (colorStr == null)
                            return Colors.Transparent;

                        return Color.Parse(colorStr);
                    }

                case JsonTokenType.Number:
                    return Color.FromUInt32(reader.GetUInt32());

                default:
                    return Colors.Transparent;
            }
        }

        public override void Write(
            Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
