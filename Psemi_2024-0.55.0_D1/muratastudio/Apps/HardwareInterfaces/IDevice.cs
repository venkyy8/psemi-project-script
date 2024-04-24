using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace HardwareInterfaces
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
		string DeviceName { get; }

		/// <summary>
		/// Returns a String Containing the Device Name
		/// </summary>
		string DisplayName { get; }

		/// <summary>
		/// Returns a String Containing the Alt Device Name
		/// </summary>
		string AltDisplayName { get; }

		/// <summary>
		/// Returns a String Containing the Device Name
		/// </summary>
		string DeviceInfoName { get; }

		/// <summary>
		/// Returns a String Containing the Device Version Number
		/// </summary>
		string DeviceVersion { get; }

		/// <summary>
		/// Returns a list of the compatible plugins
		/// </summary>
		List<string> PluginCompatibility { get; }

		byte[] DataSheet { get; }

		byte[] HelpSheet { get; }

		XElement UiElements { get; }

		#endregion

		#region Low Level Communication Access

		byte ReadByte(uint address);

		ushort ReadWord(uint address);

		void ReadBlock(uint address, int length, ref byte[] data, bool isAdapterControl = false);

		void WriteByte(uint address, byte data);

		void WriteWord(uint address, ushort data);

		void WriteBlock(uint address, int length, ref byte[] data);

		void WriteControl(ushort command);

		#endregion
	}
}
