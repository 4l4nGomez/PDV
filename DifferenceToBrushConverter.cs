using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace BakeryPOS
{
    public class DifferenceToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int diff)
            {
                if (diff < 0) return Brushes.Red;
                if (diff > 0) return Brushes.Green;
            }
            if (value is decimal financial && financial != 0)
            {
                if (financial < 0) return Brushes.Red;
                if (financial > 0) return Brushes.Green;
            }
            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
