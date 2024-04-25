using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Mpq7920Control.Converters
{
	public class ValueToPercentConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			decimal val;
			decimal param = 4095;
			if (parameter != null)
			{
				param = decimal.Parse(parameter.ToString());
			}

			if(decimal.TryParse(value.ToString(), out val))
			{
				val = (val / param) * 100;
			}
			return val.ToString("f3") + "%";
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return string.Empty;
		}
	}
}
