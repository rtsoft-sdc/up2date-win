using System;
using System.Globalization;
using System.Windows.Data;

namespace Up2dateConsole.Helpers
{

    public class EnumToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo language)
        {
            return !(value is Enum) ? false : (object)value.Equals(parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is bool) || parameter == null) return Binding.DoNothing;

            return value.Equals(true) ? parameter : Binding.DoNothing;
        }
    }
}