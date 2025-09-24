using TextAnalyzer.Models;

namespace TextAnalyzer.ViewModels.Converters
{
    class FilterItemVMConverter
    {
        public static void Convert(EditFilterVM source, FilterItemVM target)
        {
            target.Description = source.Description;
            target.ForegroundColor = source.SelectedTextColor;
            target.BackgroundColor = source.SelectedBackgroundColor;

            target.Modifiers.Clear();

            if (source.IsExcluded)
                target.Modifiers.Add(FilterModiferType.Excluding);

            if (source.SelectedFilterType == FilterType.Marker)
            {
                target.Modifiers.Add(FilterModiferType.Marker);
                target.Marker = source.SelectedMarker;
            }
            else
            {
                target.Text = source.Text;

                if (source.IsCaseSensitive)
                    target.Modifiers.Add(FilterModiferType.CaseSensitive);

                if (source.IsRegularExpression)
                {
                    target.Modifiers.Add(FilterModiferType.RegularExpression);
                }
                else if (source.IsLogicOperation)
                {
                    target.Modifiers.Add(FilterModiferType.LogicOperation);
                }
            }
        }
    }
}
