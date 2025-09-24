using System.Diagnostics;
using TextAnalyzer.ViewModels;

namespace TextAnalyzer.Models.Converters
{
    static class FilterModelConverter
    {
        public static FilterModel ToModel(this FilterItemVM filterItemVM)
        {
            var model = new FilterModel();
            model.IsEnabled = filterItemVM.IsEnabled;

            if (filterItemVM.Modifiers.Contains(FilterModiferType.Marker))
            {
                model.FilterType = FilterType.Marker;
            }
            else
            {
                model.FilterType = FilterType.Text;
            }

            model.ForegroundColor = filterItemVM.ForegroundColor;
            model.BackgroundColor = filterItemVM.BackgroundColor;
            model.FilterText = filterItemVM.Text;
            model.Marker = filterItemVM.Marker;
            model.Description = filterItemVM.Description;
            model.IsExcluded = filterItemVM.Modifiers.Contains(
                FilterModiferType.Excluding);
            model.IsCaseSensitive = filterItemVM.Modifiers.Contains
                (FilterModiferType.CaseSensitive);
            model.IsRegularExpression = filterItemVM.Modifiers.Contains(
                FilterModiferType.RegularExpression);
            model.IsLogicOperation = filterItemVM.Modifiers.Contains(
                FilterModiferType.LogicOperation);
            Debug.Assert(!(model.IsRegularExpression && model.IsLogicOperation));

            return model;
        }
    }
}
