using AdapterControl.ViewModel;
using PluginFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace AdapterControl
{
	public class PluginManager : IPlugin
	{
		private AdapterControl _adapterControl;
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
			PluginViewModel pvm = (PluginViewModel)_adapterControl.DataContext;
			pvm.Cleanup();
		}

		public FrameworkElement GetElement(object device, object activeDevice, bool isInternalMode, bool is16BitI2CMode = false, bool isAllRegRead = false)
		{
			_adapterControl = new AdapterControl(device, activeDevice,isInternalMode, is16BitI2CMode);
			_adapterControl.Tag = this;
			return _adapterControl;
		}

		public PluginManager()
		{
			_info = new PluginInfo 
			{
				AssemblyName = Assembly.GetExecutingAssembly().GetName().ToString(),
				AssemblyVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString(),
				TabName = "Tools",
				PanelName = "Adapter",
				ButtonName = "Protocols",
				ButtonImage = new BitmapImage(new Uri("/AdapterControl;component/Images/Adapter32.png", UriKind.Relative)),
				ButtonSmallImage = new BitmapImage(new Uri("/AdapterControl;component/Images/Adapter48.png", UriKind.Relative)),
				DockTabImage = new BitmapImage(new Uri("/AdapterControl;component/Images/Adapter16.png", UriKind.Relative)),
				ButtonToolTipText = "Used for sending and receiving low level data communications."
			};
		}
	}
}
