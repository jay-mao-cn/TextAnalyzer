using Avalonia.Media;
using System.Text.Json.Serialization;
using TextAnalyzer.Converters;

namespace TextAnalyzer.Models
{
    internal class FilterModel
    {
        public bool IsEnabled { get; set; }
        public FilterType FilterType { get; set; }
        [JsonConverter(typeof(ColorJsonConverter))]
        public Color ForegroundColor { get; set; }
        [JsonConverter(typeof(ColorJsonConverter))]
        public Color BackgroundColor { get; set; }
        public string FilterText { get; set; } = string.Empty;
        public int Marker { get; set; }
        public string Description { get; set; } = string.Empty;
        public bool IsExcluded { get; set; }
        public bool IsCaseSensitive { get; set; }
        public bool IsRegularExpression { get; set; }
        public bool IsLogicOperation { get; set; }
    }
}
