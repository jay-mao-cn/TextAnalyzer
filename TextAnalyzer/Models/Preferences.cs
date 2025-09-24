using Avalonia.Media;
using System.Text.Json.Serialization;
using TextAnalyzer.Converters;

namespace TextAnalyzer.Models
{
    internal class Preferences
    {
        [JsonConverter(typeof(ColorJsonConverter))]
        public Color DefaultExcludedTextColor { get; set; }
            = Color.FromRgb(0x6D, 0x6D, 0x6D);

        [JsonConverter(typeof(ColorJsonConverter))]
        public Color DefaultForegroundColor { get; set; } = Colors.Black;

        [JsonConverter(typeof(ColorJsonConverter))]
        public Color DefaultBackgroundColor { get; set; } = Colors.Transparent;

        public bool IsShowOnlyFilteredLines { get; set; }

        public bool IsHideEmptyLines { get; set; }

        public double ZoomRatio { get; set; } = 1.0;

    }
}
