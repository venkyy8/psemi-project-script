using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using PE24106Control.ViewModel;
using System.Windows;

namespace PE24106Control.Converters
{
	public class InverseBoolToVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			bool b = bool.Parse(value.ToString());
			if (b)
			{
				return Visibility.Collapsed;
			}
			else
			{
				return Visibility.Visible;
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return value;
		}
	}
}
