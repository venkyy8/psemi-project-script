using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace RegisterControl.Converters
{
	public class HexToValueConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			int decValue = int.Parse(value.ToString(), System.Globalization.NumberStyles.HexNumber);			
			return decValue.ToString();
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
