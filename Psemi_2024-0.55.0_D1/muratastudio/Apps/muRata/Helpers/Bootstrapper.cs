using HardwareInterfaces;
using PluginFramework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows;

namespace muRata.Helpers
{
	public class Bootstrapper
	{
		public IList<IPlugin> Plugins
		{
			get;
			private set;
		}

		public void LoadPlugins()
		{
			Plugins = new List<IPlugin>();
			var folderPath = FileUtilities.PluginsPath;

			if (string.IsNullOrEmpty(folderPath))
			{
				throw new InvalidOperationException("Plugin folder path not found");
			}

			var folder = new DirectoryInfo(folderPath);

			if(!folder.Exists)
			{
				throw new InvalidOperationException(string.Format("Folder {0} does not exist", folderPath));
			}

			foreach (var file in folder.GetFiles("*.dll"))
			{
				var assembly = Assembly.LoadFile(file.FullName);

				foreach (var type in assembly.GetTypes())
				{
					if (typeof (IPlugin).IsAssignableFrom(type) && !type.IsInterface)
					{
						var constructor = type.GetConstructor(new Type[] { });

						if (constructor == null)
						{
							throw new InvalidOperationException("No default constructor for plugin manager found");
						}

						var manager = constructor.Invoke(new object[] { });
						
						Plugins.Add((IPlugin)manager);
					}
				}
			}
		}

		public FrameworkElement GetElement(IPlugin pluginManager, object device, object activeDevice, bool isInternalMode, bool is16BitI2CMode, bool isAllRegRead = false)
		{
			var fullType = pluginManager.GetType();

			var method = fullType.GetMethod("GetElement");

			var element = method.Invoke(pluginManager, new object[] { device , activeDevice, isInternalMode, is16BitI2CMode, isAllRegRead });

			return element as FrameworkElement;
		}
	}
}
