using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace CsvToXml
{
	class Program
	{
		static void Main(string[] args)
		{
			var regs = new Registers();

			using (StreamReader sr = File.OpenText(args[0]))
			{
				regs.I2CRegisters = new List<Register>();
				int n = 0;
				while (!sr.EndOfStream)
				{
					string[] inLine = sr.ReadLine().Split(',');
					var reg = new Register
					{
						Name = string.IsNullOrEmpty(inLine[0]) ? string.Format("Undefined{0}", n++) : inLine[0],
						Address = inLine[2].ToUpper(),
						DataType = "H",
						Size = "1",
						Format = "X2",
						Unit = "Hex",
						ReadOnly = inLine[1].ToLower() == "rw" ? "False" : "True",
						Private = (inLine[3].ToLower() == "customer") ? "False" : "True",
						Description = "See datasheet"
					};
					reg.Bit = new List<Bit>();

					int mask = 0x0100;
					for (int i = 0; i < 8; i++)
					{
						mask = mask >> 1;
						Bit b = new Bit
						{
							Mask = "0x" + (mask).ToString("X4"),
							Name = string.IsNullOrEmpty(inLine[4 + i]) ? "Reserved" : CleanString(inLine[4 + i]),
							Description = "See datasheet"
						};
						reg.Bit.Add(b);
					}
					regs.I2CRegisters.Add(reg);
				}
			}

			var filename = args[0].Replace(".csv", ".xml");
			XmlSerializeRegisterMap(filename, regs);
		}

		static void XmlSerializeRegisterMap(string fileName, Registers map)
		{
			FileStream fs = new FileStream(fileName, FileMode.Create);
			XmlSerializer formatter = new XmlSerializer(typeof(Registers), new Type[] { typeof(Register) , typeof(Bit) });
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
		public List<Register> I2CRegisters { get; set; }
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
