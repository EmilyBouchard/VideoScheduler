using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace VideoScheduler.Presentation.WPF.Converters;

/// <summary>
/// Converts null to Visibility.Visible, and non-null to Visibility.Collapsed.
/// Used to show placeholders when content is missing.
/// </summary>
[ValueConversion(typeof(object), typeof(Visibility))]
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value == null ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
