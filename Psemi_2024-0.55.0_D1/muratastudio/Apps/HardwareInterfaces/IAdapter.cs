using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HardwareInterfaces
{
	public interface IAdapter
	{
		/// <summary>
		/// Returns a String Containing the Adapter Name
		/// </summary>
		/// <returns></returns>
		string AdapterName { get; }

		/// <summary>
		/// Returns a String Containing the Adapter Version Number
		/// </summary>
		/// <returns></returns>
		string AdapterVersion { get; }

		/// <summary>
		/// Returns a String Containing the Adapter Serial Number
		/// </summary>
		/// <returns></returns>
		string AdapterSerialNumber { get; }

		/// <summary>
		/// Returns a list of the compatible plugins
		/// </summary>
		List<string> PluginCompatibility { get; }
	}
}
