using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace HardwareInterfaces
{
	[Serializable]
	public class RegisterMap
	{
		public string DeviceName { get; set; }

		public string DeviceVersion { get; set; }

		public List<Map> Registers { get; set; }

		public SystemData System { get; set; }
	}

	[Serializable]
	public class Map
	{
		[XmlAttribute]
		public string Address
		{
			get { return IntAddress.ToString("X2"); }
			set { IntAddress = Convert.ToByte(value, 16); }
		}

		[XmlAttribute]
		public string Value
		{
			get { return UIntValue.ToString("X2"); }
			set { UIntValue = Convert.ToUInt16(value, 16); }
		}

		[XmlIgnore]
		public byte IntAddress;

		[XmlIgnore]
		public ushort UIntValue;

		public Map()
		{
		}

		public Map(byte address, ushort value)
		{
			this.IntAddress = address;
			this.UIntValue = value;
		}
	}

	[Serializable]
	public class SystemData
	{
		[XmlAttribute]
		public bool Enc;

		[XmlIgnore]
		private string _data;

		public string Data
		{
			get
			{
				_data = string.Empty;
				foreach (var item in Values)
				{
					_data += Obfuscate(item.Key).ToString("X2") + ":" + Obfuscate(item.Value).ToString("X2") + ":";
				}

				return _data;
			}
			set
			{
				Values.Clear();
				_data = value;
				string[] vals = _data.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);

				for (int i = 0; i < vals.Length; i += 2)
				{
					Values.Add(Obfuscate(Convert.ToUInt16(vals[i], 16)), Obfuscate(Convert.ToUInt16(vals[i + 1], 16)));
				}
			}
		}

		[XmlIgnore]
		public Dictionary<ushort, ushort> Values = new Dictionary<ushort, ushort>();

		private ushort Obfuscate(ushort value)
		{
			if (!Enc)
			{
				return value;
			}
			else
			{
				return (ushort)(value ^ 0xA5);
			}
		}
	}
}
