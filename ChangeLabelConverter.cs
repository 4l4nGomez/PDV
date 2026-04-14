using System;
using System.Globalization;
using System.Windows.Data;

namespace BakeryPOS
{
    public class ChangeLabelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal amount)
            {
                return amount >= 0 ? "CAMBIO A ENTREGAR" : "MONTO FALTANTE";
            }
            return "CAMBIO";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
