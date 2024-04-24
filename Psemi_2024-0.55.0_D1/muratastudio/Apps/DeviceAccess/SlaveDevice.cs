using HardwareInterfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DeviceAccess
{
	public class SlaveDevice : IDevice, IRegister
	{
		#region Private Members

		private const byte CMD_READ = 0x0F;
		private const byte CMD_WRITE = 0x08;

		private Device _masterDevice;
		private List<Register> _allRegisters;
		private List<Register> _registers;
		private string _deviceName = string.Empty;
		private string _displayName = string.Empty;
		private string _altDisplayName = string.Empty;
		private SlaveAccess _access;

		#endregion

		#region Constructor

		internal SlaveDevice(Device masterDevice, SlaveAccess access)
		{
			_masterDevice = masterDevice;
			_access = access;
		}

		#endregion

		#region Internal Methods

		internal void ParseRegisterData(IEnumerable configuration)
		{
			_allRegisters = new List<Register>();
			_registers = new List<Register>();
			foreach (XElement register in configuration)
			{
				Register reg = new Register(register);
				reg.Device = this;
				_allRegisters.Add(reg);

				if (!reg.Private)
				{
					_registers.Add(reg);
				}
			}
		}

		#endregion

		#region IDevice Implementation

		public string GetAdapterName()
		{
			return _masterDevice.GetAdapterName();
		}

		public string GetAdapterVersion()
		{
			return _masterDevice.GetAdapterVersion();
		}

		public string GetAdapterSerialNumber()
		{
			return _masterDevice.GetAdapterSerialNumber();
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
			get { return _masterDevice.DeviceInfoName; }
		}

		public string DeviceVersion
		{
			get { return _masterDevice.DeviceVersion; }
		}

		public List<string> PluginCompatibility
		{
			get { return _masterDevice.PluginCompatibility; ; }
		}

		public byte[] DataSheet
		{
			get { return _masterDevice.DataSheet; }
		}

		public byte[] HelpSheet
		{
			get { return _masterDevice.HelpSheet; }
		}

		public System.Xml.Linq.XElement UiElements
		{
			get { return _masterDevice.UiElements; }
		}

		public byte ReadByte(uint address)
		{
			byte result = 0;

			// Write the address
			_masterDevice.WriteByte((uint)_access.Address, (byte)address);

			// Write the command
			_masterDevice.WriteByte((uint)_access.Command, CMD_READ);

			// Verify command status TODO Validate the result
			result = _masterDevice.ReadByte((uint)_access.Command);

			// Read the result
			result = _masterDevice.ReadByte((uint)_access.ReadData);

			return result;
		}

		public ushort ReadWord(uint address)
		{
			throw new NotImplementedException();
		}

		public void ReadBlock(uint address, int length, ref byte[] data, bool isAdapterControl = false)
		{
			// Write the address
			_masterDevice.WriteByte((uint)_access.Address, (byte)address);

			// Write the command
			_masterDevice.WriteByte((uint)_access.Command, CMD_READ);

			// Verify command status TODO Validate the result
			_masterDevice.ReadBlock((uint)_access.Command, length, ref data);

			// Read the result
			_masterDevice.ReadBlock((uint)_access.ReadData, length, ref data);
		}

		public void WriteByte(uint address, byte data)
		{
			byte result = 0;

			// Write the address
			_masterDevice.WriteByte((uint)_access.Address, (byte)address);

			// Write the register data
			_masterDevice.WriteByte((uint)_access.WriteData, data);

			// Write the command
			_masterDevice.WriteByte((uint)_access.Command, CMD_WRITE);

			// Verify command status TODO Validate the result
			result = _masterDevice.ReadByte((uint)_access.Command);
		}

		public void WriteWord(uint address, ushort data)
		{
			throw new NotImplementedException();
		}

		public void WriteBlock(uint address, int length, string protocolType, ref byte[] data)
		{
			if (protocolType.ToLower() == "pmbus")
			{
				Array.Reverse(data);
			}

			_masterDevice.WriteBlock(address, length, ref data);
		}

		public void WriteBlock(uint address, int length, ref byte[] data)
		{
			byte result = 0;

			// Write the address
			_masterDevice.WriteByte((uint)_access.Address, (byte)address);

			// Write the register data
			_masterDevice.WriteBlock((uint)_access.WriteData, length, ref data);

			// Write the command
			_masterDevice.WriteByte((uint)_access.Command, CMD_WRITE);

			// Verify command status TODO Validate the result
			result = _masterDevice.ReadByte((uint)_access.Command);
		}

		public void WriteControl(ushort command)
		{
			throw new NotImplementedException();
		}

		#endregion

		#region IRegister Interface Methods

		public List<Register> Registers
		{
			get
			{
				if (_masterDevice._deviceManager.IsAuthenticated)
					return _allRegisters;
				else
					return _registers;
			}
		}

		public Register GetRegister(string registerID)
		{
			return this.Registers.Find(r => r.ID == registerID);
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

		public string GetRegisterAccess(string registerID)
		{
			String returnString = "";
			Register register = GetRegister(registerID);
			if (register != null) returnString = GetRegister(registerID).Access;
			return returnString;
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
			Boolean success = false;
			object returnValue = 0;

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

						//Convert the Raw Return Value to a Real Value
						switch (register.DataType.ToUpper())
						{
							case "U":
							case "H":
								switch (register.Size)
								{
									case 1:
										returnValue = readValue[0];
										break;
									case 2:
										returnValue = BitConverter.ToUInt16(readValue, 0);
										break;
									case 4:
										returnValue = BitConverter.ToUInt32(readValue, 0);
										break;
									default:
										break;
								}
								break;
							case "I":
								switch (register.Size)
								{
									case 1:
										returnValue = BitConverter.ToChar(readValue, 0);
										break;
									case 2:
										returnValue = BitConverter.ToInt16(readValue, 0);
										break;
									case 4:
										returnValue = BitConverter.ToInt32(readValue, 0);
										break;
									default:
										break;
								}
								break;
							default:
								break;
						}
						//Update the Last Read Raw Value
						register.LastReadRawValue = readValue;
						register.LastReadRawValueTimestamp = DateTime.Now;
						register.LastReadRawValueError = false;
						register.LastReadValueError = false;
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
					//Convert the Raw Return Value to a Real Value
					switch (register.DataType.ToUpper())
					{
						case "U":
						case "H":
							switch (register.Size)
							{
								case 1:
									returnValue = ((Byte[])register.LastReadRawValue)[0];
									break;
								case 2:
									returnValue = BitConverter.ToUInt16(((Byte[])register.LastReadRawValue), 0);
									break;
								case 4:
									returnValue = BitConverter.ToUInt32(((Byte[])register.LastReadRawValue), 0);
									break;
								default:
									break;
							}
							break;
						case "I":
							switch (register.Size)
							{
								case 1:
									returnValue = BitConverter.ToChar(((Byte[])register.LastReadRawValue), 0);
									break;
								case 2:
									returnValue = BitConverter.ToInt16(((Byte[])register.LastReadRawValue), 0);
									break;
								case 4:
									returnValue = BitConverter.ToInt32(((Byte[])register.LastReadRawValue), 0);
									break;
								default:
									break;
							}
							break;
						default:
							break;
					}
					//Indicate the Success
					success = true;
				}
			}

			//Return the Data to the Caller if Successful
			if (success) value = returnValue;

			return success;
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
				success = true;
			}

			return success;
		}
		public bool WriteRegister(Register register, object value)
		{
			Boolean success = false;

			if (register != null)
			{
				byte[] writeValue = new byte[register.Size];

				//Convert the Value to a Raw Value
				switch (register.DataType.ToUpper())
				{
					case "U":
					case "H":
						switch (register.Size)
						{
							case 1:
								writeValue = BitConverter.GetBytes(System.Convert.ToByte(value));
								break;
							case 2:
								writeValue = BitConverter.GetBytes(System.Convert.ToUInt16(value));
								break;
							case 4:
								writeValue = BitConverter.GetBytes(System.Convert.ToUInt32(value));
								break;
							default:
								break;
						}
						break;
					case "I":
						switch (register.Size)
						{
							case 1:
								writeValue = BitConverter.GetBytes(System.Convert.ToChar(value));
								break;
							case 2:
								writeValue = BitConverter.GetBytes(System.Convert.ToInt16(value));
								break;
							case 4:
								writeValue = BitConverter.GetBytes(System.Convert.ToInt32(value));
								break;
							default:
								break;
						}
						break;
					default:
						break;
				}

				//Write the Raw Value to the Register
				try
				{
					// remove any bits that are masked as not writable
					if (!_masterDevice._deviceManager.IsAuthenticated)
					{
						for (int i = 0; i < writeValue.Length; i++)
						{
							writeValue[i] = (byte)(writeValue[i] & register.LockBitMask[i]);
						}
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


			if (register != null)
			{
				Byte[] readValueRaw = new byte[register.Size];

				//If the Cached Value is Still Valid
				if (DateTime.Now.Subtract(register.LastReadValueTimestamp).TotalMilliseconds > register.REGISTER_CACHE_TIMEOUT.TotalMilliseconds)
				{
					try
					{
						//Read the Value from the device
						this.ReadBlock(register.Address, (int)register.Size, ref readValueRaw);

						//Convert the Raw Value to an Actual Value
						switch (register.DataType.ToUpper())
						{
							case "U":
							case "H":
								switch (register.Size)
								{
									case 1:
										Byte tempValueU8 = readValueRaw[0];
										readValue = System.Convert.ToDouble(tempValueU8);
										break;
									case 2:
										UInt16 tempValueU16 = BitConverter.ToUInt16(readValueRaw, 0);
										readValue = System.Convert.ToDouble(tempValueU16);
										break;
									case 4:
										UInt32 tempValueU32 = BitConverter.ToUInt32(readValueRaw, 0);
										readValue = System.Convert.ToDouble(tempValueU32);
										break;
									default:
										break;
								}
								break;
							case "I":
								switch (register.Size)
								{
									case 1:
										Char tempValue8 = BitConverter.ToChar(readValueRaw, 0);
										readValue = System.Convert.ToDouble(tempValue8);
										break;
									case 2:
										Int16 tempValue16 = BitConverter.ToInt16(readValueRaw, 0);
										readValue = System.Convert.ToDouble(tempValue16);
										break;
									case 4:
										Int32 tempValue32 = BitConverter.ToInt32(readValueRaw, 0);
										readValue = System.Convert.ToDouble(tempValue32);
										break;
									default:
										break;
								}
								break;
							default:
								break;
						}

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

						//Update the Last Read Raw Value
						register.LastReadRawValue = readValueRaw;
						register.LastReadRawValueTimestamp = DateTime.Now;
						register.LastReadRawValueError = false;
						register.LastReadValue = returnValue;
						register.LastReadValueTimestamp = DateTime.Now;
						register.LastReadValueError = false;

						// Store the bit values
						foreach (Register.Bit bit in register.Bits)
						{
							uint val = (uint)returnValue;
							val &= bit.Mask;
							val >>= _masterDevice.trailingZeros(bit.Mask);
							bit.LastReadValue = Convert.ToBoolean(val);
						}

						//Indicate the Success
						success = true;
					}
					catch (Exception e)
					{
						register.LastReadRawValueError = true;
						register.LastReadValueError = true;
						throw new Exception("Reading Value from Register (" + register.DisplayName + ")", e);
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


			if (register != null)
			{
				Byte[] readValueRaw = new byte[register.Size];

				//If the Cached Value is Still Valid
				if (DateTime.Now.Subtract(register.LastReadValueTimestamp).TotalMilliseconds > register.REGISTER_CACHE_TIMEOUT.TotalMilliseconds)
				{
					try
					{
						//Read the Value from the device
						this.ReadBlock(register.Address, (int)register.Size, ref readValueRaw);

						//Convert the Raw Value to an Actual Value
						switch (register.DataType.ToUpper())
						{
							case "U":
							case "H":
								switch (register.Size)
								{
									case 1:
										Byte tempValueU8 = readValueRaw[0];
										readValue = System.Convert.ToDouble(tempValueU8);
										break;
									case 2:
										UInt16 tempValueU16 = BitConverter.ToUInt16(readValueRaw, 0);
										readValue = System.Convert.ToDouble(tempValueU16);
										break;
									case 4:
										UInt32 tempValueU32 = BitConverter.ToUInt32(readValueRaw, 0);
										readValue = System.Convert.ToDouble(tempValueU32);
										break;
									default:
										break;
								}
								break;
							case "I":
								switch (register.Size)
								{
									case 1:
										Char tempValue8 = BitConverter.ToChar(readValueRaw, 0);
										readValue = System.Convert.ToDouble(tempValue8);
										break;
									case 2:
										Int16 tempValue16 = BitConverter.ToInt16(readValueRaw, 0);
										readValue = System.Convert.ToDouble(tempValue16);
										break;
									case 4:
										Int32 tempValue32 = BitConverter.ToInt32(readValueRaw, 0);
										readValue = System.Convert.ToDouble(tempValue32);
										break;
									default:
										break;
								}
								break;
							default:
								break;
						}

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

						//Update the Last Read Raw Value
						register.LastReadRawValue = readValueRaw;
						register.LastReadRawValueTimestamp = DateTime.Now;
						register.LastReadRawValueError = false;
						register.LastReadValue = returnValue;
						register.LastReadValueTimestamp = DateTime.Now;
						register.LastReadValueError = false;

						// Store the bit values
						foreach (Register.Bit bit in register.Bits)
						{
							uint val = (uint)returnValue;
							val &= bit.Mask;
							val >>= _masterDevice.trailingZeros(bit.Mask);
							bit.LastReadValue = Convert.ToBoolean(val);
						}

						//Indicate the Success
						success = true;
					}
					catch (Exception e)
					{
						register.LastReadRawValueError = true;
						register.LastReadValueError = true;
						throw new Exception("Reading Value from Register (" + register.DisplayName + ")", e);
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
			ReadRegisterValue(register, ref returnValue);
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

					//Convert the Value to a Raw Value
					switch (register.DataType.ToUpper())
					{
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
						default:
							break;
					}

					// remove any bits that are masked as not writable
					if (!_masterDevice._deviceManager.IsAuthenticated)
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
					returnValue >>= _masterDevice.trailingZeros(bit.Mask);

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
							writeValue = (byte)(ReadRegister(register));
							break;
						case 2:
							writeValue = (ushort)(ReadRegister(register));
							break;
						case 4:
							writeValue = (uint)(ReadRegister(register));
							break;
						default:
							break;
					}

					//Apply the Mask
					writeValue = (value == 0) ? writeValue ^ bit.Mask : writeValue | bit.Mask;

					// remove any bits that are masked as not writable
					if (!_masterDevice._deviceManager.IsAuthenticated)
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

					this.WriteRegisterValue(reg, (double)item.UIntValue);
					this.ReadRegisterValue(reg);
				}

				// Write system registers if access is open only.
				if (_masterDevice._deviceManager.IsAuthenticated)
				{
					foreach (var item in omap.System.Values)
					{
						Register reg = _allRegisters.Find(r => r.Address == (uint)item.Key);

						if (reg == null)
							throw new Exception("Unable to find system register address " + item.Key.ToString("X2"));

						this.WriteRegisterValue(reg, (double)item.Value);
						this.ReadRegisterValue(reg);
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
				System = new SystemData { Enc = !_masterDevice._deviceManager.IsAuthenticated }
			};

			await Task<object>.Run(() =>
			{
				foreach (Register reg in _allRegisters)
				{
					this.ReadRegisterValue(reg);
					if (reg.Private)
					{
						regMap.System.Values.Add((ushort)reg.Address, (ushort)reg.LastReadValue);
					}
					else
					{
						regMap.Registers.Add(
							new Map((byte)reg.Address, (ushort)reg.LastReadValue));
					}
				}
			});

			return regMap;
		}

		public Register GetRegisters(string regId)
		{
			return ((IRegister)_masterDevice).GetRegisters(regId);
		}

		#endregion

		#region Classes

		internal class SlaveAccess
		{
			public byte Command { get; set; }

			public byte Address { get; set; }

			public byte WriteData { get; set; }

			public byte ReadData { get; set; }

			public byte Status { get; set; }
		}

		#endregion
	}
}
