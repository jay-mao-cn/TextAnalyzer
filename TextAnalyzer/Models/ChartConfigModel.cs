namespace TextAnalyzer.Models
{
    internal class ChartConfigModel
    {
        public string Title { get; set; } = string.Empty;
        public string Filter { get; set; } = string.Empty;
        public bool IsCaseSensitive { get; set; }
        public bool IsRegularExpression { get; set; }
        public bool IsLogicOperation { get; set; }
        public string Key { get; set; } = string.Empty;
        public string EndChar { get; set; } = string.Empty;
        public int LabelStartPos { get; set; } = 0;
        public int LabelLength { get; set; } = 0;
    }
}
