using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using AdapterAccess.Protocols;
using System.Xml.Linq;
using HardwareInterfaces;
using System.IO;

namespace AdapterAccess.Adapters
{
	public class FtdiAdapter : Adapter, IAdapter, II2c
	{
		#region Constant Members

		/* Options to I2C_DeviceWrite & I2C_DeviceRead */
		/*Generate start condition before transmitting */
		public const uint I2C_TRANSFER_OPTIONS_START_BIT = 0x00000001;

		/*Generate stop condition before transmitting */
		public const uint I2C_TRANSFER_OPTIONS_STOP_BIT = 0x00000002;

		/*Continue transmitting data in bulk without caring about Ack or nAck from device if this bit is
		not set. If this bit is set then stop transitting the data in the buffer when the device nAcks*/
		public const uint I2C_TRANSFER_OPTIONS_BREAK_ON_NACK = 0x00000004;

		/* libMPSSE-I2C generates an ACKs for every byte read. Some I2C slaves require the I2C
		master to generate a nACK for the last data byte read. Setting this bit enables working with such
		I2C slaves */
		public const uint I2C_TRANSFER_OPTIONS_NACK_LAST_BYTE = 0x00000008;

		/* no address phase, no USB interframe delays */
		private const uint I2C_TRANSFER_OPTIONS_FAST_TRANSFER_BYTES	= 0x00000010;
		private const uint I2C_TRANSFER_OPTIONS_FAST_TRANSFER_BITS = 0x00000020;
		private const uint I2C_TRANSFER_OPTIONS_FAST_TRANSFER = 0x00000030;

		/* if I2C_TRANSFER_OPTION_FAST_TRANSFER is set then setting this bit would mean that the
		address field should be ignored. The address is either a part of the data or this is a special I2C
		frame that doesn't require an address*/
		private const uint I2C_TRANSFER_OPTIONS_NO_ADDRESS = 0x00000040;

		private const uint I2C_CMD_GETDEVICEID_RD = 0xF9;
		private const uint I2C_CMD_GETDEVICEID_WR = 0xF8;

		private const uint I2C_GIVE_ACK = 1;
		private const uint I2C_GIVE_NACK = 0;

		/* 3-phase clocking is enabled by default. Setting this bit in ConfigOptions will disable it */
		private const uint I2C_DISABLE_3PHASE_CLOCKING = 0x0001;

		/* The I2C master should actually drive the SDA line only when the output is LOW. It should be
		tristate the SDA line when the output should be high. This tristating the SDA line during output
		HIGH is supported only in FT232H chip. This feature is called DriveOnlyZero feature and is
		enabled when the following bit is set in the options parameter in function I2C_Init */
		private const uint I2C_ENABLE_DRIVE_ONLY_ZERO = 0x0002;

		private const uint FTC_SUCCESS = 0;
		private const uint FTC_DEVICE_IN_USE = 27;

		// Constants for FT_STATUS
		/// <summary>
		/// Status values for FTDI devices.
		/// </summary>
		public enum FT_STATUS
		{
			/// <summary>
			/// Status OK
			/// </summary>
			FT_OK = 0,
			/// <summary>
			/// The device handle is invalid
			/// </summary>
			FT_INVALID_HANDLE,
			/// <summary>
			/// Device not found
			/// </summary>
			FT_DEVICE_NOT_FOUND,
			/// <summary>
			/// Device is not open
			/// </summary>
			FT_DEVICE_NOT_OPENED,
			/// <summary>
			/// IO error
			/// </summary>
			FT_IO_ERROR,
			/// <summary>
			/// Insufficient resources
			/// </summary>
			FT_INSUFFICIENT_RESOURCES,
			/// <summary>
			/// A parameter was invalid
			/// </summary>
			FT_INVALID_PARAMETER,
			/// <summary>
			/// The requested baud rate is invalid
			/// </summary>
			FT_INVALID_BAUD_RATE,
			/// <summary>
			/// Device not opened for erase
			/// </summary>
			FT_DEVICE_NOT_OPENED_FOR_ERASE,
			/// <summary>
			/// Device not poened for write
			/// </summary>
			FT_DEVICE_NOT_OPENED_FOR_WRITE,
			/// <summary>
			/// Failed to write to device
			/// </summary>
			FT_FAILED_TO_WRITE_DEVICE,
			/// <summary>
			/// Failed to read the device EEPROM
			/// </summary>
			FT_EEPROM_READ_FAILED,
			/// <summary>
			/// Failed to write the device EEPROM
			/// </summary>
			FT_EEPROM_WRITE_FAILED,
			/// <summary>
			/// Failed to erase the device EEPROM
			/// </summary>
			FT_EEPROM_ERASE_FAILED,
			/// <summary>
			/// An EEPROM is not fitted to the device
			/// </summary>
			FT_EEPROM_NOT_PRESENT,
			/// <summary>
			/// Device EEPROM is blank
			/// </summary>
			FT_EEPROM_NOT_PROGRAMMED,
			/// <summary>
			/// Invalid arguments
			/// </summary>
			FT_INVALID_ARGS,
			/// <summary>
			/// An other error has occurred
			/// </summary>
			FT_OTHER_ERROR
		};

		// Constants for other error states internal to this class library
		/// <summary>
		/// Error states not supported by FTD2XX DLL.
		/// </summary>
		private enum FT_ERROR
		{
			FT_NO_ERROR = 0,
			FT_INCORRECT_DEVICE,
			FT_INVALID_BITMODE,
			FT_BUFFER_SIZE
		};

		#endregion

		#region Private Members

		private FtDeviceListInfoNode _adapterInfo;
		private ChannelCfg _chcfg;
		private List<string> _pluginCompatibility = new List<string>();
		private uint _channel = 0;
		private bool _isFastMode;
		private bool _isScanAllAddresses;
		private byte _defaultSlaveAddress;

		/// <summary>
		/// Used to synchronize the low level driver "scan" functionality.
		/// </summary>
		static object _getAllAdaptersLock = new object();

		#endregion

		#region Constructors

		internal FtdiAdapter(uint channel)
		{
			this._channel = channel;
			_supportedProtocols.Add(new ProtocolInfo(typeof(I2cProtocol), "I2C", true, true));
			
			ConfigureDefaultAdapterSettings();
		}

		internal FtdiAdapter(uint channel, FtDeviceListInfoNode adapterInfo)
		{
			this._channel = channel;
			this._adapterInfo = adapterInfo;
			_supportedProtocols.Add(new ProtocolInfo(typeof(I2cProtocol), "I2C", true, true));

			ConfigureDefaultAdapterSettings();
		} 

		#endregion

		#region Overrides

		public string AdapterName
		{
			get { return _adapterInfo.Description; }
		}

		public string AdapterVersion
		{
			get { return _adapterInfo.LocId.ToString(); }
		}

		public string AdapterSerialNumber
		{
			get { return _adapterInfo.SerialNumber; }
		}

		public List<string> PluginCompatibility
		{
			get { return _pluginCompatibility; }

			internal set
			{
				_pluginCompatibility = value;
			}
		}

		public override bool IsInterfaceSupported(Type interfaceType)
		{
			if (interfaceType == typeof(II2c))
			{
				return true;
			}
			return false;
		}

		public override string ToString()
		{
			return _adapterInfo.Description + " - S/N: " + _adapterInfo.SerialNumber;
		}

		#endregion

		#region Open and Close

		protected override bool OpenDriverInterface()
		{
			Init_libMPSSE();
			uint status = I2C_OpenChannel(_channel, ref _adapterInfo.FtHandle);
			if (status != 0)
			{
				// MessageBox.Show("FTDI error while open channel " + channel);
				I2C_CloseChannel(_adapterInfo.FtHandle);
				return false;
			}

			status = I2C_InitChannel(_adapterInfo.FtHandle, ref _chcfg);

			if (status != 0)
			{
				// MessageBox.Show("FTDI error while init channel " + ch);
				I2C_CloseChannel(_adapterInfo.FtHandle);
				return false;
			}

			return true;
		}

		protected override void CloseDriverInterface()
		{
			if (_adapterInfo.FtHandle == IntPtr.Zero)
			{
				return;
			}

			I2C_CloseChannel(_adapterInfo.FtHandle);
			_adapterInfo.FtHandle = IntPtr.Zero;

			Cleanup_libMPSSE();
			return;
		}

		protected override bool ReconfigureInterface()
		{
			return (I2C_InitChannel(_adapterInfo.FtHandle, ref _chcfg) == FTC_SUCCESS);
		}

		#endregion

		#region I2c Interface

		public I2cCapabilities I2cGetCapabilities()
		{
			return new I2cCapabilities
			{
				SupportedBitRates = new int[] { 100000, 400000, 1000000, 3400000 },
				StopCommand = false,
				SoftwareControlledPullups = false,
				SupportedBusLockTimeouts = new int[] { 0 }
			};
		}

		public I2cConfiguration I2cGetConfiguration()
		{
			return new I2cConfiguration
			{
				BitRate = (int)_chcfg.ClockRate,
				BusLockTimeoutMs = 0,
				PullupsEnabled = false
			};
		}

		public bool I2cSetBitRate(int bitRate)
		{
			_chcfg.ClockRate = (uint)bitRate;

			if (bitRate > 100000)
			{
				_chcfg.LatencyTimer = 1;
				_isFastMode = true;
			}
			else
			{
				_chcfg.LatencyTimer = 255;
				_isFastMode = false;
			}

			_chcfg.Options = I2C_ENABLE_DRIVE_ONLY_ZERO;

			return ReconfigureInterface();
		}

		public bool I2cSetDriveByZero(int drive)
		{
			return true;
		}

		public void I2cSetConfiguration(I2cConfiguration config)
		{
			//I2C_CLOCK_STANDARD_MODE = 100000,		/* 100kb/sec LatencyTimer = 255 */
			//I2C_CLOCK_FAST_MODE = 400000,			/* 400kb/sec LatencyTimer = 1 */
			//I2C_CLOCK_FAST_MODE_PLUS = 1000000, 	/* 1000kb/sec LatencyTimer = 1 */
			//I2C_CLOCK_HIGH_SPEED_MODE = 3400000 	/* 3.4Mb/sec LatencyTimer = 1 */

			_chcfg.ClockRate = (uint)config.BitRate;

			if (config.BitRate > 100000)
			{
				_chcfg.LatencyTimer = 1;
				_isFastMode = true;
			}
			else
			{
				_chcfg.LatencyTimer = 255;
				_isFastMode = false;
			}

			_chcfg.Options = I2C_ENABLE_DRIVE_ONLY_ZERO;
			_isScanAllAddresses = config.ScanAllAddresses;
			_defaultSlaveAddress = config.SlaveAddress;
		}

		public void I2cEnable()
		{
		}

		public void I2cDisable()
		{
		}

		public void I2cWriteByte(byte targetAddress, byte address, byte data)
		{
			byte[] bdata = new byte[] { data };
			I2cWriteBlock(targetAddress, address, 1, bdata);
		}

		public void I2cWriteBlock(byte targetAddress, byte address, int length, byte[] data)
		{
			CheckAdapterPresents();
			uint status = 0;

			byte[] addressAndData = new byte[length + 1];
			addressAndData[0] = address;
			Array.Copy(data, 0, addressAndData, 1, length);

			uint options = 0;
			if (_isFastMode)
			{
				options = 
					I2C_TRANSFER_OPTIONS_START_BIT | 
					I2C_TRANSFER_OPTIONS_STOP_BIT | 
					I2C_TRANSFER_OPTIONS_FAST_TRANSFER_BYTES;
			}
			else
			{
				options = 
					I2C_TRANSFER_OPTIONS_START_BIT | 
					I2C_TRANSFER_OPTIONS_STOP_BIT;
			}

			uint sizeTransfered = 0;

			if ((status = I2C_DeviceWrite(_adapterInfo.FtHandle, targetAddress, (uint)length + 1, addressAndData, ref sizeTransfered, options)) != FTC_SUCCESS)
			{
				ErrorHandler((FT_STATUS)status, 0);
			}           
			return;
		}

		public byte I2cReadByte(byte targetAddress, byte address)
		{
			byte[] data = new byte[1];
			I2cReadBlock(targetAddress, address, 1, ref data);
			return data[0];
		}

		public void I2cReadBlock(byte targetAddress, byte address, int length, ref byte[] data)
		{
			CheckAdapterPresents();
			uint status = 0;
			uint options = 0;

			// --------------------------------------------------------------------
			// Write Address
			// --------------------------------------------------------------------
			if (_isFastMode)
			{
				options = 
					I2C_TRANSFER_OPTIONS_START_BIT | I2C_TRANSFER_OPTIONS_FAST_TRANSFER_BYTES;
					//I2C_TRANSFER_OPTIONS_STOP_BIT |
			}
			else
			{
				options = 
					I2C_TRANSFER_OPTIONS_START_BIT; // | I2C_TRANSFER_OPTIONS_STOP_BIT;
			}

			uint sizeTransfered = 0;

			byte[] bdata = new byte[] { address };
			
			if ((status = I2C_DeviceWrite(_adapterInfo.FtHandle, targetAddress, 1, bdata, ref sizeTransfered, options))!= FTC_SUCCESS)
			{
				ErrorHandler((FT_STATUS)status, 0);
			}

			// --------------------------------------------------------------------
			// Read data
			// --------------------------------------------------------------------
			if (_isFastMode)
			{
				options = 
					I2C_TRANSFER_OPTIONS_START_BIT |
					I2C_TRANSFER_OPTIONS_STOP_BIT |
					I2C_TRANSFER_OPTIONS_NACK_LAST_BYTE |
					I2C_TRANSFER_OPTIONS_FAST_TRANSFER_BYTES;
			}
			else
			{
				options = 
					I2C_TRANSFER_OPTIONS_START_BIT |
					I2C_TRANSFER_OPTIONS_STOP_BIT |
					I2C_TRANSFER_OPTIONS_NACK_LAST_BYTE;
			}
			
			if ((status = I2C_DeviceRead(_adapterInfo.FtHandle, targetAddress, (uint)length, data, ref sizeTransfered, options)) != FTC_SUCCESS)
			{
				ErrorHandler((FT_STATUS)status, 0);
			}

			return;
		}

		public void I2cSendStop()
		{
			CheckAdapterPresents();
			uint status = 0;

			uint options = 0;
			if (_isFastMode)
			{
				options = I2C_TRANSFER_OPTIONS_STOP_BIT |
					I2C_TRANSFER_OPTIONS_FAST_TRANSFER_BYTES;
			}
			else
			{
				options = I2C_TRANSFER_OPTIONS_STOP_BIT;
			}

			uint sizeTransfered = 0;

			if ((status = I2C_DeviceWrite(_adapterInfo.FtHandle, 0, 0, new byte[]{0}, ref sizeTransfered, options)) != FTC_SUCCESS)
			{
				//ErrorHandler((FT_STATUS)status, 0);
			}
			return;
		}

		public bool IsScanAllAddresses 
		{
			get { return _isScanAllAddresses; } 
		}

		public byte DefaultSlaveAddress 
		{
			get { return _defaultSlaveAddress; }
		}

		public void SetGpio(byte value)
		{
		}

		#endregion

		#region Enumeration

		internal static List<FtDeviceListInfoNode> GetAllHardwareAdapters()
		{
			List<FtDeviceListInfoNode> adapters = new List<FtDeviceListInfoNode>();
			uint channels = 0;
			uint status = 0;

			// Check for the existance of the FTDI Driver DLL.
			// If missing then throw an error.
			if(!File.Exists(Path.Combine(Environment.SystemDirectory, "ftd2xx.dll")))
			{
				throw new Exception("USB adapter driver is not installed. Please see the user's guide for installation instructions.");
			}

			lock (_getAllAdaptersLock)
			{
				Init_libMPSSE();

				// TODO get all return codes and handle
				status = I2C_GetNumChannels(ref channels);

				for (uint channel = 0; channel < channels; channel++)
				{
					FtDeviceListInfoNode ftInfo = new FtDeviceListInfoNode();
					status = I2C_GetChannelInfo(channel, ref ftInfo);
					adapters.Add(ftInfo);
				}
				Cleanup_libMPSSE();
			}
			
			return adapters;
		} 

		internal FtDeviceListInfoNode GetAdapterInfo()
		{
			return this._adapterInfo;
		}
		internal bool Resync(FtDeviceListInfoNode ftdiInfo)
		{
			return OpenDriverInterface();
		}


		#endregion

		#region Private Methods

		private void CheckAdapterPresents()
		{
			if (_adapterInfo.FtHandle == IntPtr.Zero)
			{
				throw new AdapterException("No adapter configured");
			}
		}

		private void ConfigureDefaultAdapterSettings()
		{
			//I2C_CLOCK_STANDARD_MODE = 100000,		/* 100kb/sec LatencyTimer = 255 */
			//I2C_CLOCK_FAST_MODE = 400000,			/* 400kb/sec LatencyTimer = 1 */
			//I2C_CLOCK_FAST_MODE_PLUS = 1000000, 	/* 1000kb/sec LatencyTimer = 1 */
			//I2C_CLOCK_HIGH_SPEED_MODE = 3400000 	/* 3.4Mb/sec LatencyTimer = 1 */

			_chcfg.ClockRate = 100000;
			_chcfg.LatencyTimer = 1;
			_chcfg.Options = I2C_ENABLE_DRIVE_ONLY_ZERO;
			_isFastMode = false;
			_defaultSlaveAddress = 0x60;
			_isScanAllAddresses = true;
		}

		#endregion

		#region libMPSSE Dll Imports

		// Built-in Windows API functions to allow us to dynamically load our own DLL.
		[DllImportAttribute("libMPSSE.dll", EntryPoint = "I2C_GetNumChannels", CallingConvention = CallingConvention.Cdecl)]
		static extern uint I2C_GetNumChannels(ref uint NumChannels);

		[DllImportAttribute("libMPSSE.dll", EntryPoint = "I2C_GetChannelInfo", CallingConvention = CallingConvention.Cdecl)]
		static extern uint I2C_GetChannelInfo(uint index, ref FtDeviceListInfoNode chanInfo);

		[DllImportAttribute("libMPSSE.dll", EntryPoint = "I2C_OpenChannel", CallingConvention = CallingConvention.Cdecl)]
		static extern uint I2C_OpenChannel(uint index, ref IntPtr handler);

		[DllImportAttribute("libMPSSE.dll", EntryPoint = "I2C_CloseChannel", CallingConvention = CallingConvention.Cdecl)]
		static extern uint I2C_CloseChannel(IntPtr handler);

		[DllImportAttribute("libMPSSE.dll", EntryPoint = "I2C_InitChannel", CallingConvention = CallingConvention.Cdecl)]
		static extern uint I2C_InitChannel(IntPtr handler, ref ChannelCfg config);

		[DllImportAttribute("libMPSSE.dll", EntryPoint = "I2C_DeviceRead", CallingConvention = CallingConvention.Cdecl)]
		static extern uint I2C_DeviceRead(IntPtr handler, UInt32 deviceAddress, UInt32 sizeToTransfer, byte[] buffer, ref UInt32 sizeTransfered, UInt32 options);

		[DllImportAttribute("libMPSSE.dll", EntryPoint = "I2C_DeviceWrite", CallingConvention = CallingConvention.Cdecl)]
		static extern uint I2C_DeviceWrite(IntPtr handler, UInt32 deviceAddress, UInt32 sizeToTransfer, byte[] buffer, ref UInt32 sizeTransfered, UInt32 options);

		[DllImportAttribute("libMPSSE.dll", EntryPoint = "FT_WriteGPIO", CallingConvention = CallingConvention.Cdecl)]
		static extern uint FT_WriteGPIO(IntPtr handler, byte dir, byte value);

		[DllImportAttribute("libMPSSE.dll", EntryPoint = "FT_ReadGPIO", CallingConvention = CallingConvention.Cdecl)]
		static extern uint FT_ReadGPIO(IntPtr handler, ref  byte value);

		[DllImportAttribute("libMPSSE.dll", EntryPoint = "Init_libMPSSE", CallingConvention = CallingConvention.Cdecl)]
		static extern void Init_libMPSSE();

		[DllImportAttribute("libMPSSE.dll", EntryPoint = "Cleanup_libMPSSE", CallingConvention = CallingConvention.Cdecl)]
		static extern void Cleanup_libMPSSE();

		#endregion

		#region EXCEPTION_HANDLING
		/// <summary>
		/// Exceptions thrown by errors within the FTDI class.
		/// </summary>
		[global::System.Serializable]
		public class FT_EXCEPTION : Exception
		{
			/// <summary>
			/// 
			/// </summary>
			public FT_EXCEPTION() { }
			/// <summary>
			/// 
			/// </summary>
			/// <param name="message"></param>
			public FT_EXCEPTION(string message) : base(message) { }
			/// <summary>
			/// 
			/// </summary>
			/// <param name="message"></param>
			/// <param name="inner"></param>
			public FT_EXCEPTION(string message, Exception inner) : base(message, inner) { }
			/// <summary>
			/// 
			/// </summary>
			/// <param name="info"></param>
			/// <param name="context"></param>
			protected FT_EXCEPTION(
			System.Runtime.Serialization.SerializationInfo info,
			System.Runtime.Serialization.StreamingContext context)
				: base(info, context) { }
		}
		#endregion

		#region HELPER_METHODS
		//**************************************************************************
		// ErrorHandler
		//**************************************************************************
		/// <summary>
		/// Method to check ftStatus and ftErrorCondition values for error conditions and throw exceptions accordingly.
		/// </summary>
		private void ErrorHandler(FT_STATUS ftStatus, FT_ERROR ftErrorCondition)
		{
			if (ftStatus != FT_STATUS.FT_OK)
			{
				// Check FT_STATUS values returned from FTD2XX DLL calls
				switch (ftStatus)
				{
					case FT_STATUS.FT_DEVICE_NOT_FOUND:
						{
							throw new FT_EXCEPTION("FTDI: Target device not found. Please verify the device is connected to the adapter.");
						}
					case FT_STATUS.FT_DEVICE_NOT_OPENED:
						{
							throw new FT_EXCEPTION("FTDI device not opened.");
						}
					case FT_STATUS.FT_DEVICE_NOT_OPENED_FOR_ERASE:
						{
							throw new FT_EXCEPTION("FTDI device not opened for erase.");
						}
					case FT_STATUS.FT_DEVICE_NOT_OPENED_FOR_WRITE:
						{
							throw new FT_EXCEPTION("FTDI device not opened for write.");
						}
					case FT_STATUS.FT_EEPROM_ERASE_FAILED:
						{
							throw new FT_EXCEPTION("Failed to erase FTDI device EEPROM.");
						}
					case FT_STATUS.FT_EEPROM_NOT_PRESENT:
						{
							throw new FT_EXCEPTION("No EEPROM fitted to FTDI device.");
						}
					case FT_STATUS.FT_EEPROM_NOT_PROGRAMMED:
						{
							throw new FT_EXCEPTION("FTDI device EEPROM not programmed.");
						}
					case FT_STATUS.FT_EEPROM_READ_FAILED:
						{
							throw new FT_EXCEPTION("Failed to read FTDI device EEPROM.");
						}
					case FT_STATUS.FT_EEPROM_WRITE_FAILED:
						{
							throw new FT_EXCEPTION("Failed to write FTDI device EEPROM.");
						}
					case FT_STATUS.FT_FAILED_TO_WRITE_DEVICE:
						{
							throw new FT_EXCEPTION("Failed to write to FTDI device.");
						}
					case FT_STATUS.FT_INSUFFICIENT_RESOURCES:
						{
							throw new FT_EXCEPTION("Insufficient resources.");
						}
					case FT_STATUS.FT_INVALID_ARGS:
						{
							throw new FT_EXCEPTION("Invalid arguments for FTD2XX function call.");
						}
					case FT_STATUS.FT_INVALID_BAUD_RATE:
						{
							throw new FT_EXCEPTION("Invalid Baud rate for FTDI device.");
						}
					case FT_STATUS.FT_INVALID_HANDLE:
						{
							throw new FT_EXCEPTION("Invalid handle for FTDI device.");
						}
					case FT_STATUS.FT_INVALID_PARAMETER:
						{
							throw new FT_EXCEPTION("Invalid parameter for FTD2XX function call.");
						}
					case FT_STATUS.FT_IO_ERROR:
						{
							throw new FT_EXCEPTION("FTDI device IO error. Please verify the adapter is connected.");
						}
					case FT_STATUS.FT_OTHER_ERROR:
						{
							throw new FT_EXCEPTION("An unexpected error has occurred when trying to communicate with the FTDI device.");
						}
					default:
						break;
				}
			}

			if (ftErrorCondition != FT_ERROR.FT_NO_ERROR)
			{
				// Check for other error conditions not handled by FTD2XX DLL
				switch (ftErrorCondition)
				{
					case FT_ERROR.FT_INCORRECT_DEVICE:
						{
							throw new FT_EXCEPTION("The current device type does not match the EEPROM structure.");
						}
					case FT_ERROR.FT_INVALID_BITMODE:
						{
							throw new FT_EXCEPTION("The requested bit mode is not valid for the current device.");
						}
					case FT_ERROR.FT_BUFFER_SIZE:
						{
							throw new FT_EXCEPTION("The supplied buffer is not big enough.");
						}

					default:
						break;
				}
			}
			return;
		}
		#endregion
	}

	#region Structures

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct ChannelCfg
	{
		public UInt32 ClockRate;
		public byte LatencyTimer;
		public UInt32 Options;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct FtDeviceListInfoNode
	{
		public uint Flags;
		public uint Type;
		public uint ID;
		public uint LocId;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
		public string SerialNumber;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
		public string Description;
		public IntPtr FtHandle;
		public bool Claimed;
		public bool Free;
	}

	#endregion
}
