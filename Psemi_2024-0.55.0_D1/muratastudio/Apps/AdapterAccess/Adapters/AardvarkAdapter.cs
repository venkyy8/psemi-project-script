using System;
//using System.Collections.Generic;
using System.Text;
using System.Xml;

using TotalPhase;
using System.Collections.Generic;
using HardwareInterfaces;
using AdapterAccess.Protocols;

namespace AdapterAccess.Adapters
{
	/// <summary>
	/// Supports the Total Phase Aardvark I2C/SPI Adapter. It uses the 
	/// Aardvark .NET API.
	/// </summary>
	public class AardvarkAdapter : Adapter, IAdapter, II2c
	{
		#region Member Variables
		//////////////////////////////////////////////////////////////////
		// Member Variables
		//////////////////////////////////////////////////////////////////

		private List<string> _pluginCompatibility = new List<string>();
		private bool _isScanAllAddresses;
		private byte _defaultSlaveAddress;
		private AardvarkHardwareEnumeration _adapterInfo;
		private I2cCapabilities _i2cCapabilities;
		private DriverConfig _currentDriverConfig;

		/// <summary>
		/// Unique ID that can be used to track specific adapters between
		/// attachment and removal events.
		/// </summary>
		private uint _uniqueId;

		/// <summary>
		/// Unique ID that can be used to track specific adapters between
		/// attachment and removal events.
		/// </summary>
		public uint UniqueId
		{
			get { return _uniqueId; }
		}

		/// <summary>
		/// The current driver "port number" for the adapter (can change).
		/// </summary>
		private ushort _portNumber;

		/// <summary>
		/// Driver communication handle.
		/// </summary>
		private int _aardvarkHandle = (int)AardvarkStatus.AA_INVALID_HANDLE;

		/// <summary>
		/// Contains version and feature information.
		/// </summary>
		private AardvarkApi.AardvarkExt _aaExt;

		/// <summary>
		/// Used to synchronize the low level driver "scan" functionality.
		/// </summary>
		static object _getAllAdaptersLock = new object();

		#endregion Member Variables

		#region Constructor
		//////////////////////////////////////////////////////////////////
		// Constructor
		//////////////////////////////////////////////////////////////////
		public AardvarkAdapter(AardvarkHardwareEnumeration hw)
			: base()
		{
			_adapterInfo = hw;
			_uniqueId = hw.UniqueId;
			_portNumber = hw.PortNumber;

			if ((_portNumber & AardvarkApi.AA_PORT_NOT_FREE) == 0)
				_state = AdapterState.Available;
			else
				_state = AdapterState.NotAvailable;

			_supportedProtocols.Add(new ProtocolInfo(typeof(I2cProtocol), "I2C", true, true));
			//_supportedProtocols.Add( new ProtocolInfo( typeof( SpiProtocol ), "SPI", true, true ) );

			// Initialize the default driver configuration
			_currentDriverConfig.I2cPullups = AA_I2C_PULLUP_BOTH;
			_currentDriverConfig.I2cBitrate = 100;
			_currentDriverConfig.BusLockTimeout = 200;
			_currentDriverConfig.TargetPower = AA_TARGET_POWER_BOTH;
			_currentDriverConfig.SpiPolarity = AardvarkSpiPolarity.AA_SPI_POL_RISING_FALLING;
			_currentDriverConfig.SpiPhase = AardvarkSpiPhase.AA_SPI_PHASE_SAMPLE_SETUP;
			_currentDriverConfig.SpiBitorder = AardvarkSpiBitorder.AA_SPI_BITORDER_MSB;
			_currentDriverConfig.SpiBitrate = 125;
			_currentDriverConfig.SpiSsPolarity = AardvarkSpiSSPolarity.AA_SPI_SS_ACTIVE_HIGH; // Currently used to power I2C
			_currentDriverConfig.GpioDirection = 0x00; // All inputs
			_currentDriverConfig.GpioOutputs = 0x00; // Default to ouputing 0's
		}

		#endregion Constructor

		#region Properties and Information
		//////////////////////////////////////////////////////////////////
		// Properties and Information
		//////////////////////////////////////////////////////////////////

		public string AdapterName
		{
			get { return _adapterInfo.Name; }
		}

		public string AdapterVersion
		{
			get
			{
				var hw = AardvarkAdapter.VersionToString(_aaExt.version.hardware);
				var fw = AardvarkAdapter.VersionToString(_aaExt.version.firmware);
				var sw = AardvarkAdapter.VersionToString(_aaExt.version.software);
				var ver = string.Format("HW:{0} FW:{1} SW{2}", hw, fw, sw);
				return ver;
			}
		}

		public string AdapterSerialNumber
		{
			get { return _adapterInfo.UniqueId.ToString(); }
		}

		public ushort PortNumber
		{
			get { return _portNumber; }
		}

		public override bool IsInterfaceSupported(Type interfaceType)
		{
			if (interfaceType == typeof(II2c))
			{
				return true;
			}
			return false;
		}

		protected string GetI2cStatusString(int status)
		{
			return GetI2cStatusString((AardvarkI2cStatus)status);
		}

		protected string GetI2cStatusString(AardvarkI2cStatus status)
		{
			switch (status)
			{
				case AardvarkI2cStatus.AA_I2C_STATUS_ARB_LOST:
					return "Another master was arbitrating and gained control of the bus.";
				case AardvarkI2cStatus.AA_I2C_STATUS_BUS_ERROR:
					return "A bus error occurred.";
				case AardvarkI2cStatus.AA_I2C_STATUS_BUS_LOCKED:
					return "The bus is locked.";
				case AardvarkI2cStatus.AA_I2C_STATUS_DATA_NACK:
					return "The transaction was not acknowledged by the slave.";
				case AardvarkI2cStatus.AA_I2C_STATUS_LAST_DATA_ACK:
					return "The master acknowleded the last byte of the transfer.";
				case AardvarkI2cStatus.AA_I2C_STATUS_OK:
					return "No error.";
				case AardvarkI2cStatus.AA_I2C_STATUS_SLA_ACK:
					return "The adapter was adressed by another master and switched to slave mode.";
				case AardvarkI2cStatus.AA_I2C_STATUS_SLA_NACK:
					return "The slave address was not acknowledged.";
				default:
					return null;
			}
		}

		public bool IsScanAllAddresses
		{
			get { return _isScanAllAddresses; }
		}

		public byte DefaultSlaveAddress
		{
			get { return _defaultSlaveAddress; }
		}

		public List<string> PluginCompatibility
		{
			get { return _pluginCompatibility; }

			internal set
			{
				_pluginCompatibility = value;
			}
		}

		#endregion Properties and Information

		#region Open and Close
		//////////////////////////////////////////////////////////////////
		// Open and Close
		//////////////////////////////////////////////////////////////////

		protected override bool OpenDriverInterface()
		{
			_aardvarkHandle = AardvarkApi.aa_open_ext(_portNumber, ref _aaExt);

			if (_aardvarkHandle > 0)
			{
				// Enable SPI and I2C
				int result = AardvarkApi.aa_configure(_aardvarkHandle, AardvarkConfig.AA_CONFIG_SPI_I2C);
				if (result < 0)
				{
					throw new AdapterException("Error configuring the Aardvark Adapter: " + AardvarkApi.aa_status_string(result));
				}

				// Enable the I2C Pullups
				result = AardvarkApi.aa_i2c_pullup(_aardvarkHandle, _currentDriverConfig.I2cPullups);
				if (result < 0)
				{
					throw new AdapterException("Error configuring the I2C pullups: " + AardvarkApi.aa_status_string(result));
				}

				// Set the Bus Lock Timeout
				result = AardvarkApi.aa_i2c_bus_timeout(_aardvarkHandle, (ushort)_currentDriverConfig.BusLockTimeout);
				if (result < 0)
				{
					throw new AdapterException("Error setting the bus lock timeout: " + AardvarkApi.aa_status_string(result));
				}

				// Enable the Target Power
				result = AardvarkApi.aa_target_power(_aardvarkHandle, _currentDriverConfig.TargetPower);
				if (result < 0)
				{
					throw new AdapterException("Error configuring the target power setting: " + AardvarkApi.aa_status_string(result));
				}

				// Set I2C frequency to 100 kHz
				result = AardvarkApi.aa_i2c_bitrate(_aardvarkHandle, _currentDriverConfig.I2cBitrate);
				if (result < 0)
				{
					throw new AdapterException("Error setting the I2C bitrate: " + AardvarkApi.aa_status_string(result));
				}

				// Set SPI configuration
				result = AardvarkApi.aa_spi_configure(_aardvarkHandle,
					_currentDriverConfig.SpiPolarity,
					_currentDriverConfig.SpiPhase,
					_currentDriverConfig.SpiBitorder);
				if (result < 0)
				{
					throw new AdapterException("Error configuring the SPI interface: " + AardvarkApi.aa_status_string(result));
				}

				// Set SPI frequency to 125 kHz
				result = AardvarkApi.aa_spi_bitrate(_aardvarkHandle, _currentDriverConfig.SpiBitrate);
				if (result < 0)
				{
					throw new AdapterException("Error setting the SPI bitrate: " + AardvarkApi.aa_status_string(result));
				}

				// Set the Slave Select Polarity
				result = AardvarkApi.aa_spi_master_ss_polarity(_aardvarkHandle, _currentDriverConfig.SpiSsPolarity);
				if (result < 0)
				{
					throw new AdapterException("Error setting the SPI slave select polarity: " + AardvarkApi.aa_status_string(result));
				}
			}
			else
			{
				switch (_aardvarkHandle)
				{
					case (int)AardvarkStatus.AA_INCOMPATIBLE_DEVICE:
						// Check versions
						string message;
						if (_aaExt.version.sw_req_by_fw > _aaExt.version.software)
						{
							message = String.Format("The Aardvark Adapter requires a newer version of the Aardvark DLL. The current DLL version is {0}. The required version is {1}.",
								VersionToString(_aaExt.version.software), VersionToString(_aaExt.version.sw_req_by_fw));
						}
						else if (_aaExt.version.fw_req_by_sw > _aaExt.version.firmware)
						{
							message = String.Format("The Aardvark DLL requires a newer version of the Aardvark Adapter firmware. The current firmware version is {0}. The required version is {1}.",
								VersionToString(_aaExt.version.firmware), VersionToString(_aaExt.version.fw_req_by_sw));
						}
						else
						{
							message = AardvarkApi.aa_status_string(_aardvarkHandle);
						}
						throw new AdapterException(message);
					case (int)AardvarkStatus.AA_UNABLE_TO_OPEN:
						throw new AdapterException("Unable to open the Aardvark Adapter. It may have been removed or in use by another application.");
					default:
						throw new AdapterException(AardvarkApi.aa_status_string(_aardvarkHandle));
				}
			}

			return (_aardvarkHandle > 0);
		}

		protected override void CloseDriverInterface()
		{
			if (_aardvarkHandle != (int)AardvarkStatus.AA_INVALID_HANDLE)
			{
				AardvarkApi.aa_target_power(_aardvarkHandle, AA_TARGET_POWER_NONE);
				AardvarkApi.aa_close(_aardvarkHandle);

				_aardvarkHandle = (int)AardvarkStatus.AA_INVALID_HANDLE;
			}
		}

		/// <summary>
		/// Reconnects the low level hardware with an existing adapter object.
		/// </summary>
		/// <param name="hwInfo">Hardware enumeration information.</param>
		/// <returns>Success or failure.</returns>
		internal bool Resync(AardvarkHardwareEnumeration hwInfo)
		{
			// Update port number
			_portNumber = hwInfo.PortNumber;

			// Try to reopen the driver interface
			return OpenDriverInterface();
		}

		#endregion Open and Close

		#region Enumeration
		//////////////////////////////////////////////////////////////////
		// Enumeration
		//////////////////////////////////////////////////////////////////

		/// <summary>
		/// Returns basic information about all Aardvark adapters that are present.
		/// </summary>
		/// <returns>Adapter enumeration information.</returns>
		internal static AardvarkHardwareEnumeration[] GetAllHardwareAdapters()
		{
			ushort[] portNumbers = new ushort[1];
			uint[] uniqueIds = null;
			AardvarkHardwareEnumeration[] hwInfo = new AardvarkHardwareEnumeration[0];

			// Set initial "conditions" so that the while loop below will
			// execute at least once
			int initialDeviceCount = -1;
			int deviceCount = 0;

			// Only allow one thread to do the "scan" at a time
			lock (_getAllAdaptersLock)
			{
				// This loop captures the case where one or more devices were attached/removed
				// during the "find" process
				while (deviceCount != initialDeviceCount)
				{
					try
					{
						initialDeviceCount = AardvarkApi.aa_find_devices(1, portNumbers);
					}
					catch (Exception e)
					{
						System.Diagnostics.Debug.WriteLine(e.Message);
						throw e;
					}
					if (initialDeviceCount < 0)
					{
						// Some kind of error code
						break;
					}

					// Reallocate arrays for the number of expected devices
					portNumbers = new ushort[initialDeviceCount];
					uniqueIds = new uint[initialDeviceCount];

					// Get the IDs for all of the devices
					deviceCount = AardvarkApi.aa_find_devices_ext(initialDeviceCount, portNumbers, initialDeviceCount, uniqueIds);

					hwInfo = new AardvarkHardwareEnumeration[deviceCount];
					for (int i = 0; i < deviceCount; i++)
					{
						hwInfo[i] = new AardvarkHardwareEnumeration(portNumbers[i], uniqueIds[i]);
					}
				}
			}

			return hwInfo;
		}

		internal void UpdatePortNumber(ushort newPortNumber)
		{
			_portNumber = newPortNumber;
		}

		#endregion Enumeration

		#region II2c Members
		//////////////////////////////////////////////////////////////////
		// II2cAccess Members
		//////////////////////////////////////////////////////////////////

		public bool I2cSetDriveByZero(int drive)
		{
			return true;
		}

		public void I2cSetCapabilities(I2cCapabilities config)
		{
			_i2cCapabilities = config;
		}

		public I2cCapabilities I2cGetCapabilities()
		{
			return _i2cCapabilities;
		}

		public I2cConfiguration I2cGetConfiguration()
		{
			I2cConfiguration config = new I2cConfiguration();
			int result;

			// Has the adapter been opened?
			if (_aardvarkHandle != (int)AardvarkStatus.AA_INVALID_HANDLE)
			{
				// Get current bitrate
				result = AardvarkApi.aa_i2c_bitrate(_aardvarkHandle, 0);
				if (result < 0)
				{
					throw new Exception(AardvarkApi.aa_status_string(result));
				}
				_currentDriverConfig.I2cBitrate = result;

				// Get state of the pullups
				result = AardvarkApi.aa_i2c_pullup(_aardvarkHandle, AA_I2C_PULLUP_QUERY);
				if (result < 0)
				{
					throw new Exception(AardvarkApi.aa_status_string(result));
				}
				_currentDriverConfig.I2cPullups = (byte)result;

				// Get bus lock timeout
				result = AardvarkApi.aa_i2c_bus_timeout(_aardvarkHandle, 0);
				if (result < 0)
				{
					throw new Exception(AardvarkApi.aa_status_string(result));
				}
				_currentDriverConfig.BusLockTimeout = (ushort)result;
			}


			config.BitRate = BitRateToIndex(_currentDriverConfig.I2cBitrate);
			config.PullupsEnabled = (_currentDriverConfig.I2cPullups == AA_I2C_PULLUP_BOTH);
			config.BusLockTimeoutMs = _currentDriverConfig.BusLockTimeout;

			return config;
		}

		public void I2cSetConfiguration(I2cConfiguration config)
		{
			int result;
			_isScanAllAddresses = config.ScanAllAddresses;
			_defaultSlaveAddress = config.SlaveAddress;
			_currentDriverConfig.I2cBitrate = config.BitRate;
			_currentDriverConfig.I2cPullups = (config.PullupsEnabled ? AA_I2C_PULLUP_BOTH : AA_I2C_PULLUP_NONE);
			_currentDriverConfig.BusLockTimeout = (ushort)config.BusLockTimeoutMs;

			// Has the adapter been opened?
			if (_aardvarkHandle != (int)AardvarkStatus.AA_INVALID_HANDLE)
			{
				// Set bitrate
				result = AardvarkApi.aa_i2c_bitrate(_aardvarkHandle, _currentDriverConfig.I2cBitrate);
				if (result < 0)
				{
					throw new Exception(AardvarkApi.aa_status_string(result));
				}

				// Set state of the pullups
				result = AardvarkApi.aa_i2c_pullup(_aardvarkHandle, _currentDriverConfig.I2cPullups);
				if (result < 0)
				{
					throw new Exception(AardvarkApi.aa_status_string(result));
				}

				// Set bus lock timeout
				result = AardvarkApi.aa_i2c_bus_timeout(_aardvarkHandle, _currentDriverConfig.BusLockTimeout);
				if (result < 0)
				{
					throw new Exception(AardvarkApi.aa_status_string(result));
				}
			}
		}

		public void I2cSendByte(byte targetAddress, byte data)
		{
		}

		public void I2cEnable()
		{
		}

		public void I2cDisable()
		{
		}

		public void I2cWriteByte(byte targetAddress, byte address, byte data)
		{
			byte[] addressAndData = new byte[2];
			addressAndData[0] = address;
			addressAndData[1] = data;
			ushort bytesWritten = 0;

			int status = AardvarkApi.aa_i2c_write_ext(_aardvarkHandle, (ushort)(targetAddress), AardvarkI2cFlags.AA_I2C_NO_FLAGS, 2, addressAndData, ref bytesWritten);
			if (status != (int)AardvarkI2cStatus.AA_I2C_STATUS_OK)
			{
				// Free the bus (don't really think this is necessary)
				AardvarkApi.aa_i2c_free_bus(_aardvarkHandle);
				throw new AdapterException(GetI2cStatusString(status));
			}
		}

		public void I2cWriteBlock(byte targetAddress, byte? address, int length, ref byte[] data)
		{
			// Add the byteAddress to the start of the data array
			byte[] addressAndData = new byte[length + 1];
			addressAndData[0] = address.Value;
			Array.Copy(data, 0, addressAndData, 1, length);
			ushort bytesWritten = 0;

			int status = AardvarkApi.aa_i2c_write_ext(_aardvarkHandle, (ushort)(targetAddress), AardvarkI2cFlags.AA_I2C_NO_FLAGS, (ushort)(length + 1), addressAndData, ref bytesWritten);
			if (status != (int)AardvarkI2cStatus.AA_I2C_STATUS_OK)
			{
				// Free the bus (don't really think this is necessary)
				AardvarkApi.aa_i2c_free_bus(_aardvarkHandle);
				throw new AdapterException(GetI2cStatusString(status));
			}
		}

		public void I2cWrite16Bit(byte targetAddress, ushort? address, int length, ref byte[] data)
		{

		}

		public byte I2cReadByte(byte targetAddress, byte address)
		{
			byte[] data = new byte[1];
			I2cReadBlock(targetAddress, address, 1, ref data);
			return data[0];
		}

		public void I2cReadBlock(byte targetAddress, byte address, int length, ref byte[] data)
		{
			// Shift the target byteAddress down 1 bit to accomodate the way the Aarvark handles the addressing
			ushort slaveAddress = (ushort)(targetAddress);
			// Write the register byteAddress
			byte[] registerAddress = new byte[1];
			registerAddress[0] = address;
			int status = 0;

			ushort bytesWritten = 0;
			status = AardvarkApi.aa_i2c_write_ext(_aardvarkHandle, slaveAddress, AardvarkI2cFlags.AA_I2C_NO_STOP, 1, registerAddress, ref bytesWritten);
			if (status != (int)AardvarkI2cStatus.AA_I2C_STATUS_OK)
			{
				// Need to "free" the bus
				AardvarkApi.aa_i2c_free_bus(_aardvarkHandle);
				throw new AdapterException(GetI2cStatusString(status));
			}

			ushort bytesRead = 0;
			status = AardvarkApi.aa_i2c_read_ext(_aardvarkHandle, slaveAddress, AardvarkI2cFlags.AA_I2C_NO_FLAGS, (ushort)length, data, ref bytesRead);
			if (status != (int)AardvarkI2cStatus.AA_I2C_STATUS_OK)
			{
				throw new AdapterException(GetI2cStatusString(status));
			}
		}

		public void I2cRead16Bit(byte targetAddress, ushort address, int length, ref byte[] data)
		{

		}

		public void I2cSendStop()
		{
			AardvarkApi.aa_i2c_free_bus(_aardvarkHandle);
		}

		public bool I2cSetBitRate(int bitRate)
		{
			I2cConfiguration config = I2cGetConfiguration();
			config.ScanAllAddresses = _isScanAllAddresses;
			config.SlaveAddress = _defaultSlaveAddress;
			var bitRates = ((II2c)this).I2cGetCapabilities().SupportedBitRates;
			config.BitRate = IndexToBitRate(bitRate);
			I2cSetConfiguration(config);
			return true;
		}

		public void SetGpio(byte value)
		{
			//throw new NotImplementedException();
		}

		#endregion II2cAccess Members

		#region Object Overrides
		//////////////////////////////////////////////////////////////////
		// Object Overrides
		//////////////////////////////////////////////////////////////////

		public override int GetHashCode()
		{
			return (int)_uniqueId;
		}

		public override bool Equals(object obj)
		{
			// If the object isn't the same type, then return false
			if (obj is AardvarkAdapter)
			{
				AardvarkAdapter a1 = (AardvarkAdapter)obj;
				return (a1 == this);
			}
			else
			{
				return false;
			}
		}

		public override string ToString()
		{
			return _adapterInfo.Name + " - S/N: " + _adapterInfo.UniqueId;
		}

		#endregion Object Overrides

		#region Utilities
		//////////////////////////////////////////////////////////////////
		// Utilities
		//////////////////////////////////////////////////////////////////

		internal static string VersionToString(ushort version)
		{
			return String.Format("{0}.{1:#00}", (byte)(version >> 8), (byte)version);
		}

		public static bool operator ==(AardvarkAdapter a1, AardvarkAdapter a2)
		{
			// If both objects reference the same instance (or null), return true
			if (Object.ReferenceEquals(a1, a2))
				return true;

			// If one of the objects (but not both) is null, return false
			if (((object)a1 == null) || ((object)a2) == null)
				return false;

			// Return true if the _uniqueId fields match
			return (a1._uniqueId == a2._uniqueId);
		}

		public static bool operator !=(AardvarkAdapter a1, AardvarkAdapter a2)
		{
			return !(a1 == a2);
		}

		#endregion Utilities

		#region Constants
		//////////////////////////////////////////////////////////////////
		// Constants
		//////////////////////////////////////////////////////////////////

		// Constants that appear to be missing from the Aardvark .NET API
		public const byte AA_I2C_PULLUP_NONE = 0x00;
		public const byte AA_I2C_PULLUP_BOTH = 0x03;
		public const byte AA_I2C_PULLUP_QUERY = 0x80;
		public const byte AA_TARGET_POWER_NONE = 0x00;
		public const byte AA_TARGET_POWER_BOTH = 0x03;
		public const byte AA_TARGET_POWER_QUERY = 0x80;
		private const int AA_NUMBER_OF_VOUT_PINS = 1;

		#endregion Constants

		#region Data Types
		//////////////////////////////////////////////////////////////////
		// Data Types
		//////////////////////////////////////////////////////////////////

		private struct DriverConfig
		{
			public byte I2cPullups;
			public int I2cBitrate;
			public ushort BusLockTimeout;
			public byte TargetPower;
			public AardvarkSpiPolarity SpiPolarity;
			public AardvarkSpiPhase SpiPhase;
			public AardvarkSpiBitorder SpiBitorder;
			public int SpiBitrate;
			public AardvarkSpiSSPolarity SpiSsPolarity;
			public byte GpioDirection;
			public byte GpioOutputs;
		}

		#endregion Data Types

		private int IndexToBitRate(int index)
		{
			var bitRates = ((II2c)this).I2cGetCapabilities().SupportedBitRates;
			return bitRates[index];
		}

		private int BitRateToIndex(int bitRate)
		{
			var bitRates = ((II2c)this).I2cGetCapabilities().SupportedBitRates;
			for (int i = 0; i < bitRates.GetUpperBound(0); i++)
			{
				if (bitRate == bitRates[i])
				{
					return i;
				}
			}
			return -1;
		}
	}

	/// <summary>
	/// Contains information about the driver level enumeration
	/// of an Aardvark hardware adapter.
	/// </summary>
	public class AardvarkHardwareEnumeration
	{
		/// <summary>
		/// Driver assigned port number. This number may be different if
		/// the adapter is removed and reattached.
		/// </summary>
		public ushort PortNumber;

		/// <summary>
		/// Unique ID that can be used to track specific adapters between
		/// attachment and removal events.
		/// </summary>
		public uint UniqueId;

		/// <summary>
		/// Indicates this instance has been matched with an existing
		/// adapter object.
		/// </summary>
		public bool Claimed;

		/// <summary>
		/// Indicates the hardware adapter can be used by this software.
		/// </summary>
		public bool Free;

		public string Name { get; set; }

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="portNumber">Driver assigned port number.</param>
		/// <param name="uniqueId">Unique ID.</param>
		public AardvarkHardwareEnumeration(ushort portNumber, uint uniqueId)
		{
			PortNumber = portNumber;
			UniqueId = uniqueId;
			Claimed = false;
			Free = ((PortNumber & AardvarkApi.AA_PORT_NOT_FREE) == 0);
			Name = "Aardvark";
		}
	}
}
