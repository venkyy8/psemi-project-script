using AdapterAccess.Adapters;
using HardwareInterfaces;
using Serilog;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AdapterAccess.Protocols
{
	public class PMBusProtocol : Protocol, INotifyPropertyChanged
	{
		#region Member Variables

		private IPMBus _pmbusInterface = null;
		private int _accessTimeoutMs = 3000;	// 3s
		private byte _targetAddress = 0x30;
		private int _bitRate = 0;				// 100KHz
		private int _driveByZero = 2;			// Both SCL and SDA
        private bool _isPec = false;

		#endregion

		#region Constructor

		public PMBusProtocol(byte targetAddress, int bitRate, Adapter adapter, AdapterMode accessMode)
			: base(adapter, "PMBus")
		{
			if (_adapter is IPMBus)
			{
				_targetAddress = targetAddress;
				_bitRate = bitRate;
				_adapter.AttachProtocol(this, accessMode);
				_pmbusInterface = (IPMBus)adapter;
				_pmbusInterface.I2cEnable();
			}
			else
			{
				Log.Error("The adapter does not support the PMBusProtocol.");
				throw new Exception("The adapter does not support the PMBus protocol.");
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
				if (_pmbusInterface.I2cSetBitRate(temp))
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
				if (_pmbusInterface.I2cSetDriveByZero(temp))
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
                if (_pmbusInterface.I2cSetPec(temp))
                {
                    _isPec = value;
                    OnPropertyChanged("IsPec");
                }
            }
        }

		public override Type GetInterfaceType()
		{
			return typeof(IPMBus);
		}

		public static bool IsAdapterSupported(Adapter adapter)
		{
			if (adapter is IPMBus)
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
				_pmbusInterface.I2cSendByte(_targetAddress, data);
				Log.Debug("PMBus Protocol SendByte : TargetAddress : " + -_targetAddress + " Data : " + data.ToString());

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
				_pmbusInterface.I2cWriteByte(_targetAddress, (byte)address, data);

				Log.Debug("PMBus Protocol WriteByte 1- TargetAddress : " + _targetAddress.ToString() + " Register Address :" +
					address.ToString() + " Data : " + data.ToString());
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
				_pmbusInterface.I2cWriteByte(targetAddress, address, data);

				Log.Debug("PMBus Protocol WriteByte 2- TargetAddress : " + targetAddress.ToString() + " Register Address :" + address.ToString() +
					" Data : " + data.ToString());


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
				_pmbusInterface.I2cWriteBlock(_targetAddress, (byte)address, length, ref data);

				if (data.Length > 1)
				{
					Log.Debug("PMBus Protocol WriteBlock 1- TargetAddress : " + _targetAddress.ToString() + " Register Address :" + address.ToString() +
						" deviceFileName : " + data[0].ToString() + " Data[1] : " + data[1].ToString() + " Length : " + length);
				}
				else if (data.Length == 1)
				{

					Log.Debug("PMBus Protocol WriteBlock 1- TargetAddress : " + _targetAddress.ToString() + " Register Address :" + address.ToString() +
						" deviceFileName : " + data[0].ToString() + " Length : " + length);
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
				_pmbusInterface.I2cWriteBlock(targetAddress, address, length, ref data);

				if (data.Length > 1)
				{
					Log.Debug("PMBus Protocol WriteBlock 2- TargetAddress : " + targetAddress.ToString() + " Register Address :" + address.ToString() +
						" deviceFileName : "+ data[0].ToString() + " Data[1] : " + data[1].ToString() + " Length : " + length);
				}
				else if (data.Length == 1)
				{
					Log.Debug("PMBus Protocol WriteBlock 2- TargetAddress : " + targetAddress.ToString() + " Register Address :" + address.ToString() +
						" deviceFileName : " + data[0].ToString() + " Length : " + length);
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
				data = _pmbusInterface.I2cReadByte(_targetAddress, (byte)address);
				Log.Debug("PMBus Protocol ReadByte 1- TargetAddress : " + _targetAddress.ToString() + "Register Address :" + address.ToString());
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
				data = _pmbusInterface.I2cReadByte(targetAddress, (byte)address);
				Log.Debug("PMBus Protocol ReadByte 2- TargetAddress : " + targetAddress.ToString() + 
					" Register Address :" + address.ToString() + " Data : "+ data.ToString());
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
				_pmbusInterface.I2cReadBlock(_targetAddress, (byte)address, length, ref data);

				if (data.Length > 1)
				{
					Log.Debug("PMBus Protocol ReadBlock 1- TargetAddress : " + _targetAddress.ToString() + " Register Address :" + address.ToString() +
						" Data[0]: " + data[0].ToString() + " Data[1] : " + data[1].ToString());
				}
				else if (data.Length == 1)
				{
					Log.Debug("PMBus Protocol ReadBlock 1- TargetAddress : " + _targetAddress.ToString() + " Register Address :" + address.ToString() + 
						" Data[0]: " + data[0].ToString());
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
				_pmbusInterface.I2cReadBlock(targetAddress, address, length, ref data);

				if (data.Length > 1)
				{
					Log.Debug("PMBus Protocol ReadBlock 2 - TargetAddress : " + _targetAddress.ToString() + " Register Address :" + address.ToString() +
						" Data[0]: " + data[0].ToString() + " Data[1] : " + data[1].ToString());
				}
				else if (data.Length == 1)
				{
					Log.Debug("PMBus Protocol ReadBlock 2 - TargetAddress : " + _targetAddress.ToString() + " Register Address :" + address.ToString() +
						" Data[0]: " + data[0].ToString());
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
				_pmbusInterface.SetGpio(value);
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
