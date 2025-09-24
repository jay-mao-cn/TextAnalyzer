using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace TextAnalyzer.Converters
{
    internal class ColorToBrushConverter : IValueConverter
    {
        public object? Convert(
            object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is Color color)
            {
                return new SolidColorBrush(color);
            }

            return null;
        }

        public object? ConvertBack(
            object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
