using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;

namespace HardwareInterfaces
{
	/// <summary>
	/// Static class used to find file and directory information common to the entire application.
	/// </summary>
	public static class FileUtilities
	{
		public static DirectoryInfo DeviceInfoDirectory
		{
			get
			{
				return new DirectoryInfo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\Devices\\");
			}
		}

		public static string PluginsPath
		{
			get
			{
				return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\Plugins";
			}
		}

		public static string UpdaterPath
		{
			get
			{
				return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\ASUpdater.exe";
			}
		}

		public static string UpdateLog
		{
			get
			{
				return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\Update\Update.log";
			}
		}

		public static string ApplicationPath
		{
			get
			{
				return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			}
		}
	}
}
