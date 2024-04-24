using AdapterAccess.Adapters;
using HardwareInterfaces;
using Serilog;
using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace AdapterAccess.Protocols
{
	public class VirtualProtocol : Protocol, INotifyPropertyChanged
	{
		#region Member Variables

		private IVirtual _virtualInterface = null;
		private int _accessTimeoutMs = 3000;    // 3s
		private byte _targetAddress = 0x30;
		private int _bitRate = 0;               // 100KHz
		private int _driveByZero = 2;           // Both SCL and SDA
		private bool _isPec = false;
        public string deviceFileName;

		#endregion

		#region Constructor

		public VirtualProtocol(byte targetAddress, int bitRate, bool is16BitI2C, Adapter adapter, AdapterMode accessMode, string optionalDeviceName = "")
		   : base(adapter, "virtual")
		{
			if (_adapter is IVirtual)
			{
				_targetAddress = targetAddress;
				_bitRate = bitRate;
				_adapter.AttachProtocol(this, accessMode);
				_adapter.IsOpened = true;
				_adapter.Is16Bit = is16BitI2C;
				_virtualInterface = (IVirtual)adapter;
				_virtualInterface.VirtualEnable();
				this.deviceFileName = optionalDeviceName;
				Log.Information("VirtualProtocol identified");
			}
			else
			{
				Log.Error("The adapter does not support the Virtual protocol.");
				throw new Exception("The adapter does not support the Virtual protocol.");
			}
		}

        #endregion Constructor

        #region Properties and Information

        public byte TargetAddress
		{
			get { return _targetAddress; }
			set
			{
				if (_targetAddress != value)
				{
					_targetAddress = value;
					OnPropertyChanged("TargetAddress");
				}
			}
		}

		public int BitRate
		{
			get { return _bitRate; }
			set
			{
				int temp = value;
				if (_virtualInterface.VirtualSetBitRate(temp))
				{
					_bitRate = value;
					OnPropertyChanged("BitRate");
				}
			}
		}

		public int DriveByZero
		{
			get { return _driveByZero; }
			set
			{
				int temp = value;
				if (_virtualInterface.VirtualSetDriveByZero(temp))
				{
					_driveByZero = value;
					OnPropertyChanged("DriveByZero");
				}
			}
		}

		public bool IsPec
		{
			get { return _isPec; }
			set
			{
				bool temp = value;
				if (_virtualInterface.VirtualSetPec(temp))
				{
					_isPec = value;
					OnPropertyChanged("IsPec");
				}
			}
		}

        public override Type GetInterfaceType()
		{
			return typeof(IVirtual);
		}

		public static bool IsAdapterSupported(Adapter adapter)
		{
			if (adapter is IVirtual)
				return true;

			return false;
		}

		#endregion

		#region Access Members

		public override bool ResetAdapter()
		{
			try
			{
				GetExclusiveAccess(_accessTimeoutMs);
				return base.ResetAdapter();
			}
			finally
			{
				ReleaseExclusiveAccess();
			}
		}

		public override void SendByte(byte data)
		{
			try
			{
				GetExclusiveAccess(_accessTimeoutMs);
				_virtualInterface.VirtualSendByte(_targetAddress, data);
				Log.Debug("Virtual Protocol SendByte : TargetAddress : " + - _targetAddress + " Data : " + data.ToString());
			}
			finally
			{
				ReleaseExclusiveAccess();
			}
		}

		public override void WriteByte(uint address, byte data)
		{
			try
			{
				GetExclusiveAccess(_accessTimeoutMs);
				_virtualInterface.VirtualWriteByte(_targetAddress, (byte)address, data, deviceFileName);

				Log.Debug("Virtual Protocol WriteByte 1- TargetAddress : " + _targetAddress.ToString() + " Register Address :" + address.ToString() +
						" deviceFileName : " + deviceFileName + " Data : " + data.ToString());
			}
			finally
			{
				ReleaseExclusiveAccess();
			}
		}

		public void WriteByte(byte targetAddress, byte address, byte data)
		{
			try
			{
				GetExclusiveAccess(_accessTimeoutMs);
				_virtualInterface.VirtualWriteByte(targetAddress, address, data);

				Log.Debug("Virtual Protocol WriteByte 2- TargetAddress : " + targetAddress.ToString() + " Register Address :" + address.ToString() +
						" deviceFileName : " + deviceFileName + " Data : " + data.ToString());
			}
			finally
			{
				ReleaseExclusiveAccess();
			}
		}

		public override void WriteBlock(uint address, int length, ref byte[] data)
		{
			try
			{
				GetExclusiveAccess(_accessTimeoutMs);

				if (deviceFileName == "PE26100")
				{
					_virtualInterface.VirtualWriteBlock8bit3String(_targetAddress, (ushort)address, length, ref data, deviceFileName);
				}
				else
				{
					if (_adapter.Is16Bit)
					{
						_virtualInterface.VirtualWrite16Bit(_targetAddress, (ushort)address, length, ref data, deviceFileName);
					}
					else
					{
						_virtualInterface.VirtualWriteBlock(_targetAddress, (byte)address, length, ref data, deviceFileName);
					}
				}
				if (data.Length > 1)
				{
					Log.Debug("Virtual Protocol WriteBlock 1- TargetAddress : " + _targetAddress.ToString() + " Register Address :" + address.ToString() +
						" deviceFileName : " + deviceFileName + " Data[0]: " + data[0].ToString() + " Data[1] : " + data[1].ToString() + " Length : " + length);
				}
				else
				{
					Log.Debug("Virtual Protocol WriteBlock 1- TargetAddress : " + _targetAddress.ToString() + " Register Address :" + address.ToString() +
						" deviceFileName : " + deviceFileName + " Data[0]: " + data[0].ToString() + " Length : " + length);
				}
			}
			finally
			{
				ReleaseExclusiveAccess();
			}
		}

		public void WriteBlock(byte targetAddress, byte address, int length, ref byte[] data)
		{
			try
			{
				GetExclusiveAccess(_accessTimeoutMs);
				if (_adapter.Is16Bit)
				{
					_virtualInterface.VirtualWrite16Bit(targetAddress, address, length, ref data, deviceFileName);
				}
				else
				{

					_virtualInterface.VirtualWriteBlock(targetAddress, address, length, ref data, deviceFileName);
				}

				if (data.Length > 1)
				{
					Log.Debug("Virtual Protocol WriteBlock 2- TargetAddress : " + targetAddress + " Register Address :" + address.ToString() +
						"deviceFileName : " + deviceFileName + " Data[0]: " + data[0].ToString() + " Data[1] : " + data[1].ToString());
				}
				else
				{
					Log.Debug("Virtual Protocol WriteBlock 2- TargetAddress : " + targetAddress.ToString() + " Register Address :" + address.ToString() +
						" deviceFileName : " + deviceFileName + " Data[0]: " + data[0].ToString());
				}
			}
			finally
			{
				ReleaseExclusiveAccess();
			}
		}

		public override byte ReadByte(uint address)
		{
			byte data = 0;
			try
			{
				GetExclusiveAccess(_accessTimeoutMs);
				data = _virtualInterface.VirtualReadByte(_targetAddress, (byte)address, deviceFileName);
				Log.Debug("Virtual Protocol ReadByte 1- TargetAddress : " + _targetAddress.ToString() + " deviceFileName : " + 
					deviceFileName + " Register Address :" + address + " Data : " + data.ToString());

			}
			finally
			{
				ReleaseExclusiveAccess();
			}
			return data;
		}

		public byte ReadByte(byte targetAddress, byte address)
		{
			byte data = 0;
			try
			{
				GetExclusiveAccess(_accessTimeoutMs);
				data = _virtualInterface.VirtualReadByte(targetAddress, (byte)address);

				Log.Debug("Virtual Protocol ReadByte 2- TargetAddress : " + targetAddress.ToString() + " Register Address :" + address.ToString() + " Data : " +data.ToString());
			}
			finally
			{
				ReleaseExclusiveAccess();
			}
			return data;
		}

		public override void ReadBlock(uint address, int length, ref byte[] data, bool isAdapterControl = false)
		{
			try
			{
				GetExclusiveAccess(_accessTimeoutMs);
				if (deviceFileName.ToUpper() == "PE26100")
				{
					_virtualInterface.VirtualReadBlock(_targetAddress, (ushort)address, length, ref data, deviceFileName, isAdapterControl);
				}
				else
				{
					if (_adapter.Is16Bit)
					{
						_virtualInterface.VirtualRead16Bit(_targetAddress, (ushort)address, length, ref data, deviceFileName);
					}
					else
					{
						_virtualInterface.VirtualReadBlock(_targetAddress, (byte)address, length, ref data, deviceFileName, isAdapterControl);
					}
				}
				if (data.Length > 1)
				{
					Log.Debug("Virtual Protocol ReadBlock 1- TargetAddress : " + _targetAddress + " Register Address :" + address.ToString() + 
						" deviceFileName : " + deviceFileName  + "Data[0]: " + data[0].ToString() + " Data[1] : " + data [1].ToString());
				}
                else
                {
					Log.Debug("Virtual Protocol ReadBlock 1- TargetAddress : " + _targetAddress.ToString() + " Register Address :" + address.ToString() +
						" deviceFileName : " + deviceFileName + " Data[0]: " + data[0].ToString());
				}
			}
			finally
			{
				ReleaseExclusiveAccess();
			}
		}

		public void ReadBlock(byte targetAddress, byte address, int length, ref byte[] data)
		{
			try
			{
				GetExclusiveAccess(_accessTimeoutMs);
				if (_adapter.Is16Bit)
				{
					_virtualInterface.VirtualRead16Bit(targetAddress, address, length, ref data, deviceFileName);
				}
				else
				{
					_virtualInterface.VirtualReadBlock(targetAddress, address, length, ref data, deviceFileName);
				}

				if (data.Length > 1)
				{
					Log.Debug("Virtual Protocol ReadBlock 2- TargetAddress : " + _targetAddress + " Register Address :" + address +
						" deviceFileName : " + deviceFileName + " Data[0]: " + data[0] + " Data[1] : " + data[1]);
				}
				else
				{
					Log.Debug("Virtual Protocol ReadBlock 2- TargetAddress : " + _targetAddress + " Register Address :" + address +
						" deviceFileName : " + deviceFileName + " Data[0]: " + data[0]);
				}
			}
			finally
			{
				ReleaseExclusiveAccess();
			}
		}

		public void SetGpio(byte value)
		{
			try
			{
				GetExclusiveAccess(_accessTimeoutMs);
				_virtualInterface.SetGpio(value);
			}
			finally
			{
				ReleaseExclusiveAccess();
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
