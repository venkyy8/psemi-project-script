using AdapterAccess.Adapters;
using AdapterAccess.Protocols;
using HardwareInterfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Linq;
using Serilog;

namespace DeviceAccess
{
	public class Device : IDevice, IRegister
	{
		#region Private Member Variables

		private Protocol _protocol;
		internal DeviceManager _deviceManager;
		private List<Register> _allRegisters;
		private List<Register> _allI2CRegisters;

		private List<Register> _registers;
		private List<Register> _i2cregisters;


		// Child register map
		private List<XElement> _slaveRegisterMap;

		private bool _isParameterized;
		private bool _is16BitAddressing;
		private bool _isMTADevice;
		public string _deviceInfoName = string.Empty; // This name is used to find the correct XML configuration
		private string _baseName = string.Empty;
		private string _i2cBaseName = string.Empty;
		private string _displayName = string.Empty;
		private string _altDisplayName = string.Empty;
		private string _deviceName = string.Empty;
		private string _deviceVersion = string.Empty;
		private List<string> _pluginCompatibility;
		private byte[] _datasheet;
		private byte[] _helpsheet;
		private XElement _ui;
		private XElement _baseNames;

		private const byte DEFAULT_CMD = 0x01;
		private const byte VOUT_COMMAND = 0x21;
		private const byte MFR_ID_REG = 0x99;
		private const byte MFR_MODEL_REG = 0x9A;
		private const byte MFR_REVISION_REG = 0x9B;
		private const byte MFR_4_DIGIT_REG = 0x9D;
		private const byte IC_DEVICE_ID_REG = 0xAD;
		private const byte MAX_BLOCK_READ = 0x14;

		private const byte DEFAULT_REG = 0x00;
		private const byte DEFAULT_REG_16bit = 0x000;
		private const byte LEGACY_KEY_REG = 0x1E;   // ChipID for ARC1C0608 and ARC2C0608 variants
		private const byte ID_REG = 0xFF;           // ChipID
		private const byte KEY_REG = 0xFE;          // ChipID
		private const byte ACCESS_REG = 0x41;
		private const byte UNLOCK = 0x37;
		private const byte LOCK = 0xFF;

		#endregion

		#region Constructor

		internal Device(Protocol protocol, DeviceManager deviceManager, string deviceInfoName = "Unknown", string deviceVersion = "")
		{
			_protocol = protocol;
			_deviceManager = deviceManager;
			_deviceInfoName = deviceInfoName;
			_deviceVersion = deviceVersion;
			_pluginCompatibility = new List<string>();
			SlaveDevices = new List<SlaveDevice>();
		}

		#endregion

		#region General Properties
		public IAdapter Adapter
		{
			get { return _protocol.AttachedAdapter as IAdapter; }
		}

		public bool IsPE26100
        {
			get;
			set;
        }
		public Protocol Protocol
		{
			get { return _protocol; }
		}

		public bool IsParameterized
		{
			get { return _isParameterized; }

			internal set
			{
				_isParameterized = value;
			}
		}

		public bool Is16BitAddressing
		{
			get { return _is16BitAddressing; }

			internal set
			{
				_is16BitAddressing = value;
			}
		}

		public bool IsMTADevice
		{
			get { return _isMTADevice; }

			internal set
			{
				_isMTADevice = value;
			}
		}

		//remove the variable and assign it according to reg response
		bool isR2D2 = true;

		public string DeviceNameAddress
		{
			get
			{
				if (!isR2D2)
				{
					DisplayName = AltDisplayName;
				}

				if (Protocol is I2cProtocol)
				{
					var i2c = Protocol as I2cProtocol;
					return string.Format("I2C: 0x{0:X2} {1}", i2c.TargetAddress, DisplayName);
				}
				else if (Protocol is PMBusProtocol)
				{
					var pmbus = Protocol as PMBusProtocol;
					return string.Format("PMBus: 0x{0:X2} {1}", pmbus.TargetAddress, DisplayName);
				}
				else
				{
					return DisplayName;
				}
			}
		}

		public List<SlaveDevice> SlaveDevices { get; internal set; }

		#endregion

		#region Internal Methods

		internal void ParseRegisterData(IEnumerable configuration, bool _is16BitI2CMode = false)
		{
			_allRegisters = new List<Register>();
			_registers = new List<Register>();
			foreach (XElement register in configuration)
			{
				Register reg = new Register(register, _is16BitI2CMode);
				_allRegisters.Add(reg);

				if (!reg.Private)
				{
					_registers.Add(reg);
				}
			}

			//if (!_is16BitI2CMode)
			//{
			//	_allRegisters = new List<Register>();
			//	_registers = new List<Register>();
			//	foreach (XElement register in configuration)
			//	{
			//		Register reg = new Register(register, _is16BitI2CMode);
			//		_allRegisters.Add(reg);

			//		if (!reg.Private)
			//		{
			//			_registers.Add(reg);
			//		}
			//	}
			//}
			//else
			//{
			//	_allI2CRegisters = new List<Register>();
			//	_i2cregisters = new List<Register>();

			//	foreach (XElement register in configuration)
			//	{
			//		Register reg = new Register(register, _is16BitI2CMode);
			//		_allI2CRegisters.Add(reg);

			//		if (!reg.Private)
			//		{
			//			_i2cregisters.Add(reg);
			//		}
			//	}
			//}
		}

		internal void Unlock()
		{
			this.WriteByte(ACCESS_REG, UNLOCK); // Unlock device
		}

		internal void Lock()
		{
			this.WriteByte(ACCESS_REG, LOCK);   // Lock device
		}

		internal void LoadSlaveRegisterMapConfig(IEnumerable configuration)
		{
			_slaveRegisterMap = new List<XElement>();
			foreach (XElement element in configuration)
			{
				_slaveRegisterMap.Add(element);
			}
		}

		internal List<XElement> GetSlaveRegisterMap()
		{
			return _slaveRegisterMap;
		}

		//Calculate the Number of Trailing Zeros for a 8-bit Value
		internal byte trailingZeros(byte value)
		{
			byte trailingZeros;
			for (trailingZeros = 0; trailingZeros < 7 && ((value & 1) == 0); trailingZeros++) value >>= 1;
			return trailingZeros;
		}

		//Calculate the Number of Trailing Zeros for a 16-bit Value
		internal byte trailingZeros(ushort value)
		{
			byte trailingZeros;
			for (trailingZeros = 0; trailingZeros < 15 && ((value & 1) == 0); trailingZeros++) value >>= 1;
			return trailingZeros;
		}

		//Calculate the Number of Trailing Zeros for a 32-bit Value
		internal byte trailingZeros(uint value)
		{
			byte trailingZeros;
			for (trailingZeros = 0; trailingZeros < 31 && ((value & 1) == 0); trailingZeros++) value >>= 1;
			return trailingZeros;
		}

		internal static void WaitMs(int waitTimeMs)
		{
			if (waitTimeMs != 0)
			{
				// The Stopwatch class makes use of the system's HighPerformanceTimer
				Stopwatch sw = new Stopwatch();
				sw.Start();

				// If the delay time hasn't elapsed, sleep the thread
				while (sw.ElapsedMilliseconds < waitTimeMs)
					Thread.Sleep(0);
			}
		}

		internal double ReadLinear11(int data)
		{
			short exp = 0;
			short magnitude = 0;

			var val = data >> 11;

			if (val > 15)
				exp = (short)((-val ^ 0x1F) + 1);
			else
				exp = (short)val;

			var val2 = data & 0x7FF;

			if (val2 > 1023)
				magnitude = (short)((-val2 ^ 0x7FF) + 1);
			else
				magnitude = (short)val2;

			var result = magnitude * Math.Pow(2, exp);
			return result;
		}

		internal double WriteLinear11(double data, short exp)
		{
			var y = data - (int)data;
			y = (int)(y / Math.Pow(2, exp));
			var magnitude = (int)data / Math.Pow(2, exp);

			var newexp = (ushort)(exp << 11);

			var result = (ushort)(newexp | (ushort)(((ushort)magnitude | (ushort)y)));

			return result;
		}

		internal double ConvertTwosCompliment(int data, int signedBitPos)
		{
			var signedBit = (data >> signedBitPos) & 0x01;
			var removeBit = signedBit << signedBitPos;
			var nValue = data ^ removeBit;
			return (signedBit == 1) ? nValue * -1 : nValue;
		}

		internal string ConvertBlockReadToString(byte[] data)
		{
			// First byte is the length
			int length = data[0];
			byte[] copyArray = new byte[length];
			Array.Copy(data, 1, copyArray, 0, copyArray.Length);
			Array.Reverse(copyArray);

			// If all bytes are non printable then just return the hex string
			if (copyArray.All(b => b < 0x20 || b > 0x7e))
			{
				StringBuilder sb = new StringBuilder();
				foreach (var item in copyArray)
				{
					sb.AppendFormat(item.ToString("X2"));
				}
				return sb.ToString();
			}

			return Encoding.ASCII.GetString(copyArray);
		}

		internal byte[] RemovePEC(int length, byte[] data)
		{
			// Trim off the PEC if present
			byte[] truncArray = new byte[length];

			if (data.Length > 0)
				Array.Copy(data, truncArray, truncArray.Length);
			return truncArray;
		}

		// Convert an object to a byte array
		internal byte[] ObjectToByteArray(object obj)
		{
			BinaryFormatter bf = new BinaryFormatter();
			using (var ms = new MemoryStream())
			{
				bf.Serialize(ms, obj);
				return ms.ToArray();
			}
		}

		// Rounds time to the nearest second
		private DateTime GetTime()
		{
			DateTime now = DateTime.Now;
			int second = 0;

			// round to nearest 1 second mark
			if (now.Second % 1 > 0.5)
			{
				// round up
				second = now.Second + (1 - (now.Second % 1));
			}
			else
			{
				// round down
				second = now.Second - (now.Second % 1);
			}

			return new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, second);
		}

		#endregion

		#region Device Identification

		/// <summary>
		/// Pings the device to see if it is still "present".
		/// </summary>
		/// <param name="isSilent">If set then simply return true without actually attempting to communicate.</param>
		/// <returns></returns>
		public bool IsPresent(out bool is16BitAddresing, bool isSilent = false, bool isDeviceIdHidden = false)
		{
			is16BitAddresing = false;
			// This is a special purpose option used internally
			// It basically assumes there is a device connected.
			if (isSilent)
			{
				return true;
			}

			bool found = false;

			try
			{
				#region PMBus

				// Clear the bus with the stop command
				if (this.Protocol.GetInterfaceType() == typeof(IPMBus))
				{
					// Determine if a device responds
					this.ReadByte(DEFAULT_CMD);

					IPMBus i2cAdapter = this.Adapter as IPMBus;
					i2cAdapter.I2cSetPec(false); // Disable PEC for device search
					i2cAdapter.I2cSendStop();

					// If the device info name is not forced to a value then process
					if (string.IsNullOrEmpty(this._deviceInfoName))
					{
						byte[] data;
						string mfrId = string.Empty;
						string model = string.Empty; // Can be either IC_DEVICE_ID or MFR_MODEL
						string revision = string.Empty;
						string derivitive = string.Empty;

						// Try MFG_ID
						try
						{
							data = new byte[MAX_BLOCK_READ];
							this.ReadBlock(MFR_ID_REG, MAX_BLOCK_READ, ref data);
							mfrId = GetStringFromData(data);
						}
						catch (Exception)
						{ }

						// Try MFR_MODEL
						try
						{
							data = new byte[MAX_BLOCK_READ];
							this.ReadBlock(MFR_MODEL_REG, MAX_BLOCK_READ, ref data);
							model = GetStringFromData(data);
						}
						catch (Exception)
						{ }

						if (string.IsNullOrEmpty(model))
						{
							// Try IC_DEVICE_ID
							try
							{
								data = new byte[MAX_BLOCK_READ];
								this.ReadBlock(IC_DEVICE_ID_REG, MAX_BLOCK_READ, ref data);
								model = GetStringFromData(data);
							}
							catch (Exception)
							{ }
						}

						// Try MFR_REVISION
						try
						{
							data = new byte[MAX_BLOCK_READ];
							this.ReadBlock(MFR_REVISION_REG, MAX_BLOCK_READ, ref data);
							revision = GetStringFromData(data);
						}
						catch (Exception)
						{ }

						// Try MFR_4_DIGIT
						try
						{
							data = new byte[MAX_BLOCK_READ];
							this.ReadBlock(MFR_4_DIGIT_REG, MAX_BLOCK_READ, ref data);
							derivitive = GetStringFromData(data);
						}
						catch (Exception)
						{ }

						//var di = this._deviceManager.DeviceInformation.Find(d => d.MFRID == mfrId && d.Model == model && d.Revision == revision && d.Code == derivitive);
						//if (di != null)
						//{
						//	this._deviceInfoName = di.Plugin;
						//                      i2cAdapter.I2cSetPec(i2cAdapter.I2cGetCapabilities().Pec);
						//	found = true;
						//}

						var di = this._deviceManager.DeviceInformation.Where(d => d.MFRID == mfrId && d.Model == model && d.Revision == revision && d.Code == derivitive).ToList();

						if (di.Any())
						{
							Log.Debug("Matching devices count : ", di.Count, "is16bitAddressing : ", this._deviceManager.Is16BitAddressing);

							if (di.Count == 1)
							{
								this._deviceInfoName = di[0].Plugin;
								i2cAdapter.I2cSetPec(i2cAdapter.I2cGetCapabilities().Pec);
								found = true;
							}
							else
							{
								Log.Debug("Matching devices count : ", di.Count);

								if (!this._deviceManager.Is16BitAddressing)
								{
									if (this._deviceManager.IsPE24103PMBusDevice)
									{
										this._deviceInfoName = di.Find(a => a.Plugin == "PE24103-R01").Plugin;
									}
									else
									{
										this._deviceInfoName = di.Find(a => a.Plugin == "PE24104-R01").Plugin;
									}
								}

								Log.Debug("Device identified : ", this._deviceInfoName);

								if (!String.IsNullOrEmpty(this._deviceInfoName))
								{
									i2cAdapter.I2cSetPec(i2cAdapter.I2cGetCapabilities().Pec);
									found = true;
								}
							}
						}
						else
						{
							Log.Debug("Matching devices doesn't found from the identity registers list. ");
						}
					}
					else
					{
						found = true;
					}
				}

				#endregion

				#region I2C
				
				// Clear the bus with the stop command
				if (this.Protocol.GetInterfaceType() == typeof(II2c))
				{
					II2c i2cAdapter = this.Adapter as II2c;
					i2cAdapter.I2cSendStop();
					

					// Determine if a device responds
					if (!Is16BitAddressing)
					{
						this.ReadByte(DEFAULT_REG);
					}
					else
					{
						this.Read16Bit(DEFAULT_REG_16bit);
					}

					// If the device info name is not forced to a value then process
					if (string.IsNullOrEmpty(this._deviceInfoName))
					{
						byte id = 0;
						byte key = 0;
						if (isDeviceIdHidden)
						{
							this.WriteByte(ACCESS_REG, UNLOCK);     // Unlock device
						}

						foreach (var identityRegister in _deviceManager.IdentityRegisters)
						{
							DeviceManager.DeviceInfo di = null;

							// Read the chip indentification register
							if (identityRegister.RegCode == null)
							{
								key = (byte)((int)this.ReadByte(identityRegister.RegId) & identityRegister.Mask);   // Read Key
								di = this._deviceManager.DeviceInformation.Find(d => d.Code == "" && d.Key == (int)key);
							}
							else
							{
								id = this.ReadByte(identityRegister.RegCode.Value);     // Read ID
								key = (byte)((int)this.ReadByte(identityRegister.RegId) & identityRegister.Mask);   // Read Key
								di = this._deviceManager.DeviceInformation.Find(d => d.Code == id.ToString("X2") && d.Key == (int)key);
							}

							if (di != null)
							{
								this._deviceInfoName = di.Plugin;
								found = true;
								break;
							}
						}

						if (String.IsNullOrEmpty(this._deviceInfoName))
						{
							// 16 bit addressing 

							ushort id16bit = 0;
							ushort key16bit = 0;

							foreach (var identityRegister in _deviceManager.IdentityRegistersTwoBytes)
							{
								DeviceManager.DeviceInfo di = null;

								// Read the chip indentification register
								if (identityRegister.RegCode == null)
								{
									key16bit = (ushort)((int)this.ReadWord(identityRegister.RegId) & identityRegister.Mask);   // Read Key
									di = this._deviceManager.DeviceInformation.Find(d => d.Code == "" && d.Key == (int)key);
								}
								else
								{
									id16bit = this.Read16Bit(identityRegister.RegCode.Value);     // Read ID
									key16bit = (ushort)((int)this.Read16Bit(identityRegister.RegId) & identityRegister.Mask);

									di = this._deviceManager.DeviceInformation.Find(d => d.Code == id16bit.ToString("X2") && d.Key == (int)key16bit);
								}

								//if (di != null )
								//{
								//	this._deviceInfoName = di.Plugin;
								//	is16BitAddresing = true;
								//	found = true;
								//	break;
								//}

								if (di != null)
								{
									if (!this._deviceManager.Is16BitAddressing)
									{
										if (di.Plugin == "PE24103-R01")
										{
											this._deviceManager.IsPE24103PMBusDevice = true;
											found = false;
											break;
										}
										else if (di.Plugin == "PE24104-R01")
										{
											this._deviceManager.IsPE24103PMBusDevice = false;
											found = false;
											break;
										}
									}
									else
									{
										this._deviceInfoName = di.Plugin;
										is16BitAddresing = true;
										found = true;
										break;
									}

									Log.Debug("Matching device plugin : ", di.Plugin);

								}
								else
								{
									Log.Debug("Matching devices doesn't found from the identity registers list. ");
								}
							}
						}

						if (isDeviceIdHidden)
						{
							this.WriteByte(ACCESS_REG, LOCK);       // Lock device)
						}
					}
					else
					{
						found = true;
						if (this._deviceManager.Is16BitAddressing)
						{
							is16BitAddresing = true;
						}
					}
				}

				#endregion

				return found;
			}
			catch (Exception ex)
			{
				Debug.Print(ex.Message);
				return false;
			}
		}

		private string GetStringFromData(byte[] data)
		{
			return ConvertBlockReadToString(data);
		}

		#endregion

		#region IDevice Interface Methods

		public string GetAdapterName()
		{
			return Adapter.AdapterName;
		}

		public string GetAdapterVersion()
		{
			return Adapter.AdapterVersion;
		}

		public string GetAdapterSerialNumber()
		{
			return Adapter.AdapterSerialNumber;
		}

		public string BaseName
		{
			get { return _baseName; }
			internal set { _baseName = value; }
		}

		public string I2CBaseName
		{
			get { return _i2cBaseName; }
			internal set { _i2cBaseName = value; }
		}

		public string DeviceName
		{
			get { return _deviceName; }
			internal set { _deviceName = value; }
		}

		public string DisplayName
		{
			get { return _displayName; }
			internal set { _displayName = value; }
		}

		public string AltDisplayName
		{
			get { return _altDisplayName; }
			internal set { _altDisplayName = value; }
		}

		public string DeviceInfoName
		{
			get { return _deviceInfoName; }
		}

		public string DeviceVersion
		{
			get { return _deviceVersion; }
			internal set { _deviceVersion = value; }
		}

		public List<string> PluginCompatibility
		{
			get { return _pluginCompatibility; }

			internal set
			{
				_pluginCompatibility = value;
			}
		}

		public byte[] DataSheet
		{
			get { return _datasheet; }

			internal set
			{
				_datasheet = value;
			}
		}

		public byte[] HelpSheet
		{
			get { return _helpsheet; }

			internal set
			{
				_helpsheet = value;
			}
		}

		public XElement UiElements
		{
			get { return _ui; }

			internal set
			{
				_ui = value;
			}
		}

		public byte ReadByte(uint address)
		{
			return _protocol.ReadByte(address);
		}

		public ushort ReadWord(uint address)
		{
			return _protocol.ReadWord(address);
		}

		public ushort Read16Bit(uint address)
		{
			return _protocol.Read16Bit(address);
		}

		public void ReadBlock(uint address, int length, ref byte[] data, bool isAdapterControl = false)
		{
			_protocol.ReadBlock(address, length, ref data, isAdapterControl);
		}

		public void WriteByte(uint address, byte data)
		{
			_protocol.WriteByte(address, data);
		}

		public void WriteWord(uint address, ushort data)
		{
			_protocol.WriteWord(address, data);
		}

		public void WriteBlock(uint address, int length, ref byte[] data)
		{
			if (length > 2)
			{
				Array.Reverse(data);
			}
			_protocol.WriteBlock(address, length, ref data);
		}

		public void WriteControl(ushort command)
		{
			_protocol.WriteWord(0, command);
		}

		#endregion

		#region IRegister Interface Methods

		public List<Register> Registers
		{
			get
			{
				if (this._deviceManager.IsAuthenticated)
					return _allRegisters;
				else
					return _registers;
			}
		}

		public List<Register> I2CRegisters
		{
			get
			{
				if (this._deviceManager.IsAuthenticated)
					return _allI2CRegisters;
				else
					return _i2cregisters;
			}
		}

        public List<Register> AllRegisters
		{
			get
			{
				return _allRegisters;
			}
		}

        public Register GetRegister(string registerID)
		{
			return this.Registers.Find(r => r.ID == registerID);
		}
		public Register GetRegisters(string registerID)
		{
			return this._allRegisters.Find(r => r.ID == registerID);
		}

		public Register.Bit GetRegisterBit(string bitID)
		{
			Register.Bit bit = null;
			foreach (var register in this.Registers)
			{
				bit = register.Bits.Find(b => b.ID == bitID);
				if (bit != null)
				{
					return bit;
				}
			}
			return bit;
		}

		public bool isRegisterIDValid(string registerID)
		{
			if (GetRegister(registerID) != null) return true;
			else return false;
		}

		public string GetRegisterDisplayName(string registerID)
		{
			string returnString = "";
			Register register = GetRegister(registerID);
			if (register != null) returnString = GetRegister(registerID).DisplayName;
			return returnString;
		}

		public uint GetRegisterAddress(string registerID)
		{
			uint returnValue = 0;
			Register register = GetRegister(registerID);
			if (register != null) returnValue = GetRegister(registerID).Address;
			return returnValue;
		}

		public string GetRegisterDataType(string registerID)
		{
			string returnString = "";
			Register register = GetRegister(registerID);
			if (register != null) returnString = GetRegister(registerID).DataType;
			return returnString;
		}

		public int GetRegisterSignedBit(string registerID)
		{
			int returnValue = 0;
			Register register = GetRegister(registerID);
			if (register != null) returnValue = GetRegister(registerID).SignedBit;
			return returnValue;
		}

		public uint GetRegisterSize(string registerID)
		{
			uint returnValue = 0;
			Register register = GetRegister(registerID);
			if (register != null) returnValue = GetRegister(registerID).Size;
			return returnValue;
		}

		public string GetRegisterDescription(string registerID)
		{
			string returnString = "";
			Register register = GetRegister(registerID);
			if (register != null) returnString = GetRegister(registerID).Description;
			return returnString;
		}

		public string GetRegisterFormat(string registerID)
		{
			String returnString = "";
			Register register = GetRegister(registerID);
			if (register != null) returnString = GetRegister(registerID).Format;
			return returnString;
		}

		public string GetRegisterAccess(string registerID)
		{
			String returnString = "";
			Register register = GetRegister(registerID);
			if (register != null) returnString = GetRegister(registerID).Access;
			return returnString;
		}

		public string GetRegisterUnit(string registerID)
		{
			String returnString = "";
			Register register = GetRegister(registerID);
			if (register != null) returnString = GetRegister(registerID).Unit;
			return returnString;
		}

		public string GetRegisterLoadFormula(string registerID)
		{
			String returnString = "";
			Register register = GetRegister(registerID);
			if (register != null) returnString = GetRegister(registerID).LoadFormula;
			return returnString;
		}

		public string GetRegisterStoreFormula(string registerID)
		{
			String returnString = "";
			Register register = GetRegister(registerID);
			if (register != null) returnString = GetRegister(registerID).StoreFormula;
			return returnString;
		}

		public bool ReadRegister(Register register, ref object value)
		{
			if (register.DataType.ToUpper() == "C")
			{
				Boolean success = false;
				object returnValue = 0;
				byte[] tempValue = null;

				if (register != null)
				{
					byte[] readValue = new byte[register.Size];

					//If the Cached Value is Still Valid
					if (DateTime.Now.Subtract(register.LastReadRawValueTimestamp).TotalMilliseconds > register.REGISTER_CACHE_TIMEOUT.TotalMilliseconds)
					{
						try
						{
							//Read the Value from the Device
							this.ReadBlock(register.Address, (int)register.Size, ref readValue);

							// Remove the PEC byte if present
							tempValue = RemovePEC((int)register.Size, readValue);
							returnValue = ConvertBlockReadToString(readValue);

							//Update the Last Read Raw Value
							register.LastReadRawValue = readValue;
							register.LastReadRawValueTimestamp = DateTime.Now;
							register.LastReadRawValueError = false;
							register.LastReadValueError = false;
							register.LastReadString = returnValue.ToString();

							//Indicate the Success
							success = true;
						}
						catch (Exception e)
						{
							register.LastReadRawValueError = true;
							register.LastReadValueError = true;
							throw new Exception("Reading from Register (" + register.DisplayName + ")", e);
						}
					}
					//Otherwise Use the Cached Value
					else
					{
						returnValue = ConvertBlockReadToString(ObjectToByteArray(register.LastReadRawValue));

						//Indicate the Success
						success = true;
					}
				}

				//Return the Data to the Caller if Successful
				if (success) value = returnValue;

				return success;
			}
			else
			{
				double returnDouble = 0;
				var result = ReadRegisterValue(register, ref returnDouble);
				value = register.LastReadValueWithoutFormula;
				return result;
			}
		}

		public bool ReadRegister(string registerID, ref object value)
		{
			return ReadRegister(GetRegister(registerID), ref value);
		}

		public bool ReadRegister(uint address, ref object value)
		{
			Register reg = _allRegisters.Find(r => r.Address == address);
			if (reg != null)
			{
				return ReadRegister(reg, ref value);
			}
			else
			{
				return false;
			}
		}

		public object ReadRegister(Register register)
		{
			object returnValue = 0;
			ReadRegister(register, ref returnValue);
			return returnValue;
		}

		public object ReadRegister(string registerID)
		{
			return ReadRegister(GetRegister(registerID));
		}

		public object ReadRegister(uint address)
		{
			Register reg = _allRegisters.Find(r => r.Address == address);
			if (reg != null)
			{
				return ReadRegister(reg);
			}
			else
			{
				return null;
			}
		}

		public bool WriteRegister(Register register, string protocolType, string readCount, byte[] data)
		{
			Boolean success = false;
			int byteCount = 0;
			bool isIntData = int.TryParse(readCount, out byteCount);

			if (isIntData && register != null)
			{
				this.WriteBlock(register.Address, data.Length, protocolType, ref data);
			}

			return success;
		}

		public void WriteBlock(uint address, int length, string protocolType, ref byte[] data)
		{
			if (protocolType.ToLower() == "pmbus")
			{
				Array.Reverse(data);
			}
			_protocol.WriteBlock(address, length, ref data);
		}

		public bool WriteRegister(Register register, object value)
		{
			Boolean success = false;

			if (register != null)
			{
				byte[] writeValue = new byte[register.Size];

				uint color;

				//Convert the Value to a Raw Value
				switch (register.Size)
				{
					case 1:
						uint rawValue1 = Convert.ToUInt32(value);
						writeValue[0] = (byte)(rawValue1 & 0xFF);

						//if (value.ToString().Length != register.Size)
						//{
						//	writeValue = new byte[value.ToString().Length / 2];
						//	for (int i = 0; i < value.ToString().Length / 2; i++)
						//	{
						//		writeValue[i] = Convert.ToByte(value.ToString().Substring(i * 2, 2), 16);
						//	}
						//}
						//else
						//{
						//	uint rawValue1 = Convert.ToUInt32(value);
						//	writeValue[0] = (byte)(rawValue1 & 0xFF);
						//}

						break;
					case 2:
						if (Is16BitAddressing)
						{
							string hexValue = (Int64.Parse(value.ToString())).ToString("X4");

							if (value.ToString().Length % 2 != 0 || value.ToString().Length == 2)
							{
								value = value.ToString().PadLeft(4, '0');
							}

							writeValue = null;
							writeValue = new byte[hexValue.ToString().Length / 2];
							for (int i = 0; i < hexValue.ToString().Length / 2; i++)
							{
								writeValue[i] = Convert.ToByte(hexValue.ToString().Substring(i * 2, 2), 16);
							}
						}
						else
						{
							uint rawValue2 = Convert.ToUInt32(value);
							var ushortVal = (ushort)(rawValue2 & 0xFFFF);
							writeValue = BitConverter.GetBytes(ushortVal);
						}
						break;
					default:
						break;
				}

				//Write the Raw Value to the Register
				try
				{
					// remove any bits that are masked as not writable
					if (!this._deviceManager.IsAuthenticated)
					{
						for (int i = 0; i < writeValue.Length; i++)
						{
							writeValue[i] = (byte)(writeValue[i] & register.LockBitMask[i]);
						}
					}

					Log.Debug("writeValue Length : " + writeValue.Length);

					if (writeValue.Length > 1)
					{
						Log.Debug("Device - WriteRegister . Register : " + register.DisplayName + " Register size : " + (int)register.Size
							+ " Value : " + value.ToString() + " Data[0] : " + writeValue[0] + " Data[1] :" + writeValue[1]);
					}
					else if (writeValue.Length == 1)
					{
						Log.Debug("Device - WriteRegister . Register : " + register.DisplayName + " Register size : " + (int)register.Size
							+ " Value : " + value.ToString() + " Data[0] : " + writeValue[0]);
					}

					this.WriteBlock(register.Address, (int)register.Size, ref writeValue);
					success = true;
					//Clear the Last Read Timestamps to Invalidate the Cache
					register.LastReadRawValueTimestamp = DateTime.MinValue;
					register.LastReadValueTimestamp = DateTime.MinValue;
					register.LastReadValueError = false;
					register.LastReadRawValueError = false;
				}
				catch (Exception e)
				{
					register.LastReadValueError = true;
					register.LastReadRawValueError = true;
					throw new Exception("Writing to Register (" + register.DisplayName + ")", e);
				}
			}
			return success;
		}

		public bool WriteRegister(string registerID, object value)
		{
			return WriteRegister(GetRegister(registerID), value);
		}

		public bool WriteRegister(uint address, object value)
		{
			Register reg = _allRegisters.Find(r => r.Address == address);
			if (reg != null)
			{
				return WriteRegister(reg, value);
			}
			else
			{
				return false;
			}
		}

		public bool ReadRegisterValue(Register register, ref double value)
		{
			double returnValue = 0;
			double readValue = 0;
			bool success = false;
			byte[] tempValue = null;
			string formattedValue = "";

			if (register != null)
			{
				Byte[] readValueRaw = new byte[register.Size];

				//If the Cached Value is Still Valid
				if (DateTime.Now.Subtract(register.LastReadValueTimestamp).TotalMilliseconds > register.REGISTER_CACHE_TIMEOUT.TotalMilliseconds)
				{
					try
					{
						//Read the Value from the Gauge
						this.ReadBlock(register.Address, (int)register.Size, ref readValueRaw);

						// Format the read bytes
						tempValue = RemovePEC((int)register.Size, readValueRaw);

						//Convert the Raw Value to an Actual Value
						switch (register.DataType.ToUpper())
						{
							case "F":   // Floating
							case "U":   // Unsigned Int
							case "H":   // Hex
							case "L11": // Linear11
							case "S":   // 2's compliment
								switch (register.Size)
								{
									case 1:
										Byte tempValueU8 = tempValue[0];
										readValue = System.Convert.ToDouble(tempValueU8);
										break;
									case 2:
										UInt16 tempValueU16 = BitConverter.ToUInt16(tempValue, 0);
										readValue = System.Convert.ToDouble(tempValueU16);

										if (Is16BitAddressing)
										{
											ushort data = (ushort)(tempValue[1] + ((UInt16)tempValue[0] << 8));
											readValue = Convert.ToDouble(data);
										}

										Log.Debug("Device : ReadRegisterValue . Register value : " + readValue);

										break;
									case 4:
										UInt32 tempValueU32 = BitConverter.ToUInt32(tempValue, 0);
										readValue = System.Convert.ToDouble(tempValueU32);
										break;
									default:
										break;
								}

								if (register.DataType.ToUpper().Equals("S"))
								{
									returnValue = ConvertTwosCompliment((int)readValue, (int)register.SignedBit);
									formattedValue = returnValue.ToString(register.Format);
								}
								break;
							case "I": // Int
								switch (register.Size)
								{
									case 1:
										Char tempValue8 = BitConverter.ToChar(tempValue, 0);
										readValue = System.Convert.ToDouble(tempValue8);
										break;
									case 2:
										Int16 tempValue16 = BitConverter.ToInt16(tempValue, 0);
										readValue = System.Convert.ToDouble(tempValue16);
										break;
									case 4:
										Int32 tempValue32 = BitConverter.ToInt32(tempValue, 0);
										readValue = System.Convert.ToDouble(tempValue32);
										break;
									default:
										break;
								}
								break;
							case "C": // String
								Log.Error("Cannot convert register data type from string to double.");
								throw new Exception("Cannot convert register data type from string to double.");
							default:
								break;
						}

						if (register.DataType.ToUpper().Equals("L11"))
						{
							returnValue = ReadLinear11((int)readValue);
							formattedValue = returnValue.ToString(register.Format);
						}
						else
						{
							//If There is a Load Formula
							if (register.LoadFormula != null)
							{
								//Calculate the Value and Add it To the Graph
								double evalValue = Evaluator.EvalToDouble(register.LoadFormula.ToUpper().Replace("X", readValue.ToString(CultureInfo.InvariantCulture)));
								returnValue = System.Convert.ToDouble(evalValue);
							}
							//Otherwise Use the Raw Value
							else
							{
								returnValue = readValue;
							}
						}

						//Update the Last Read Raw Value
						register.LastReadRawValueTimestamp = DateTime.Now;
						register.LastReadValueTimestamp = DateTime.Now;
						register.LastReadRawValueError = false;
						register.LastReadValueError = false;
						register.LastReadRawValue = readValueRaw;


						if (Is16BitAddressing &&
							register.Name == "VOUT1_R" || register.Name == "VOUT2_R" || register.Name == "VOUT3_R" || register.Name == "VOUT4_R")
						{
							var lastReadVal = Convert.ToInt32(((returnValue * 100) / 2048));
							register.LastReadValue = lastReadVal;
						}
						else
						{
							register.LastReadValue = returnValue;
						}

						register.LastReadValueWithoutFormula = readValue;

						// Load the values buffer for plotting
						var now = GetTime();
						register.PlotValues.Add(new MeasureModel
						{
							DateTime = now,
							Value = returnValue
						});

						if (register.PlotValues.Count > register.SampleBuffer) register.PlotValues.RemoveAt(0);
						register.MaxX = now.Ticks + TimeSpan.FromSeconds(0).Ticks; // lets force the axis to be 1 second ahead
						register.MinX = now.Ticks - TimeSpan.FromSeconds(8).Ticks; // and 8 seconds behind

						// Create the formatted string value
						switch (register.DataType.ToUpper())
						{
							case "S":
							case "U":
							case "H":
							case "I":
								var valUInt = Convert.ToUInt32(returnValue);
								if (_deviceInfoName.ToUpper() == "PE26100")
								{
									IsPE26100 = true;
								}
								if (IsPE26100)
                                {
									
									formattedValue = valUInt.ToString("X2");
								}
                                else
                                {
									formattedValue = valUInt.ToString(register.Format);
								}
								
								break;
							case "F":
								var valDouble = Convert.ToDouble(returnValue);
								formattedValue = valDouble.ToString(register.Format);
								break;
						}

						register.LastReadString = formattedValue;

						// Store the bit values
						foreach (Register.Bit bit in register.Bits)
						{
							uint val = (uint)readValue;
							val &= bit.Mask;
							val >>= trailingZeros(bit.Mask);
							bit.LastReadValue = Convert.ToBoolean(val);
						}

						//Indicate the Success
						success = true;
					}
					catch (Exception e)
					{
						register.LastReadRawValueError = true;
						register.LastReadValueError = true;

						//Removed code to throw exception as it fails other reg valu fetch also.
						//throw new Exception("Reading Value from Register (" + register.DisplayName + ")", e);
					}
				}
				//Otherwise Use the Cached Value
				else
				{
					//Store the Cached Value
					returnValue = register.LastReadValue;

					//Indicate the Success
					success = true;
				}
			}

			//Return the Data to the Caller if Successful
			if (success) value = returnValue;

			return success;
		}

		public bool ReadRegisterValue(Register register, ref double value, int readCount, bool isAdapterControl = false)
		{
			double returnValue = 0;
			double readValue = 0;
			bool success = false;
			byte[] tempValue = null;
			string formattedValue = "";

			if (register != null)
			{
				Byte[] readValueRaw = new byte[register.Size];

				//If the Cached Value is Still Valid
				if (DateTime.Now.Subtract(register.LastReadValueTimestamp).TotalMilliseconds > register.REGISTER_CACHE_TIMEOUT.TotalMilliseconds)
				{
					try
					{
						//Read the Value from the Gauge
						this.ReadBlock(register.Address, readCount, ref readValueRaw, true);

						// Format the read bytes
						tempValue = RemovePEC((int)register.Size, readValueRaw);

						//Convert the Raw Value to an Actual Value
						switch (register.DataType.ToUpper())
						{
							case "F":   // Floating
							case "U":   // Unsigned Int
							case "H":   // Hex
							case "L11": // Linear11
							case "S":   // 2's compliment
								switch (register.Size)
								{
									case 1:
										Byte tempValueU8 = tempValue[0];
										readValue = System.Convert.ToDouble(tempValueU8);
										break;
									case 2:
										UInt16 tempValueU16 = BitConverter.ToUInt16(tempValue, 0);
										readValue = System.Convert.ToDouble(tempValueU16);

										if (Is16BitAddressing)
										{
											ushort data = (ushort)(tempValue[1] + ((UInt16)tempValue[0] << 8));
											readValue = Convert.ToDouble(data);
										}

										Log.Debug("Device : ReadRegisterValue . Register value : " + readValue);

										break;
									case 4:
										UInt32 tempValueU32 = BitConverter.ToUInt32(tempValue, 0);
										readValue = System.Convert.ToDouble(tempValueU32);
										break;
									default:
										break;
								}

								if (register.DataType.ToUpper().Equals("S"))
								{
									returnValue = ConvertTwosCompliment((int)readValue, (int)register.SignedBit);
									formattedValue = returnValue.ToString(register.Format);
								}
								break;
							case "I": // Int
								switch (register.Size)
								{
									case 1:
										Char tempValue8 = BitConverter.ToChar(tempValue, 0);
										readValue = System.Convert.ToDouble(tempValue8);
										break;
									case 2:
										Int16 tempValue16 = BitConverter.ToInt16(tempValue, 0);
										readValue = System.Convert.ToDouble(tempValue16);
										break;
									case 4:
										Int32 tempValue32 = BitConverter.ToInt32(tempValue, 0);
										readValue = System.Convert.ToDouble(tempValue32);
										break;
									default:
										break;
								}
								break;
							case "C": // String
								Log.Error("Cannot convert register data type from string to double.");
								throw new Exception("Cannot convert register data type from string to double.");
							default:
								break;
						}

						if (register.DataType.ToUpper().Equals("L11"))
						{
							returnValue = ReadLinear11((int)readValue);
							formattedValue = returnValue.ToString(register.Format);
						}
						else
						{
							//If There is a Load Formula
							if (register.LoadFormula != null)
							{
								//Calculate the Value and Add it To the Graph
								double evalValue = Evaluator.EvalToDouble(register.LoadFormula.ToUpper().Replace("X", readValue.ToString(CultureInfo.InvariantCulture)));
								returnValue = System.Convert.ToDouble(evalValue);
							}
							//Otherwise Use the Raw Value
							else
							{
								returnValue = readValue;
							}
						}

						//Update the Last Read Raw Value
						register.LastReadRawValueTimestamp = DateTime.Now;
						register.LastReadValueTimestamp = DateTime.Now;
						register.LastReadRawValueError = false;
						register.LastReadValueError = false;
						register.LastReadRawValue = readValueRaw;

						if (Is16BitAddressing &&
							register.Name == "VOUT1_R" || register.Name == "VOUT2_R" || register.Name == "VOUT3_R" || register.Name == "VOUT4_R")
						{
							var lastReadVal = Convert.ToInt32(((returnValue * 100) / 2048));
							register.LastReadValue = lastReadVal;
						}
						else
						{
							register.LastReadValue = returnValue;
						}

						register.LastReadValueWithoutFormula = readValue;

						// Load the values buffer for plotting
						var now = GetTime();
						register.PlotValues.Add(new MeasureModel
						{
							DateTime = now,
							Value = returnValue
						});

						if (register.PlotValues.Count > register.SampleBuffer) register.PlotValues.RemoveAt(0);
						register.MaxX = now.Ticks + TimeSpan.FromSeconds(0).Ticks; // lets force the axis to be 1 second ahead
						register.MinX = now.Ticks - TimeSpan.FromSeconds(8).Ticks; // and 8 seconds behind

						// Create the formatted string value
						switch (register.DataType.ToUpper())
						{
							case "S":
							case "U":
							case "H":
							case "I":
								var valUInt = Convert.ToUInt32(returnValue);
								formattedValue = valUInt.ToString(register.Format);
								break;
							case "F":
								var valDouble = Convert.ToDouble(returnValue);
								formattedValue = valDouble.ToString(register.Format);
								break;
						}

						register.LastReadString = formattedValue;

						// Store the bit values
						foreach (Register.Bit bit in register.Bits)
						{
							uint val = (uint)readValue;
							val &= bit.Mask;
							val >>= trailingZeros(bit.Mask);
							bit.LastReadValue = Convert.ToBoolean(val);
						}

						//Indicate the Success
						success = true;
					}
					catch (Exception e)
					{
						register.LastReadRawValueError = true;
						register.LastReadValueError = true;

						//Removed code to throw exception as it fails other reg valu fetch also.
						//throw new Exception("Reading Value from Register (" + register.DisplayName + ")", e);
					}
				}
				//Otherwise Use the Cached Value
				else
				{
					//Store the Cached Value
					returnValue = register.LastReadValue;

					//Indicate the Success
					success = true;
				}
			}

			//Return the Data to the Caller if Successful
			if (success) value = returnValue;

			return success;
		}
		public bool ReadRegisterValue(string registerID, ref double value)
		{
			return ReadRegisterValue(GetRegister(registerID), ref value);
		}

		public double ReadRegisterValue(Register register)
		{
			double returnValue = 0;
			ReadRegisterValue(register, ref returnValue);
			return returnValue;
		}

		public double ReadRegisterValue(Register register, int readCount, bool isAdapterControl = false)
		{
			double returnValue = 0;
			ReadRegisterValue(register, ref returnValue, readCount, isAdapterControl);
			return returnValue;
		}
		public double ReadRegisterValue(string registerID)
		{
			return ReadRegisterValue(GetRegister(registerID));
		}

		public bool WriteRegisterValue(Register register, double value)
		{
			bool success = false;
			double writeValue = 0;

			if (register != null)
			{
				byte[] writeValueRaw = new byte[register.Size];

				try
				{
					if (register.DataType.ToUpper() == "L11")
					{
						writeValue = WriteLinear11(value, (short)register.SignedBit);
					}
					else
					{
						//If There is a Store Formula
						if (register.StoreFormula != null)
						{
							//Calculate the Value and Add it To the Graph
							writeValue = Evaluator.EvalToDouble(register.StoreFormula.ToUpper().Replace("X", value.ToString(CultureInfo.InvariantCulture)));
						}
						//Otherwise Use the Raw Value
						else
						{
							writeValue = value;
						}
					}

					//Convert the Value to a Raw Value
					switch (register.DataType.ToUpper())
					{
						case "L11":
						case "F":
						case "U":
						case "H":
							switch (register.Size)
							{
								case 1:
									Byte tempValueU8 = System.Convert.ToByte(writeValue);
									writeValueRaw = BitConverter.GetBytes(tempValueU8);
									break;
								case 2:
									UInt16 tempValueU16 = System.Convert.ToUInt16(writeValue);
									writeValueRaw = BitConverter.GetBytes(tempValueU16);
									break;
								case 4:
									UInt32 tempValueU32 = System.Convert.ToUInt32(writeValue);
									writeValueRaw = BitConverter.GetBytes(tempValueU32);
									break;
								default:
									break;
							}
							break;
						case "I":
							switch (register.Size)
							{
								case 1:
									Char tempValue8 = System.Convert.ToChar(writeValue);
									writeValueRaw = BitConverter.GetBytes(tempValue8);
									break;
								case 2:
									Int16 tempValue16 = System.Convert.ToInt16(writeValue);
									writeValueRaw = BitConverter.GetBytes(tempValue16);
									break;
								case 4:
									Int32 tempValue32 = System.Convert.ToInt32(writeValue);
									writeValueRaw = BitConverter.GetBytes(tempValue32);
									break;
								default:
									break;
							}
							break;
						case "S":
							throw new NotImplementedException();
						default:
							break;
					}

					// remove any bits that are masked as not writable
					if (!this._deviceManager.IsAuthenticated)
					{
						for (int i = 0; i < writeValueRaw.Length; i++)
						{
							writeValueRaw[i] = (byte)(writeValueRaw[i] & register.LockBitMask[i]);
						}
					}

					//Write the Raw Value to the Register
					this.WriteBlock(register.Address, (int)register.Size, ref writeValueRaw);
					success = true;
					//Clear the Last Read Timestamps to Invalidate the Cache
					register.LastReadRawValueTimestamp = DateTime.MinValue;
					register.LastReadValueTimestamp = DateTime.MinValue;
				}
				catch (Exception e)
				{
					throw new Exception("Writing Value to Register (" + register.DisplayName + ")", e);
				}
			}
			return success;
		}

		public bool WriteRegisterValue(string registerID, double value)
		{
			return WriteRegisterValue(GetRegister(registerID), value);
		}

		public bool ReadRegisterBit(Register.Bit bit, ref uint value)
		{
			uint returnValue = 0;
			bool success = false;

			if (bit != null)
			{
				Register register = GetRegister(bit.RegisterID);
				if (register != null)
				{
					//Read the Register and Unterpret The Result as Unsigned
					switch (register.Size)
					{
						case 1:
							returnValue = (byte)(ReadRegister(register));
							break;
						case 2:
							returnValue = (ushort)(ReadRegister(register));
							break;
						case 4:
							returnValue = (uint)(ReadRegister(register));
							break;
						default:
							break;
					}
					//Apply the Mask and Shift the Result
					returnValue &= bit.Mask;
					returnValue >>= trailingZeros(bit.Mask);

					//Indicate the Success
					success = true;
				}
			}

			//Return the Data to the Caller if Successful
			if (success)
			{
				value = returnValue;
				bit.LastReadValue = Convert.ToBoolean(value);
			}

			return success;
		}

		public bool ReadRegisterBit(string bitID, ref uint value)
		{
			return ReadRegisterBit(GetRegisterBit(bitID), ref value);
		}

		public uint ReadRegisterBit(Register.Bit bit)
		{
			uint returnValue = 0;
			ReadRegisterBit(bit, ref returnValue);
			return returnValue;
		}

		public uint ReadRegisterBit(string bitID)
		{
			return ReadRegisterBit(GetRegisterBit(bitID));
		}

		public bool WriteRegisterBit(Register.Bit bit, uint value)
		{
			bool success = false;
			uint writeValue = 0;

			if (bit != null)
			{
				Register register = GetRegister(bit.RegisterID);
				if (register != null)
				{
					//Read the Register and Unterpret The Result as Unsigned
					switch (register.Size)
					{
						case 1:
							writeValue = Convert.ToByte(ReadRegister(register));
							break;
						case 2:
							writeValue = Convert.ToUInt16(ReadRegister(register));
							break;
						case 4:
							writeValue = Convert.ToUInt32(ReadRegister(register));
							break;
						default:
							break;
					}

					Log.Debug("Device - WriteRegisterBit - writeValue before applying mask : " + writeValue.ToString());

					//Apply the Mask
					writeValue = (value == 0) ? writeValue ^ bit.Mask : writeValue | bit.Mask;

					Log.Debug("Device - WriteRegisterBit - writeValue after applying mask : " + writeValue.ToString());

					// remove any bits that are masked as not writable
					if (!this._deviceManager.IsAuthenticated)
					{
						var v = (register.LockBitMask[1] << 8) + register.LockBitMask[0];
						writeValue = (uint)(writeValue & v);
					}

					//Write the Unisgned Value to the Register
					success = WriteRegister(register, writeValue);
				}
			}
			return success;
		}

		public bool WriteRegisterBit(string bitID, uint value)
		{
			return WriteRegisterBit(GetRegisterBit(bitID), value);
		}

		public async Task<bool> LoadRegisters(object map)
		{
			bool status = false;
			var omap = (RegisterMap)map;
			await Task.Run(() =>
			{
				// User registers
				foreach (var item in omap.Registers)
				{
					Register reg = _allRegisters.Find(r => r.Address == (uint)item.IntAddress);

					if (reg == null)
						throw new Exception("Unable to find register address " + item.Address);

					if (reg.ReadOnly == false)
					{
						this.WriteRegister(reg, item.UIntValue);
						this.ReadRegisterValue(reg);
					}
				}

				// Write system registers if access is open only.
				if (_deviceManager.IsAuthenticated)
				{
					foreach (var item in omap.System.Values)
					{
						Register reg = _allRegisters.Find(r => r.Address == (uint)item.Key);

						if (reg == null)
							throw new Exception("Unable to find system register address " + item.Key.ToString("X2"));

						if (reg.ReadOnly == false)
						{
							this.WriteRegister(reg, item.Value);
							this.ReadRegisterValue(reg);
						}
					}
				}

				status = true;
			});

			return status;
		}

		public async Task<object> CreateRegisterMap()
		{
			RegisterMap regMap = new RegisterMap
			{
				DeviceName = this.DisplayName,
				DeviceVersion = this.DeviceVersion,
				Registers = new List<Map>(),
				System = new SystemData { Enc = !_deviceManager.IsAuthenticated }
			};

			await Task<object>.Run(() =>
			{
				// Save only writeable registers that are byte and word length only
				// Murata requested saving readonly register for the sake of documentation but must prevent writing during load to prevent possible issues
				var regs = _allRegisters.Where(r => r.Access.ToLower() != "sendbyte" && r.Access.ToLower() != "receivebyte");
				//regs = regs.Where(r => r.ReadOnly == false);
				regs = regs.Where(r => r.Access.ToLower() != "rblock");

				foreach (Register reg in regs)
				{
					this.ReadRegisterValue(reg);
					if (reg.Private)
					{
						regMap.System.Values.Add((ushort)reg.Address, (ushort)reg.LastReadValueWithoutFormula);
					}
					else
					{
						regMap.Registers.Add(
							new Map((byte)reg.Address, (ushort)reg.LastReadValueWithoutFormula));
					}
				}
			});

			return regMap;
		}

		#endregion
	}
}
