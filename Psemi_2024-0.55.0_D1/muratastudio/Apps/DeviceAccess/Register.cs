using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace DeviceAccess
{
	public partial class Register : INotifyPropertyChanged
	{
		#region Private Members

		private double _lastReadValue = 0;
		private DateTime _lastReadValueTimestamp = DateTime.MinValue;
		private bool _lastReadValueError = false;
		private object _lastReadRawValue = 0;
		private DateTime _lastReadRawValueTimestamp = DateTime.MinValue;
		private bool _lastReadRawValueError = false;

		#endregion

		#region Public Readonly Fields

		public readonly string ID = "";
		public readonly uint Address = 0x00;
		public readonly string DataType = "";
		public readonly uint Size = 2;
		public readonly string Name = "";
		public readonly string DisplayName = "";
		public readonly string Description = "";
		public readonly string Format = "0";
		public readonly string Unit = "";
		public readonly bool Private = false;
		public readonly bool ReadOnly = false;
		public readonly string LoadFormula = "x";
		public readonly string StoreFormula = "x";
		public readonly List<Bit> Bits = new List<Bit>();
		public readonly TimeSpan REGISTER_CACHE_TIMEOUT = new TimeSpan(0, 0, 0, 0, 500);

		#endregion

		#region Public Properties

		public double LastReadValue
		{
			get { return _lastReadValue; }
			set
			{
				_lastReadValue = value;
				OnPropertyChanged("LastReadValue");
			}
		}

		public DateTime LastReadValueTimestamp
		{
			get { return _lastReadValueTimestamp; }
			set
			{
				_lastReadValueTimestamp = value;
				OnPropertyChanged("LastReadValueTimestamp");
			}
		}

		public bool LastReadValueError
		{
			get { return _lastReadValueError; }
			set
			{
				_lastReadValueError = value;
				OnPropertyChanged("LastReadValueError");
			}
		}

		public object LastReadRawValue
		{
			get { return _lastReadRawValue; }
			set
			{
				_lastReadRawValue = value;
				OnPropertyChanged("LastReadRawValue");
			}
		}

		public DateTime LastReadRawValueTimestamp
		{
			get { return _lastReadRawValueTimestamp; }
			set
			{
				_lastReadRawValueTimestamp = value;
				OnPropertyChanged("LastReadRawValueTimestamp");
			}
		}

		public bool LastReadRawValueError
		{
			get { return _lastReadRawValueError; }
			set
			{
				_lastReadRawValueError = value;
				OnPropertyChanged("LastReadRawValueError");
			}
		} 

		#endregion

		#region Constructor

		public Register(XElement register)
		{
			bool tempbool;
			uint tempInteger;
			byte tempByte;

			//Store Information About the Register from the XML Data
			if (register.Attribute("Name") != null)
			{
				Name = register.Attribute("Name").Value;
			}

			if (register.Attribute("Address") != null)
			{
				if (register.Attribute("Address").Value.Length >= 2)
				{
					if (register.Attribute("Address").Value.Substring(0, 2).ToUpper() == "0X")
					{
						if (byte.TryParse(register.Attribute("Address").Value.Substring(2), NumberStyles.HexNumber, null, out tempByte))
						{
							Address = System.Convert.ToByte(register.Attribute("Address").Value, 16);
						}
					}
					else
					{
						if (byte.TryParse(register.Attribute("Address").Value, out tempByte))
						{
							Address = System.Convert.ToByte(register.Attribute("Address").Value);
						}
					}
				}
				else
				{
					if (byte.TryParse(register.Attribute("Address").Value, out tempByte))
					{
						Address = System.Convert.ToByte(register.Attribute("Address").Value);
					}
				}
			}

			if (register.Attribute("DataType") != null)
			{
				DataType = register.Attribute("DataType").Value.ToUpper();
			}

			if (register.Attribute("Size") != null)
			{
				if (uint.TryParse(register.Attribute("Size").Value, out tempInteger))
				{
					Size = System.Convert.ToUInt32(register.Attribute("Size").Value);
				}
			}

			if (register.Attribute("DisplayName") != null)
			{
				DisplayName = register.Attribute("DisplayName").Value;
			}
			else if (register.Attribute("Name") != null)
			{
				DisplayName = Name;
			}

			ID = DisplayName.ToUpper().Replace(" ", string.Empty).Replace("-", string.Empty).Replace("_", string.Empty);

			if (register.Attribute("Description") != null)
			{
				Description = register.Attribute("Description").Value;
			}

			if (register.Attribute("Format") != null)
			{
				Format = register.Attribute("Format").Value;
			}

			if (register.Attribute("Unit") != null)
			{
				Unit = register.Attribute("Unit").Value;
			}

			if (register.Attribute("Private") != null)
			{
				if (bool.TryParse(register.Attribute("Private").Value, out tempbool))
				{
					Private = System.Convert.ToBoolean(register.Attribute("Private").Value);
				}
			}

			if (register.Attribute("ReadOnly") != null)
			{
				if (bool.TryParse(register.Attribute("ReadOnly").Value, out tempbool))
				{
					ReadOnly = System.Convert.ToBoolean(register.Attribute("ReadOnly").Value);
				}
			}

			//Process Any Register Child Nodes
			foreach (XElement registerChildNode in register.DescendantNodes())
			{
				switch (registerChildNode.Name.ToString())
				{
					case "Load":
						LoadFormula = registerChildNode.Value;
						break;
					case "Store":
						StoreFormula = registerChildNode.Value;
						break;
					case "Bit":
						Bits.Add(new Bit(ID, registerChildNode));
						break;
				}
			}
		} 

		#endregion

		#region Public Methods

		public Register(Register deepCopyRegister, bool includePrivateFields = false)
		{
			ID = deepCopyRegister.ID;
			Address = deepCopyRegister.Address;
			DataType = deepCopyRegister.DataType;
			Size = deepCopyRegister.Size;
			Name = deepCopyRegister.Name;
			DisplayName = deepCopyRegister.DisplayName;
			Description = deepCopyRegister.Description;
			Format = deepCopyRegister.Format;
			Unit = deepCopyRegister.Unit;
			Private = deepCopyRegister.Private;
			ReadOnly = deepCopyRegister.ReadOnly;
			LoadFormula = deepCopyRegister.LoadFormula;
			StoreFormula = deepCopyRegister.StoreFormula;

			LastReadValue = deepCopyRegister.LastReadValue;
			LastReadValueTimestamp = deepCopyRegister.LastReadValueTimestamp;
			LastReadValueError = deepCopyRegister.LastReadValueError;
			LastReadRawValue = deepCopyRegister.LastReadRawValue;
			LastReadRawValueTimestamp = deepCopyRegister.LastReadRawValueTimestamp;
			LastReadRawValueError = deepCopyRegister.LastReadRawValueError;

			Bits = new List<Bit>();

			foreach (Bit field in deepCopyRegister.Bits)
			{
				if (includePrivateFields)
				{
					Bits.Add(field);
				}
				else
				{
					if (!field.Private)
					{
						Bits.Add(field);
					}
					else
					{
						Bits.Add(new Bit(field, true));
					}
				}
			}
		} 

		#endregion

		#region INotificationPropertyChanged Interface

		public event PropertyChangedEventHandler PropertyChanged;

		public void OnPropertyChanged([CallerMemberName] string propertyName = "")
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		} 

		#endregion
	}
}
