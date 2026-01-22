using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AESCryptoTool.Converters
{
    /// <summary>
    /// Converts a collection count (int) to Visibility.
    /// Default (Inverse=true): 0 -> Visible, >0 -> Collapsed. (For Empty States)
    /// Inverse=false: 0 -> Collapsed, >0 -> Visible. (For Content)
    /// </summary>
    public class CountToVisibilityConverter : IValueConverter
    {
        public bool Inverse { get; set; } = true;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count)
            {
                if (Inverse)
                {
                    // For Empty State: Visible if 0
                    return count == 0 ? Visibility.Visible : Visibility.Collapsed;
                }
                else
                {
                    // For Content: Visible if > 0
                    return count > 0 ? Visibility.Visible : Visibility.Collapsed;
                }
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
