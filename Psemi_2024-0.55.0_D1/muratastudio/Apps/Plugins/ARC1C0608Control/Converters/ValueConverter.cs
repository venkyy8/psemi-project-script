using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using ARC1C0608Control.ViewModel;

namespace ARC1C0608Control.Converters
{
	public class ValueConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			int val = (int)double.Parse(value.ToString());
			ListObject lo = parameter as ListObject;
			uint mask = uint.Parse(lo.Mask);
			int newVal = (val & (int)mask);

			// Right shift mask until the first set bit is in the first position
			for (int i = 0; i < 8; i++)
			{
				if ((mask & 1) == 1)
				{
					break;
				}
				else
				{
					newVal = (newVal >> 1);
					mask = (mask >> 1);
				}
			}

			return newVal;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return value;
		}
	}
}
