﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace muRata.Converters
{
	public class AdapterInfoConverter : IMultiValueConverter
	{
		public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			values[0] = (values[0].ToString().Contains("Unset")) ? "Not Available" : values[0];
			values[1] = (values[1].ToString().Contains("Unset")) ? "Not Available" : values[1];
			values[2] = (values[2].ToString().Contains("Unset")) ? "Not Available" : values[2];

			return string.Format("Adapter: {0}\nVersion: {1}\nS/N: {2}", values[0], values[1], values[2]);
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
		{
			return null;
		}
	}

	public class AdapterNameConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return (string.IsNullOrEmpty(value.ToString())) ? "Not Available" : value.ToString();
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return null;
		}
	}
}