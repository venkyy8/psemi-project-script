using AdapterAccess.Adapters;
using HardwareInterfaces;
using Serilog;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AdapterAccess.Protocols
{
	public class I2cProtocol : Protocol, INotifyPropertyChanged
	{
		#region Member Variables

		private II2c _i2cInterface = null;
		private int _accessTimeoutMs = 3000;	// 3s
		private byte _targetAddress = 0x16;
		private int _bitRate = 0;				// 100KHz
		private int _driveByZero = 2;			// Both SCL and SDA

		#endregion

		#region Constructor

		public I2cProtocol(byte targetAddress, int bitRate, Adapter adapter, AdapterMode accessMode)
			: base(adapter, "I2C")
		{
			if (_adapter is II2c)
			{
				_targetAddress = targetAddress;
				_bitRate = bitRate;
				_adapter.AttachProtocol(this, accessMode);
				_i2cInterface = (II2c)adapter;
				_i2cInterface.I2cEnable();
			}
			else
			{
				Log.Error("The adapter does not support the I2C Protocol.");

				throw new Exception("The adapter does not support the I2C protocol.");
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
				if (_i2cInterface.I2cSetBitRate(temp))
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
				if (_i2cInterface.I2cSetDriveByZero(temp))
				{
					_driveByZero = value;
					OnPropertyChanged("DriveByZero");
				}
			}
		}

		public override Type GetInterfaceType()
		{
			return typeof(II2c);
		}

		public static bool IsAdapterSupported(Adapter adapter)
		{
			if (adapter is II2c)
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
				_i2cInterface.I2cSendByte(_targetAddress, data);
				Log.Debug("I2C Protocol SendByte : TargetAddress : " + -_targetAddress + " Data : " + data.ToString());

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
				_i2cInterface.I2cWriteByte(_targetAddress, (byte)address, data);
				Log.Debug("I2C Protocol WriteByte 1- TargetAddress : " + _targetAddress.ToString() + " Register Address :" + address.ToString() +
					" Data : " + data.ToString());

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
				_i2cInterface.I2cWriteByte(targetAddress, address, data);
				Log.Debug("I2C Protocol WriteByte 2- TargetAddress : " + targetAddress.ToString() 
					+ " Register Address :" + address.ToString() + " Data : " + data.ToString());

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

				if (_targetAddress == 56) 
				{
					_i2cInterface.I2cWrite16Bit(_targetAddress, (ushort)address, length, ref data);
				}
				else
				{
					_i2cInterface.I2cWriteBlock(_targetAddress, (byte)address, length, ref data);
				}

				Log.Debug("I2C Protocol WriteBlock 1 - TargetAddress : " + _targetAddress.ToString() + " Register Address :" + address.ToString() +
							" deviceFileName : " + " Data : " + data[0].ToString() + " Length :" + length);
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
				_i2cInterface.I2cWriteBlock(targetAddress, address, length, ref data);

				if (data.Length > 1)
				{
					Log.Debug("I2C Protocol WriteBlock 2- TargetAddress : " + targetAddress.ToString() + " Register Address :" + address.ToString() +
						" deviceFileName : " + data[0].ToString() + " Data[1] : " + data[1].ToString() + " Length : " + length);
				}
				else if (data.Length == 1)
				{
					Log.Debug("I2C Protocol WriteBlock 2- TargetAddress : " + targetAddress.ToString() + " Register Address :" + address.ToString() +
						" deviceFileName : " + data[0].ToString() + " Length : " + length);
				}
			}
			finally
			{
				ReleaseExclusiveAccess();
			}
		}

		public void WriteBlock(byte targetAddress, ushort address, int length, ref byte[] data)
		{
			try
			{
				GetExclusiveAccess(_accessTimeoutMs);
				_i2cInterface.I2cWrite16Bit(targetAddress, address, length, ref data);

				if (data.Length > 1)
				{
					Log.Debug("I2C Protocol WriteBlock 3 - TargetAddress : " + targetAddress.ToString() + " Register Address :" + address.ToString() +
						" deviceFileName : " + data[0].ToString() + " Data[1] : " + data[1].ToString() + " Length : " + length);
				}
				else if (data.Length == 1)
				{
					Log.Debug("I2C Protocol WriteBlock 3 - TargetAddress : " + targetAddress.ToString() + " Register Address :" + address.ToString() +
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
				data = _i2cInterface.I2cReadByte(_targetAddress, (byte)address);
				Log.Debug("I2C Protocol ReadByte 1- TargetAddress : " + _targetAddress.ToString() + " Register Address :" + address.ToString());
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
				data = _i2cInterface.I2cReadByte(targetAddress, (byte)address);
				Log.Debug("I2C Protocol ReadByte 2- TargetAddress : " + targetAddress.ToString() + " Register Address :" + address.ToString() +
						" Data : " + data.ToString());
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
				//targetAddress - 56 indicates 0x38, and targetaddress 38 indicates 0x30
				if (_targetAddress == 56) //length > 1
				{
					_i2cInterface.I2cRead16Bit(_targetAddress, (ushort)address, length, ref data);
				}
				else
                {
					_i2cInterface.I2cReadBlock(_targetAddress, (byte)address, length, ref data);
				}

				if (data.Length > 1)
				{
					Log.Debug("I2C Protocol ReadBlock 1- TargetAddress : " + _targetAddress.ToString() + " Register Address :" + address.ToString() +
						" Data[0]: " + data[0].ToString() + " Data[1] : " + data[1].ToString());
				}
				else if (data.Length == 1)
				{
					Log.Debug("I2C Protocol ReadBlock 1- TargetAddress : " + _targetAddress.ToString() + " Register Address :" + address.ToString() +
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
				_i2cInterface.I2cReadBlock(targetAddress, address, length, ref data);

				if (data.Length > 1)
				{
					Log.Debug("I2C Protocol ReadBlock 2- TargetAddress : " + _targetAddress.ToString() + " Register Address :" + address.ToString() +
						" Data[0]: " + data[0].ToString() + " Data[1] : " + data[1].ToString());
				}
				else if (data.Length == 1)
				{
					Log.Debug("I2C Protocol ReadBlock 2- TargetAddress : " + _targetAddress.ToString() + " Register Address :" + address.ToString() +
						" Data[0]: " + data[0].ToString());
				}
			}
			finally
			{
				ReleaseExclusiveAccess();
			}
		}

		public void ReadBlock(byte targetAddress, ushort address, int length, ref byte[] data)
		{
			try
			{
				GetExclusiveAccess(_accessTimeoutMs);
				_i2cInterface.I2cRead16Bit(targetAddress, address, length, ref data);

				if (data.Length > 1)
				{
					Log.Debug("I2C Protocol ReadBlock 32- TargetAddress : " + _targetAddress.ToString() + " Register Address :" + address.ToString() +
						" Data[0]: " + data[0].ToString() + " Data[1] : " + data[1].ToString());
				}
				else if (data.Length == 1)
				{
					Log.Debug("I2C Protocol ReadBlock 3- TargetAddress : " + _targetAddress.ToString() + " Register Address :" + address.ToString() +
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
				_i2cInterface.SetGpio(value);
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
