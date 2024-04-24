using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows;
using ARC1C0608Control.ViewModel;

namespace ARC1C0608Control.Converters
{
	public class BoolToVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			bool b = bool.Parse(value.ToString());
			if (b)
			{
				return Visibility.Visible;
			}
			else
			{
				return Visibility.Hidden;
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return value;
		}
	}
}
