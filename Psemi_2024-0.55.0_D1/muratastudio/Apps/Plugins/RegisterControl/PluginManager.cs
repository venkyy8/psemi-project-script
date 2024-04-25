using PluginFramework;
using RegisterControl.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace RegisterControl
{
	public class PluginManager : IPlugin
	{
		private RegisterControl _registerControl;
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
			PluginViewModel pvm = (PluginViewModel)_registerControl.DataContext;
			pvm.Cleanup();
		}

		public FrameworkElement GetElement(object device, object activeDevice, bool isInternalMode,bool is16BitI2CMode = false, bool isAllRegRead = false)
		{
			_registerControl = new RegisterControl(device, isInternalMode, is16BitI2CMode);
			_registerControl.Tag = this;
			return _registerControl;
		}

		public PluginManager()
		{
			_info = new PluginInfo
			{
				AssemblyName = Assembly.GetExecutingAssembly().GetName().ToString(),
				AssemblyVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString(),
				TabName = "Tools",
				PanelName = "Device",
				ButtonName = "Registers",
				ButtonImage = new BitmapImage(new Uri("/RegisterControl;component/Images/chip32.png", UriKind.Relative)),
				ButtonSmallImage = new BitmapImage(new Uri("/RegisterControl;component/Images/chip48.png", UriKind.Relative)),
				DockTabImage = new BitmapImage(new Uri("/RegisterControl;component/Images/chip16.png", UriKind.Relative)),
				ButtonToolTipText = "Used for configuring the devices memory."
			};
		}
	}
}
