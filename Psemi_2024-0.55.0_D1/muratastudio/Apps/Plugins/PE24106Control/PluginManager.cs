using PE24106Control.ViewModel;
using PluginFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace PE24106Control
{
	public class PluginManager : IPlugin
	{
		private PE24106Control _PE24106Control;
		private PluginInfo _info;

		public PluginInfo GetPluginInfo
		{
			get { return _info; }
		}

		public void Log()
		{
		}

		public void RefreshPlugin()
		{
		}

		public void ClosePlugin()
		{
			PluginViewModel pvm = (PluginViewModel)_PE24106Control.DataContext;
			pvm.Cleanup();
		}

		public FrameworkElement GetElement(object device, object activeDevice, bool isInternalMode, bool is16BitI2CMode = false, bool isAllRegRead = false)
		{
			_PE24106Control = new PE24106Control(device, isInternalMode);
			_PE24106Control.Tag = this;
			return _PE24106Control;
		}

		public PluginManager()
		{
			_info = new PluginInfo
			{
				AssemblyName = Assembly.GetExecutingAssembly().GetName().ToString(),
				AssemblyVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString(),
				TabName = "Tools",
				PanelName = "Device",
				ButtonName = "Device",
				ButtonImage = new BitmapImage(new Uri("/PE24106Control;component/Images/Application32.png", UriKind.Relative)),
				ButtonSmallImage = new BitmapImage(new Uri("/PE24106Control;component/Images/Application48.png", UriKind.Relative)),
				DockTabImage = new BitmapImage(new Uri("/PE24106Control;component/Images/Application16.png", UriKind.Relative)),
				ButtonToolTipText = "Application UI for device."
			};
		}
	}
}
