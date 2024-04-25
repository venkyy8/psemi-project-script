﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace PE24103Control.Converters
{
	public class DecimalToHex2Converter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			string val = int.Parse(value.ToString()).ToString("X2");
			return val;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return byte.Parse(value.ToString(), System.Globalization.NumberStyles.HexNumber);
		}
	}
}