using DeviceAccess;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using PE26100Control.UIControls;

namespace PE26100Control.Converters
{
	class MultiRegisterStringValueConverter : IMultiValueConverter
	{
        private string hexToDecimal;

        public decimal InputResValue { get;  set; }
        public decimal OutputResValue { get;  set; }

        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (values.Length == 3)
			{
				
				string[] parameters = parameter.ToString().Split('|');
				decimal evalValue1 = 0;
				decimal evalValue = 0;
				int regVal;
				hexToDecimal = values[0].ToString();
				hexToDecimal = hexToDecimal.Replace("0x", "");

				if (int.TryParse(hexToDecimal, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out regVal))
				{
					if (parameters[0] == "x*2")
					{
						regVal = TempValues(parameters, regVal);
					}
					else if(parameters[0] == "x*0.01" || parameters[0] == "x*0.02" || parameters[0] == "x*0.005")
                    {
						regVal = Int32.Parse(hexToDecimal, System.Globalization.NumberStyles.HexNumber);
					}
					else if (parameters[0] == "(0.00+(0.00005/y/0.001))" || parameters[0] == "(0.00+(0.0001/k/0.001))")
					{
						regVal = Int32.Parse(hexToDecimal, System.Globalization.NumberStyles.HexNumber);

						if (InputResValue != 0 && OutputResValue != 0)
						{
							if (parameters[0] == "(0.00+(0.0001/k/0.001))")
							{
								evalValue = Evaluator.EvalToDecimal(parameters[0].ToString().ToUpper().Replace("K", OutputResValue.ToString(CultureInfo.InvariantCulture)));
							}
							else
							{
								evalValue = Evaluator.EvalToDecimal(parameters[0].ToString().ToUpper().Replace("Y", InputResValue.ToString(CultureInfo.InvariantCulture)));
							}
                        }
                        else
                        {
							evalValue = 0;
                        }
						evalValue1 = regVal * evalValue;
					}
				}
				if (parameters[0] == "(0.00+(0.00005/y/0.001))" || parameters[0] == "(0.00+(0.0001/k/0.001))")
				{
					return evalValue1.ToString(parameters[1]) + parameters[2];
				}
				else
				{
					evalValue = Evaluator.EvalToDecimal(parameters[0].ToString().ToUpper().Replace("X", regVal.ToString(CultureInfo.InvariantCulture)));
					return evalValue.ToString(parameters[1]) + parameters[2];
				}
			}
			else
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

		private static int TempValues(string[] parameters, int regVal)
		{
			if (regVal == 236) { regVal = -20; }
			else if (regVal == 237) { regVal = -19; }
			else if (regVal == 238) { regVal = -18; }
			else if (regVal == 239) { regVal = -17; }
			else if (regVal == 240) { regVal = -16; }
			else if (regVal == 241) { regVal = -15; }
			else if (regVal == 242) { regVal = -14; }
			else if (regVal == 243) { regVal = -13; }
			else if (regVal == 244) { regVal = -12; }
			else if (regVal == 245) { regVal = -11; }
			else if (regVal == 246) { regVal = -10; }
			else if (regVal == 247) { regVal = -9; }
			else if (regVal == 248) { regVal = -8; }
			else if (regVal == 249) { regVal = -7; }
			else if (regVal == 250) { regVal = -6; }
			else if (regVal == 251) { regVal = -5; }
			else if (regVal == 252) { regVal = -4; }
			else if (regVal == 253) { regVal = -3; }
			else if (regVal == 254) { regVal = -2; }
			else if (regVal == 255) { regVal = -1; }
			if (regVal > 90 && regVal < 237) { regVal = 0; }

			return regVal;
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
