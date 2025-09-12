using System;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;

namespace OGS.Converters;

public class ValueInSetConverter : IValueConverter
{
    public string Delimiters { get; set; } = ",|";

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (parameter is null)
        {
            return false;
        }

        string valueString = value?.ToString() ?? string.Empty;
        string parameterString = parameter.ToString() ?? string.Empty;

        char[] delimiterChars = Delimiters.ToCharArray();
        var allowed = parameterString
            .Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return allowed.Contains(valueString);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}


