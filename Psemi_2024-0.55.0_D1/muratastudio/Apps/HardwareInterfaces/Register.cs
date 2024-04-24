using LiveCharts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace HardwareInterfaces
{
	public partial class Register : INotifyPropertyChanged
	{
		#region Private Members

		private double _editedValue = 0;
		private double _lastReadValue = 0;
		private double _lastReadValueWithoutFormula = 0;
		private string _lastReadString = "";
		private object _lastReadRawValue = 0;
		private bool _lastReadValueError = false;
		private bool _lastReadRawValueError = false;
		private DateTime _lastReadValueTimestamp = DateTime.MinValue;
		private DateTime _lastReadRawValueTimestamp = DateTime.MinValue;
		private List<string> _byteValues = new List<string>();
		private int _sampleBuffer = 20;
		private double _axisMaxX;
		private double _axisMinX;
		private double _axisMaxY;
		private double _axisMinY;
		#endregion

		#region Public Readonly Fields

		private string _id = "";
		private uint _address = 0x00;
		private int _signedBit = 0;
		private string _dataType = "";
		private string _access = "";
		private uint _size = 2;
		private string _name = "";
		private string _displayName = "";
		private string _description = "";
		private string _format = "0";
		private string _unit = "";
		private bool _private = false;
		private bool _readOnly = false;
		private string _loadFormula = "x";
		private string _storeFormula = "x";
		private List<Bit> _bits = new List<Bit>();
		private IDevice _device;
        private byte[] _lockBitMask = new byte[] { 0xff, 0xff }; // LSB - MSB
		private bool _triggeredRead = false;

		public readonly TimeSpan REGISTER_CACHE_TIMEOUT = new TimeSpan(0, 0, 0, 0, 005);

		#endregion

		#region Public Properties

		public List<string> ByteValues
		{
			get { return _byteValues; }

			internal set
			{
				if (_byteValues != value)
				{
					_byteValues = value;
					OnPropertyChanged("ByteValues");
				}
			}
		}

		public string ID
		{
			get { return _id; }

			internal set
			{
				if (_id != value)
				{
					_id = value;
					OnPropertyChanged("ID");
				}
			}
		}

		public uint Address
		{
			get { return _address; }

			internal set
			{
				if (_address != value)
				{
					_address = value;
					OnPropertyChanged("Address");
				}
			}
		}

		public string DataType
		{
			get { return _dataType; }

			internal set
			{
				if (_dataType != value)
				{
					_dataType = value;
					OnPropertyChanged("DataType");
				}
			}
		}

		public int SignedBit
		{
			get { return _signedBit; }

			internal set
			{
				if (_signedBit != value)
				{
					_signedBit = value;
					OnPropertyChanged("SignedBit");
				}
			}
		}

		public string Access
		{
			get { return _access; }

			internal set
			{
				if (_access != value)
				{
					_access = value;
					OnPropertyChanged("Access");
				}
			}
		}

		public uint Size
		{
			get { return _size; }

			internal set
			{
				if (_size != value)
				{
					_size = value;
					OnPropertyChanged("Size");
				}
			}
		}

		public string Name
		{
			get { return _name; }

			internal set
			{
				if (_name != value)
				{
					_name = value;
					OnPropertyChanged("Name");
				}
			}
		}

		public string DisplayName
		{
			get { return _displayName; }

			internal set
			{
				if (_displayName != value)
				{
					_displayName = value;
					OnPropertyChanged("DisplayName");
				}
			}
		}

		public string Description
		{
			get { return _description.Replace("\\n", Environment.NewLine); }

			internal set
			{
				if (_description != value)
				{
					_description = value;
					OnPropertyChanged("Description");
				}
			}
		}

		public string Format
		{
			get { return _format; }

			internal set
			{
				if (_format != value)
				{
					_format = value;
					OnPropertyChanged("Format");
				}
			}
		}

		public string Unit
		{
			get { return _unit; }

			internal set
			{
				if (_unit != value)
				{
					_unit = value;
					OnPropertyChanged("Unit");
				}
			}
		}

		public bool Private
		{
			get { return _private; }

			internal set
			{
				if (_private != value)
				{
					_private = value;
					OnPropertyChanged("Private");
				}
			}
		}

		public bool ReadOnly
		{
			get { return _readOnly; }

			internal set
			{
				if (_readOnly != value)
				{
					_readOnly = value;
					OnPropertyChanged("ReadOnly");
				}
			}
		}

		public string LoadFormula
		{
			get { return _loadFormula; }

			internal set
			{
				if (_loadFormula != value)
				{
					_loadFormula = value;
					OnPropertyChanged("LoadFormula");
				}
			}
		}

		public string StoreFormula
		{
			get { return _storeFormula; }

			internal set
			{
				if (_storeFormula != value)
				{
					_storeFormula = value;
					OnPropertyChanged("StoreFormula");
				}
			}
		}

		public List<Bit> Bits
		{
			get { return _bits; }

			internal set
			{
				if (_bits != value)
				{
					_bits = value;
					OnPropertyChanged("Bits");
				}
			}
		}

		public string LastReadString
		{
			get { return _lastReadString; }
			set
			{
				if (_lastReadString != value)
				{
					_lastReadString = value;
					OnPropertyChanged("LastReadString");
				}
			}
		}

		public double LastReadValueWithoutFormula
		{
			get { return _lastReadValueWithoutFormula; }
			set
			{
				if (_lastReadValueWithoutFormula != value)
				{
					_lastReadValueWithoutFormula = value;
					OnPropertyChanged("LastReadValueWithoutFormula");
				}
			}
		}

		public double LastReadValue
		{
			get { return _lastReadValue; }
			set
			{
				if (_lastReadValue != value)
				{
					_lastReadValue = value;
					OnPropertyChanged("LastReadValue");
				}
			}
		}

		public DateTime LastReadValueTimestamp
		{
			get { return _lastReadValueTimestamp; }
			set
			{
				if (_lastReadValueTimestamp != value)
				{
					_lastReadValueTimestamp = value;
					OnPropertyChanged("LastReadValueTimestamp");
				}
			}
		}

		public bool LastReadValueError
		{
			get { return _lastReadValueError; }
			set
			{
				if (_lastReadValueError != value)
				{
					_lastReadValueError = value;
					OnPropertyChanged("LastReadValueError");
				}
			}
		}

		public object LastReadRawValue
		{
			get { return _lastReadRawValue; }
			set
			{
				if (_lastReadRawValue != value)
				{
					_lastReadRawValue = value;
					OnPropertyChanged("LastReadRawValue");
				}
			}
		}

		public DateTime LastReadRawValueTimestamp
		{
			get { return _lastReadRawValueTimestamp; }
			set
			{
				if (_lastReadRawValueTimestamp != value)
				{
					_lastReadRawValueTimestamp = value;
					OnPropertyChanged("LastReadRawValueTimestamp");
				}
			}
		}

		public bool LastReadRawValueError
		{
			get { return _lastReadRawValueError; }
			set
			{
				if (_lastReadRawValueError != value)
				{
					_lastReadRawValueError = value;
					OnPropertyChanged("LastReadRawValueError");
				}
			}
		}

		public double EditedValue
		{
			get { return _editedValue; }
			set
			{
				_editedValue = value;
				//OnPropertyChanged("EditedValue");
			}
		}

		public bool Writeable
		{
			get { return !_readOnly; }
		}

		public IDevice Device
		{
			get { return _device; }
			set { _device = value; }
		}

		public byte[] LockBitMask
		{
			get { return _lockBitMask; }
		}

		public bool TriggeredRead
		{
			get { return _triggeredRead; }
			private set { _triggeredRead = value; }
		}

		public ChartValues<MeasureModel> PlotValues { get; set; }

		public double MaxX
		{
			get { return _axisMaxX; }
			set
			{
				_axisMaxX = value;
				OnPropertyChanged("MaxX");
			}
		}

		public double MinX
		{
			get { return _axisMinX; }
			set
			{
				_axisMinX = value;
				OnPropertyChanged("MinX");
			}
		}

		public double MaxY
		{
			get { return _axisMaxY; }
			set
			{
				_axisMaxY = value;
				OnPropertyChanged("MaxY");
			}
		}

		public double MinY
		{
			get { return _axisMinY; }
			set
			{
				_axisMinY = value;
				OnPropertyChanged("MinY");
			}
		}

		public int SampleBuffer
		{
			get { return _sampleBuffer; }
			set { _sampleBuffer = value; }
		}

		#endregion

		#region Constructor

		public Register(XElement register, bool _is16BitI2CMode = false)
		{
			PlotValues = new ChartValues<MeasureModel>();

			bool tempbool;
            int tempInt;
			uint tempInteger;
			byte tempByte;
            ushort tempUshort;

			int regRange = 256;

			if (_is16BitI2CMode)
			{
				regRange = 1024;
			}

			for (int i = 0; i < regRange; i++)
			{
				_byteValues.Add("0x" + i.ToString("X3"));
			}

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
						if (ushort.TryParse(register.Attribute("Address").Value.Substring(3), NumberStyles.HexNumber, null, out tempUshort))
						{
							Address = Convert.ToUInt16(register.Attribute("Address").Value, 16);
						}
					}
					else
					{
						if (byte.TryParse(register.Attribute("Address").Value, out tempByte))
						{
							Address = Convert.ToByte(register.Attribute("Address").Value);
						}
					}
				}
				else
				{
					if (byte.TryParse(register.Attribute("Address").Value, out tempByte))
					{
						Address = Convert.ToByte(register.Attribute("Address").Value);
					}
				}
			}

			if (register.Attribute("DataType") != null)
			{
				DataType = register.Attribute("DataType").Value.ToUpper();
			}

			if (register.Attribute("SignedBit") != null)
			{
				if (int.TryParse(register.Attribute("SignedBit").Value, out tempInt))
				{
					SignedBit = Convert.ToInt32(register.Attribute("SignedBit").Value);
				}
			}

			if (register.Attribute("Access") != null)
			{
				Access = register.Attribute("Access").Value;
			}

			if (register.Attribute("Size") != null)
			{
				if (uint.TryParse(register.Attribute("Size").Value, out tempInteger))
				{
					Size = Convert.ToUInt32(register.Attribute("Size").Value);
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

			if (register.Attribute("LoadFormula") != null)
			{
				LoadFormula = register.Attribute("LoadFormula").Value;
			}

			if (register.Attribute("StoreFormula") != null)
			{
				StoreFormula = register.Attribute("StoreFormula").Value;
			}

			if (register.Attribute("Private") != null)
			{
				if (bool.TryParse(register.Attribute("Private").Value, out tempbool))
				{
					Private = tempbool;
				}
			}

			if (register.Attribute("ReadOnly") != null)
			{
				if (bool.TryParse(register.Attribute("ReadOnly").Value, out tempbool))
				{
					ReadOnly = tempbool;
				}
			}

            if (register.Attribute("LockBitMask") != null)
            {
                if (register.Attribute("LockBitMask").Value.Substring(0, 2).ToUpper() == "0X")
                {
                    if (ushort.TryParse(register.Attribute("LockBitMask").Value.Substring(2), NumberStyles.HexNumber, null, out tempUshort))
                    {
                        var v = Convert.ToUInt16(register.Attribute("LockBitMask").Value, 16);
                        _lockBitMask[0] = (byte)(v & 0xFF);
                        _lockBitMask[1] = (byte)(v >> 8);
                    }
                }
                else
                {
                    if (ushort.TryParse(register.Attribute("LockBitMask").Value, NumberStyles.HexNumber, null, out tempUshort))
                    {
                        var v = Convert.ToUInt16(register.Attribute("LockBitMask").Value, 16);
                        _lockBitMask[0] = (byte)(v & 0xFF);
                        _lockBitMask[1] = (byte)(v >> 8);
                    }
                }
            }

			if (register.Attribute("TriggeredRead") != null)
			{
				if (bool.TryParse(register.Attribute("TriggeredRead").Value, out tempbool))
				{
					TriggeredRead = tempbool;
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
			Access = deepCopyRegister.Access;
			SignedBit = deepCopyRegister.SignedBit;
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

			LastReadString = deepCopyRegister.LastReadString;
			LastReadValueWithoutFormula = deepCopyRegister.LastReadValueWithoutFormula;
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
