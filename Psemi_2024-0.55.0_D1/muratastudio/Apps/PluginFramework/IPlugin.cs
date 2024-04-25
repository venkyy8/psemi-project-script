using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PluginFramework
{
	public interface IPlugin
	{
		PluginInfo GetPluginInfo
		{
			get;
		}

		void Log();

		void RefreshPlugin();

		void ClosePlugin();

		FrameworkElement GetElement(object device,object activeDevice, bool isInternalMode, bool is16BitI2CMode= false, bool isAllRegRead = false);
	}
}
