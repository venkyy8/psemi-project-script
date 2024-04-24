using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace PE24103i2cControl.RegisterControl
{
	public class HexValueConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			int regVal = int.Parse(value.ToString());
			return string.Format("0x{0}", regVal.ToString(parameter.ToString()));
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			string hex = value.ToString();
			if (hex.StartsWith("0x"))
			{
				hex = hex.Substring(2);
			}
			
			byte regVal = byte.Parse(hex, System.Globalization.NumberStyles.HexNumber);
			return regVal;
		}
	}
}
