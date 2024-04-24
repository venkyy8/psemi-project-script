using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace PE24103i2cControl.Converters
{
	class MultiRegisterStringValueConverter : IMultiValueConverter
	{
		public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			// Convert the xml string values to integers
			List<int> maskValues = new List<int>();
			foreach (object o in (Array)parameter)
			{
				maskValues.Add(ConvertHexToInt(o.ToString()));
			}

			// How many bits in the LSB
			int numberOfBits = CountSetBits(maskValues[0]);
			int shift = 8 - numberOfBits;
			int lsbMask = maskValues[0];
			int msbMask = maskValues[1];

			int valLsb = (int)double.Parse(values[0].ToString());
			int valMsb = (int)double.Parse(values[1].ToString()) & msbMask;
			valLsb = (valLsb >> shift);
			int val = (valMsb << numberOfBits) + valLsb;
			return string.Format("0x{0}", val.ToString("X"));
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
		{
			List<int> maskValues = new List<int>();
			foreach (object o in (Array)parameter)
			{
				maskValues.Add(ConvertHexToInt(o.ToString()));
			}

			// How many bits in the LSB
			int numberOfBits = CountSetBits(maskValues[0]);
			int shift = 8 - numberOfBits;
			int lsbMask = maskValues[0];

			double d = System.Convert.ToDouble(value);
			int val = (int)d;
			byte valLsb = (byte)(((val << shift) & lsbMask));
			byte valMsb = (byte)(val >> numberOfBits & 0xFF);
			return new object[] { valLsb, valMsb };

			//double d = System.Convert.ToDouble(value);
			//int val = (int)d;
			//int valLsb = (val & 0xF) << 4;
			//int valMsb = (val >> 4) & 0xFF;
			//return new object[] { valLsb, valMsb };
		}

		private int ConvertHexToInt(string hexValue)
		{
			string hex = (hexValue.ToUpper().Contains("0X")) ? hexValue.Substring(2) : hexValue;
			return System.Convert.ToInt32(hex, 16);
		}

		private int CountSetBits(int value)
		{
			return System.Convert.ToString(value, 2).ToCharArray().Count(c => c == '1');
		}
	}
}
