using DeviceAccess;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace PE26100Control.Converters
{
	public class TransformDisplayConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			string[] parameters = parameter.ToString().Split('|');
			decimal evalValue = 0;
			int regVal;

			string hexToDecimal = value.ToString();
			hexToDecimal = hexToDecimal.Replace("0x", "");

			if (int.TryParse(hexToDecimal, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out regVal))
			{
				evalValue = Evaluator.EvalToDecimal(parameters[0].ToString().ToUpper().Replace("X", regVal.ToString(CultureInfo.InvariantCulture)));
			}
			return evalValue.ToString(parameters[1]) + parameters[2];
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return string.Empty;
		}
	}
}
