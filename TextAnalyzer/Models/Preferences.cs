namespace TextAnalyzer.Models
{
    internal class Preferences
    {
        public bool IsShowOnlyFilteredLines { get; set; }

        public bool IsHideEmptyLines { get; set; }

        public double ZoomRatio { get; set; } = 1.0;
    }
}
