using System;
using System.Globalization;
using ApplicationCore.Enums;
using Avalonia;
using Avalonia.Data.Converters;

namespace Universal_x86_Tuning_Utility.Resources.Converters;

public class GameTypeToStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is GameType gameType)
        {
            return gameType switch
            {
                GameType.Custom => "Manually added game",
                _ => gameType.ToString()
            };
        }

        return string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        AvaloniaProperty.UnsetValue;
}