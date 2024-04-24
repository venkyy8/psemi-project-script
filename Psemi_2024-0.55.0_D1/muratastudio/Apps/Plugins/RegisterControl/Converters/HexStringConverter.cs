using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace RegisterControl.Converters
{
	public class HexStringConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			int regVal = int.Parse(value.ToString());
            //var format = regVal > 255 ? "X4" : "X2";
            return regVal.ToString(parameter.ToString());
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return value.ToString();
		}
	}
}
