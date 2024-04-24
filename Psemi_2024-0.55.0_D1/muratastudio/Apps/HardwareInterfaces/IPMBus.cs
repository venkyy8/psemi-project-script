using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace HardwareInterfaces
{
	public interface IPMBus
	{
		PMBusCapabilities I2cGetCapabilities();

		PMBusConfiguration I2cGetConfiguration();

		bool I2cSetBitRate(int bitRate);

		bool I2cSetDriveByZero(int drive);

        bool I2cSetPec(bool pec);

		void I2cSetCapabilities(PMBusCapabilities config);

		void I2cSetConfiguration(PMBusConfiguration config);

		void I2cSendByte(byte targetAddress, byte data);

		void I2cEnable();

		void I2cDisable();

		void I2cWriteByte(byte targetAddress, byte address, byte data);

		void I2cWriteBlock(byte targetAddress, byte? address, int length, ref byte[] data);

		byte I2cReadByte(byte targetAddress, byte address);

		void I2cReadBlock(byte targetAddress, byte address, int length, ref byte[] data);

		void I2cSendStop();

		bool IsScanAllAddresses { get; }

		byte DefaultSlaveAddress { get; }

		void SetGpio(byte value);
	}

	public struct PMBusCapabilities
	{
		public string[] SupportedBitRatesLabels; 
		public int[] SupportedBitRates;
		public int[] SupportedBusLockTimeouts;
		public bool SoftwareControlledPullups;
		public bool StopCommand;
		public bool SupportsDriveByZero;
		public bool Pec;
	}

	public struct PMBusConfiguration
	{
		public byte SlaveAddress;
		public int BitRate;
		public bool ScanAllAddresses;
		public bool PullupsEnabled;
		public int BusLockTimeoutMs;
	}
}
