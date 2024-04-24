using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace ARCxCCxxControl.RegisterControl
{
	public class DataToColorValueConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			int val = int.Parse(value.ToString());
			uint mask = uint.Parse(parameter.ToString());

			if ((val & mask) == mask)
			{
				return new SolidColorBrush(Color.FromArgb(0xff, 0x75, 0xdf, 0x4a));
			}
			else
			{
				return new SolidColorBrush(Color.FromArgb(0xff, 0xdd, 0xdd, 0xdd));
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
