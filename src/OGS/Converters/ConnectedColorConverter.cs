using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace OGS.Converters;

public sealed class ConnectedColorConverter : IValueConverter
{
    public static readonly ConnectedColorConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? Brushes.Green : Brushes.Red;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
