namespace TextAnalyzer.Interfaces
{
    enum FocusedArea
    {
        None,
        Texts,
        Filters,
    }

    internal interface IFocusMonitor
    {
        FocusedArea GetFocusedArea();
    }
}
