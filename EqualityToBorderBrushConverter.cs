using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace BakeryPOS
{
    public class EqualityToBorderBrushConverter : IMultiValueConverter, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null) return Brushes.Transparent;
            return value.ToString() == parameter.ToString() ? (Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#10B981") : Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        // MultiValue version for dynamic updates
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2 || values[0] == null || values[1] == null) 
                return Brushes.Transparent;
                
            return values[0].ToString() == values[1].ToString() 
                ? (Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#10B981") 
                : Brushes.Transparent;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
