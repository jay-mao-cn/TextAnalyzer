﻿using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace TextAnalyzer.Converters
{
    internal class EqualityConverter : IValueConverter
    {
        public object? Convert(
            object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null && parameter == null)
                return true;

            return value?.Equals(parameter) == true;
        }

        public object? ConvertBack(
            object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
