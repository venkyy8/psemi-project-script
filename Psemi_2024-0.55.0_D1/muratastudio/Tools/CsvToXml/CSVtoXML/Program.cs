using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace CSVtoXML
{
	class Program
	{
		private const string colon = ":";
		static int size = 0;
		static Dictionary<int, string> bitWithSameName = new Dictionary<int, string>();
		static int count = 0;
		//static string[] fullRegisterInfo = new string[100];
		static void Main(string[] args)
		{
			string input = @"C:\Users\Admin\OneDrive - Mirafra Software Technologies Pvt Ltd\Desktop\PE24104.csv";
			var regs = new Registers();
			Addbits();

			using (StreamReader sr = File.OpenText(input))
			{
				regs.I2CRegister = new List<Register>();
				int n = 0;

				string[] fullRegisterInfo = {};

				while (!sr.EndOfStream)
				{
					string[] inLine = sr.ReadLine().Split(',');
					

					if (string.IsNullOrEmpty(inLine[2]) && string.IsNullOrEmpty(inLine[3]) && string.IsNullOrEmpty(inLine[4]) && !string.IsNullOrEmpty(inLine[10]))
					{
						
						fullRegisterInfo = ConcatArrays(fullRegisterInfo,inLine );
						

						continue;
					}

					if (!string.IsNullOrEmpty(inLine[2]) && !string.IsNullOrEmpty(inLine[4]) && (inLine[4]).StartsWith("0x"))
					{
						if (count == 0)
						{
							fullRegisterInfo = inLine;
							bitWithSameName.Clear();
							count++;
							continue;
						}
						else
						{
							string[] currentInline = CreateRegisterXML(regs, n, fullRegisterInfo, inLine);
							fullRegisterInfo = inLine;
							continue;
						}
					}
					if (string.IsNullOrEmpty(inLine[2]) && !string.IsNullOrEmpty(inLine[4]) && (inLine[4]).StartsWith("0x"))
					{
						if (count == 0)
						{
							fullRegisterInfo = inLine;
							bitWithSameName.Clear();
							count++;
							continue;
						}
						else
						{
							string[] currentInline = CreateRegisterXML(regs, n, fullRegisterInfo, inLine);
							fullRegisterInfo = inLine;
							continue;
						}
					}
				}
			}

			var filename = input.Replace(".csv", ".xml");
			XmlSerializeRegisterMap(filename, regs);
		}

		private static string[] CreateRegisterXML(Registers regs, int n, string[] inLine, string[] currentInline)
		{
			if (!string.IsNullOrEmpty(inLine[7]))
			{
				size = Convert.ToInt32(inLine[7]);

				if (size % 8 == 0)
				{
					size = size / 8;
				}
				else
				{
					size = (size / 8) + 1;
				}
			}

			var reg = new Register
			{
				Name = string.IsNullOrEmpty(inLine[2]) ? string.Format("Undefined{0}", n++) : inLine[2],
				Address = inLine[4].ToUpper(),
				DataType = "H",
				Size = size.ToString(),
				Format = "X3",
				Unit = "Hex",
				ReadOnly = "False",
				Private = "True",
				Description = "See datasheet"
			};

			if (bitWithSameName.Any()) bitWithSameName.Clear();
			Addbits();
			int itemPosition = 0;
			foreach (string s in inLine)
			{
				if (s.StartsWith("Bits"))
				{
					var bitRange = s.Split(':');
					bitRange[0] = bitRange[0].Substring(5, bitRange[0].Length - 5);
					int rangeWithSameName = Convert.ToInt32(bitRange[0]) - Convert.ToInt32(bitRange[1]) + 1;
					var bitName = inLine[itemPosition + 2];

					//bitWithSameName[Convert.ToInt32(bitRange[1])] = bitName;

					int count = 0;
					for (int i = 1; i <= rangeWithSameName; i++)
					{
						if (!string.IsNullOrEmpty(bitRange[1]))
						{
							bitWithSameName[Convert.ToInt32(bitRange[1]) + count] = bitName + i;
							count++;
						}
					}
				}
				else if (s.StartsWith("Bit"))
				{
					var bitValue = s.Substring(4, s.Length - 4);
					var bitName = inLine[itemPosition + 2];
					bitWithSameName[Convert.ToInt32(bitValue)] = bitName;
					count++;
				}

				itemPosition++;
			}

			reg.Bit = new List<Bit>();

			int mask = 0x10000;
			for (int i = 0; i < 16; i++)
			{
				var a = bitWithSameName.TryGetValue(15 - i, out string value);

				mask = mask >> 1;
				Bit b = new Bit
				{
					Mask = "0x" + (mask).ToString("X4"),
					Name = string.IsNullOrEmpty(value) ? "Reserved" : value,
					Description = "See datasheet"
				};
				reg.Bit.Add(b);
			}
			regs.I2CRegister.Add(reg);
			return currentInline;
		}

		public static T[] ConcatArrays<T>(params T[][] list)
		{

			var result = new T[list.Sum(a => a.Length)];
			int offset = 0;
			for (int x = 0; x < list.Length; x++)
			{
				list[x].CopyTo(result, offset);
				offset += list[x].Length;
			}
			return result;
		}
		private static void Addbits()
		{
			if (size == 1)
			{
				bitWithSameName.Add(0, "");
				bitWithSameName.Add(1, "");
				bitWithSameName.Add(2, "");
				bitWithSameName.Add(3, "");
				bitWithSameName.Add(4, "");
				bitWithSameName.Add(5, "");
				bitWithSameName.Add(6, "");
				bitWithSameName.Add(7, "");
			}
			else if (size == 2)
			{
				bitWithSameName.Add(0, "");
				bitWithSameName.Add(1, "");
				bitWithSameName.Add(2, "");
				bitWithSameName.Add(3, "");
				bitWithSameName.Add(4, "");
				bitWithSameName.Add(5, "");
				bitWithSameName.Add(6, "");
				bitWithSameName.Add(7, "");
				bitWithSameName.Add(8, "");
				bitWithSameName.Add(9, "");
				bitWithSameName.Add(10, "");
				bitWithSameName.Add(11, "");
				bitWithSameName.Add(12, "");
				bitWithSameName.Add(13, "");
				bitWithSameName.Add(14, "");
				bitWithSameName.Add(15, "");

			}
		}

		private static void AssignBitNames(string[] inLine, ref string[] bitRange)
		{
			string bitName = inLine[12];
			string bitValue = inLine[10];

			if (bitValue.StartsWith("Bits"))
			{
				bitRange = bitValue.Split(':');
				bitRange[0] = bitRange[0].Substring(5, bitRange[0].Length - 5);
				int rangeWithSameName = Convert.ToInt32(bitRange[0]) - Convert.ToInt32(bitRange[1]) + 1;
				int count = 0;
				for (int i = 1; i <= rangeWithSameName; i++)
				{
					if (!string.IsNullOrEmpty(bitRange[1]))
					{
						bitWithSameName.Add((Convert.ToInt32(bitRange[1]) + count), bitName + i);
						count++;
					}
				}
			}
			else if (bitValue.StartsWith("Bit"))
			{
				bitRange[0] = bitValue.Substring(4, bitValue.Length - 4);
				foreach (string bit in bitRange)
				{
					if (!String.IsNullOrEmpty(bit))
					{
						bitWithSameName[Convert.ToInt32(bit)] = bitName;
					}
				}
			}

		}

		static void XmlSerializeRegisterMap(string fileName, Registers map)
		{
			FileStream fs = new FileStream(fileName, FileMode.Create);
			XmlSerializer formatter = new XmlSerializer(typeof(Registers), new Type[] { typeof(Register), typeof(Bit) });
			try
			{
				formatter.Serialize(fs, map);
			}
			catch (InvalidOperationException e)
			{
				Console.WriteLine("Failed to serialize register map.\r\nReason: " + e.Message);
				Console.ReadLine();
			}
			finally
			{
				fs.Close();
			}
		}

		static string CleanString(string value)
		{
			value = value.Replace("<", "_");
			value = value.Replace(">", "");
			return value;
		}
	}

	[Serializable]
	public class Registers
	{
		public List<Register> I2CRegister { get; set; }
	}

	[Serializable]
	public class Register
	{
		[XmlAttribute]
		public string Name { get; set; }
		[XmlAttribute]
		public string Address { get; set; }
		[XmlAttribute]
		public string DataType { get; set; }
		[XmlAttribute]
		public string Size { get; set; }
		[XmlAttribute]
		public string Format { get; set; }
		[XmlAttribute]
		public string Unit { get; set; }
		[XmlAttribute]
		public string ReadOnly { get; set; }
		[XmlAttribute]
		public string Private { get; set; }
		[XmlAttribute]
		public string Description { get; set; }

		[XmlElement("Bit")]
		public List<Bit> Bit { get; set; }
	}

	[Serializable]
	public class Bit
	{
		[XmlAttribute]
		public string Mask { get; set; }
		[XmlAttribute]
		public string Name { get; set; }
		[XmlAttribute]
		public string Description { get; set; }
	}
}
