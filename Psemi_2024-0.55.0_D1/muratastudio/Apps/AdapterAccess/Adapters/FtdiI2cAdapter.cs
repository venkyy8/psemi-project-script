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
using FTD2XX_NET;
using Serilog;

namespace AdapterAccess.Adapters
{
	public class FtdiI2cAdapter : Adapter, IAdapter, II2c
	{
		public delegate void OnErrorHandler(string message);
		public event OnErrorHandler OnError;

		#region Private Members

		private FTDI _ftdi = null;
		private FTDI.FT_STATUS _status;
		private FTDI.MpsseConfiguration _config;
		private FTDI.FT_DEVICE_INFO_NODE _adapterInfo;
		private I2cCapabilities _i2cCapabilities;
		private List<string> _pluginCompatibility = new List<string>();
		private bool _isScanAllAddresses;
		private byte _defaultSlaveAddress;

		/// <summary>
		/// Used to synchronize the low level driver "scan" functionality.
		/// </summary>
		static object _getAllAdaptersLock = new object();

		#endregion

		#region Constructors

		internal FtdiI2cAdapter(FTDI.FT_DEVICE_INFO_NODE adapterInfo)
		{
			this._ftdi = new FTDI();
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
			// Opens the device with default settings
			_status = _ftdi.OpenBySerialNumber(_adapterInfo.SerialNumber);

			_adapterInfo.ftHandle = _ftdi.GetDeviceHandle();
			if (_status == FTDI.FT_STATUS.FT_DEVICE_NOT_OPENED)
			{
				_isOpened = false;
				_adapterInfo.Claimed = true;
				_adapterInfo.Free = false;
				return false;
			}

			if (_status == FTDI.FT_STATUS.FT_OK)
			{
				_isOpened = true;
				// Configure the opened device
				_status = _ftdi.InitializeMpsse(_config);

				if (_status != FTDI.FT_STATUS.FT_OK)
				{
					if (OnError != null)
					{
						OnError(_status.ToString());
					}
					return false;
				}

				System.Threading.Thread.Sleep(50);

				return true;
			}
			else
			{
				if (OnError != null)
				{
					OnError(_status.ToString());
				}
				return false;
			}
		}

		protected override void CloseDriverInterface()
		{
			if (_adapterInfo.ftHandle == IntPtr.Zero)
			{
				return;
			}

			_ftdi.Close();
			_adapterInfo.ftHandle = IntPtr.Zero;

			return;
		}

		public bool ReconfigureInterface(FTDI.MpsseConfiguration config)
		{
			_config = config;
			return ReconfigureInterface();
		}

		protected override bool ReconfigureInterface()
		{
			_status = _ftdi.InitializeMpsse(_config);

			if (_status != FTDI.FT_STATUS.FT_OK)
			{
				if (OnError != null)
				{
					OnError(_status.ToString());
				}
				return false;
			}
			return true;
		}

		#endregion

		#region I2c Interface

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
			return new I2cConfiguration
			{
				BitRate = _config.ClockSelection,
				BusLockTimeoutMs = 0,
				PullupsEnabled = false
			};
		}

		public bool I2cSetBitRate(int bitRate)
		{
			_config.ClockSelection = bitRate;
			return ReconfigureInterface();
		}

		public bool I2cSetDriveByZero(int drive)
		{
			switch(drive)
			{
				case 0:
					_config.DriveByZero = FTDI.DriveByZero.OFF;
					break;
				case 1:
					_config.DriveByZero = FTDI.DriveByZero.SCL;
					break;
				case 2:
					_config.DriveByZero = FTDI.DriveByZero.SDA;
					break;
				case 3:
					_config.DriveByZero = FTDI.DriveByZero.BOTH_SCL_SDA;
					break;
				default:
					_config.DriveByZero = FTDI.DriveByZero.BOTH_SCL_SDA;
					break;
			}
			
			return ReconfigureInterface();
		}

		public void I2cSetConfiguration(I2cConfiguration config)
		{
			_isScanAllAddresses = config.ScanAllAddresses;
			_defaultSlaveAddress = config.SlaveAddress;
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
			byte[] bdata = new byte[] { data };
			I2cWriteBlock(targetAddress, address, 1, ref bdata);
		}

		public void I2cWriteBlock(byte targetAddress, byte? address, int length, ref byte[] data)
		{
			if (CheckAdapterPresents())
			{
				uint bytesTransfered = 0;
				_status = _ftdi.I2C_DeviceWrite(targetAddress, address.Value, (uint)length, ref data, ref bytesTransfered);

				if (data.Length > 1)
				{
					Log.Debug("FtdiI2cAdapter I2cWriteBlock- TargetAddress : " + targetAddress.ToString() + " Register Address :" + address.Value.ToString() +
						" Data[0]: " + data[0].ToString() + " Data[1] : " + data[1].ToString() + "Status :"+ _status.ToString());
				}
				else
				{
					Log.Debug("FtdiI2cAdapter I2cWriteBlock  TargetAddress : " + targetAddress.ToString() + " Register Address :" + address.Value.ToString() +
						" Data[0]: " + data[0].ToString() + "Status :" + _status.ToString());
				}

				if (_status != FTDI.FT_STATUS.FT_OK)
				{
					if (OnError != null)
					{
						OnError(_status.ToString());
					}

					throw new FTDI.FT_EXCEPTION(_status.ToString());
				}
			}
		}


		public void I2cWrite16Bit(byte targetAddress, ushort? address, int length, ref byte[] data)
		{
			if (CheckAdapterPresents())
			{
				uint bytesTransfered = 0;
				_status = _ftdi.I2C_DeviceWrite(targetAddress, address.Value, (uint)length, ref data, ref bytesTransfered);

				if (data.Length > 1)
				{
					Log.Debug("FtdiI2cAdapter I2cWrite16Bit- TargetAddress : " + targetAddress.ToString() + " Register Address :" + address.Value.ToString() +
						" Data[0]: " + data[0].ToString() + " Data[1] : " + data[1].ToString() + " Status :" + _status.ToString());
				}
				else
				{
					Log.Debug("FtdiI2cAdapter I2cWrite16Bit  TargetAddress : " + targetAddress.ToString() + " Register Address :" + address.Value.ToString() +
						" Data[0]: " + data[0].ToString() + " Status :" + _status.ToString());
				}

				if (_status != FTDI.FT_STATUS.FT_OK)
				{
					if (OnError != null)
					{
						OnError(_status.ToString());
					}

					throw new FTDI.FT_EXCEPTION(_status.ToString());
				}
			}
		}

		public byte I2cReadByte(byte targetAddress, byte address)
		{
			byte[] data = new byte[1];
			I2cReadBlock(targetAddress, address, 1, ref data);
			return data[0];
		}

		public void I2cReadBlock(byte targetAddress, byte address, int length, ref byte[] data)
		{
			if (CheckAdapterPresents())
			{
				uint bytesTransfered = 0;
				_status = _ftdi.I2C_DeviceRead(targetAddress, address, (uint)length, ref data, ref bytesTransfered);

				if (data.Length > 1)
				{
					Log.Debug("FtdiI2cAdapter I2cReadBlock- TargetAddress : " + targetAddress.ToString() + " Register Address :" + address.ToString() +
						" Data[0]: " + data[0].ToString() + " Data[1] : " + data[1].ToString() + "Status :" + _status.ToString());
				}
				else
				{
					Log.Debug("FtdiI2cAdapter I2cReadBlock  TargetAddress : " + targetAddress.ToString() + " Register Address :" + address.ToString() +
						" Data[0]: " + data[0].ToString() + "Status :" + _status.ToString());
				}

				if (_status != FTDI.FT_STATUS.FT_OK)
				{
					if (OnError != null)
					{
						OnError(_status.ToString());
					}

					throw new FTDI.FT_EXCEPTION(_status.ToString());
				}
			}
		}

		public void I2cRead16Bit(byte targetAddress, ushort address, int length, ref byte[] data)
		{
			if (CheckAdapterPresents())
			{
				uint bytesTransfered = 0;
				_status = _ftdi.I2C_DeviceRead(targetAddress, address, (uint)length, ref data, ref bytesTransfered);

				if (data.Length > 1)
				{
					Log.Debug("FtdiI2cAdapter I2cRead16Bit- TargetAddress : " + targetAddress.ToString() + " Register Address :" + address.ToString() +
						" Data[0]: " + data[0].ToString() + " Data[1] : " + data[1].ToString() + " Status :" + _status.ToString());
				}
				else
				{
					Log.Debug("FtdiI2cAdapter I2cRead16Bit - TargetAddress : " + targetAddress.ToString() + " Register Address :" + address.ToString() +
						" Data[0]: " + data[0].ToString() + " Status :" + _status.ToString());
				}

				if (_status != FTDI.FT_STATUS.FT_OK)
				{
					if (OnError != null)
					{
						OnError(_status.ToString());
					}

					throw new FTDI.FT_EXCEPTION(_status.ToString());
				}
			}
		}

		public void I2cReadBlock(byte targetAddress, ushort address, int length, ref byte[] data)
		{
			if (CheckAdapterPresents())
			{
				uint bytesTransfered = 0;
				_status = _ftdi.I2C_DeviceRead(targetAddress, address, (uint)length, ref data, ref bytesTransfered);

				if (data.Length > 1)
				{
					Log.Debug("FtdiI2cAdapter I2cReadBlock 2- TargetAddress : " + targetAddress.ToString() + " Register Address :" + address.ToString() +
						" Data[0] : " + data[0].ToString() + " Data[1] : " + data[1].ToString() + ", Status :" + _status.ToString() );
				}
				else
				{
					Log.Debug("FtdiI2cAdapter I2cReadBlock 2- TargetAddress : " + targetAddress.ToString() + " Register Address :" + address.ToString() +
						" Data[0] : " + data[0].ToString() + ", Status :" + _status.ToString());
				}

				if (_status != FTDI.FT_STATUS.FT_OK)
				{
					if (OnError != null)
					{
						OnError(_status.ToString());
					}

					throw new FTDI.FT_EXCEPTION(_status.ToString());
				}
			}
		}

		public void I2cSendStop()
		{
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
			if (CheckAdapterPresents())
			{
				_status = _ftdi.SetGpio(value);
				if (_status != FTDI.FT_STATUS.FT_OK)
				{
					if (OnError != null)
					{
						OnError(_status.ToString());
					}

					throw new FTDI.FT_EXCEPTION(_status.ToString());
				}
			}
		}

		#endregion

		#region Enumeration

		internal static List<FTDI.FT_DEVICE_INFO_NODE> GetAllHardwareAdapters(FTDI wrapper)
		{
			var adapters = new List<FTDI.FT_DEVICE_INFO_NODE>();

			lock (_getAllAdaptersLock)
			{
				var deviceList = new FTDI.FT_DEVICE_INFO_NODE[255];
				var status = wrapper.GetDeviceList(deviceList);

				foreach (var item in deviceList)
				{
					if (item != null)
						adapters.Add(item);
				}
			}
			
			return adapters;
		}

		internal FTDI.FT_DEVICE_INFO_NODE GetAdapterInfo()
		{
			return this._adapterInfo;
		}

		internal bool Resync(FTDI.FT_DEVICE_INFO_NODE ftdiInfo)
		{
			return true; //TODO OpenDriverInterface();
		}

		#endregion

		#region Private Methods

		private bool CheckAdapterPresents()
		{
			if (_adapterInfo.Claimed)
			{
				return false;
			}

			if (_adapterInfo.ftHandle == IntPtr.Zero)
			{
				throw new AdapterException("No adapter configured");
			}

			return true;
		}

		private void ConfigureDefaultAdapterSettings()
		{
			_config = new FTDI.MpsseConfiguration
			{
				MasterCode = 0x09,								// 00001001
				ClockSelection = 0,								// 100KHz
				IsThreePhaseClocking = true,					// ON
				DriveByZero = FTDI.DriveByZero.SDA,				// Both SDA
			};
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
