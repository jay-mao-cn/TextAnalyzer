using System;
using System.Globalization;
using System.Text;
using Avalonia.Data.Converters;

namespace TextAnalyzer.Converters;

internal class EncodingSelectionConverter : IValueConverter
{
    public object? Convert(
        object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        try
        {
            var encodingName = (parameter as string)!.ToLower();
            var encoding = value as Encoding;
            if (encodingName == "default")
            {
                return encoding!.CodePage == CultureInfo.CurrentCulture.TextInfo.ANSICodePage;
            }
            else
            {
                return encoding!.WebName == encodingName
                       || encoding.EncodingName.ToLower() == encodingName;
            }
        }
        catch (Exception)
        {
            return false;
        }
    }

    public object? ConvertBack(
        object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}