using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace HardwareInterfaces
{
	public interface IVirtual
	{
		VirtualCapabilities VirtualGetCapabilities();

		VirtualConfiguration VirtualGetConfiguration();

		bool VirtualSetBitRate(int bitRate);

		bool VirtualSetPec(bool pec);

		bool VirtualSetDriveByZero(int drive);

		void VirtualSetCapabilities(VirtualCapabilities config);

		void VirtualSetConfiguration(VirtualConfiguration config);

		void VirtualSendByte(byte targetAddress, byte data);

		void VirtualEnable();

		void VirtualDisable();

		void VirtualWriteByte(byte targetAddress, byte address, byte data, string deviceFileName = "");

		void VirtualWriteBlock8bit3String(byte targetAddress, ushort? address, int length, ref byte[] data, string deviceFileName = "");

		void VirtualWriteBlock(byte targetAddress, byte? address, int length, ref byte[] data, string deviceFileName = "");

		void VirtualWrite16Bit(byte targetAddress, ushort? address, int length, ref byte[] data,string deviceFileName = "");

		byte VirtualReadByte(byte targetAddress, byte address, string deviceFileName = "", bool isAdapterControl = false);

		void VirtualReadBlock(byte targetAddress, byte address, int length, ref byte[] data, string deviceFileName = "", bool isAdapterControl = false);

		void VirtualReadBlock(byte targetAddress, ushort address, int length, ref byte[] data, string deviceFileName = "", bool isAdapterControl = false);

		void VirtualRead16Bit(byte targetAddress, ushort address, int length, ref byte[] data, string deviceFileName = "");

		void VirtualSendStop();

		bool IsScanAllAddresses { get; }

		byte DefaultSlaveAddress { get; }

		void SetGpio(byte value);
	}

	public struct VirtualCapabilities
	{
		public string[] SupportedBitRatesLabels;
		public int[] SupportedBitRates;
		public int[] SupportedBusLockTimeouts;
		public bool SoftwareControlledPullups;
		public bool StopCommand;
		public bool SupportsDriveByZero;
	}

	public struct VirtualConfiguration
	{
		public byte SlaveAddress;
		public int BitRate;
		public bool ScanAllAddresses;
		public bool PullupsEnabled;
		public int BusLockTimeoutMs;
	}
}
