using Avalonia.Data.Converters;
using System;
using System.Globalization;
using TextAnalyzer.Models;

namespace TextAnalyzer.Converters
{
    internal class ModifierConverter : IValueConverter
    {
        public object? Convert(
            object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is FilterModiferType modifier)
            {
                switch (modifier)
                {
                    case FilterModiferType.Excluding:
                        return "[!]";

                    case FilterModiferType.CaseSensitive:
                        return "[Aa]";

                    case FilterModiferType.RegularExpression:
                        return "[R]";

                    case FilterModiferType.LogicOperation:
                        return "[&]";

                    case FilterModiferType.Marker:
                        return "[#]";

                    default:
                        break;
                }
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
