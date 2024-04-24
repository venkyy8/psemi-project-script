using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceAccess.Interfaces
{
	public interface IDevice
	{
		#region Adapter and Device Access

		/// <summary>
		/// Returns a String Containing the Adapter Name
		/// </summary>
		/// <returns></returns>
		string GetAdapterName();

		/// <summary>
		/// Returns a String Containing the Adapter Version Number
		/// </summary>
		/// <returns></returns>
		string GetAdapterVersion();

		/// <summary>
		/// Returns a String Containing the Adapter Serial Number
		/// </summary>
		/// <returns></returns>
		string GetAdapterSerialNumber();

		/// <summary>
		/// Returns a String Containing the Device Name
		/// </summary>
		/// <returns></returns>
		string GetDeviceName();

		/// <summary>
		/// Returns a String Containing the Device Version Number
		/// </summary>
		/// <returns></returns>
		string GetDeviceVersion();

		#endregion

		#region Low Level Communication Access

		byte ReadByte(uint address);

		ushort ReadWord(uint address);

		void ReadBlock(uint address, int length, ref byte[] data);

		void WriteByte(uint address, byte data);

		void WriteWord(uint address, ushort data);

		void WriteBlock(uint address, int length, byte[] data);

		void WriteControl(ushort command);

		#endregion
	}
}
