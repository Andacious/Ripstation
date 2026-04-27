using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace Ripstation.Converters;

/// <summary>
/// Converts a 0-100 progress value to a star-sized GridLength for use in a two-column
/// Grid that simulates a ProgressBar (ProgressBar control crashes in WinUI 3 WinAppSDK 1.8).
/// ConverterParameter="track" → returns the remaining (empty track) portion.
/// No parameter → returns the filled portion.
/// </summary>
public class ProgressWidthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        double pct = System.Convert.ToDouble(value) / 100.0;
        pct = Math.Clamp(pct, 0.0, 1.0);
        bool isTrack = parameter is string s && s == "track";
        double stars = isTrack ? (1.0 - pct) : pct;
        // Minimum star value avoids zero-size column which can cause layout issues
        stars = Math.Max(stars, 0.0001);
        return new GridLength(stars, GridUnitType.Star);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
