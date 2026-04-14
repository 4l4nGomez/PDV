using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace BakeryPOS
{
    public class EqualityToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null) return Visibility.Collapsed;
            
            bool isEqual = value.ToString() == parameter.ToString();
            
            // Si el parámetro empieza con "!", invertimos la lógica
            if (parameter.ToString().StartsWith("!"))
            {
                isEqual = value.ToString() != parameter.ToString().Substring(1);
            }

            return isEqual ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
