using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using muRata.ViewModel;

namespace muRata.Converters
{
	public class PollingStateToBoolConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			PollState state = (PollState)value;

			if (state == PollState.Off)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return null;
		}
	}
}
