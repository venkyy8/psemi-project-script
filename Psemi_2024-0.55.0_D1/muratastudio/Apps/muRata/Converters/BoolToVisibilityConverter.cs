using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using muRata.ViewModel;
using System.Windows;

namespace muRata.Converters
{
	public class BoolToVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (bool.Parse(value.ToString()))
			{
				return Visibility.Visible;
			}
			else
			{
				return Visibility.Collapsed;
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return null;
		}
	}
}
