using Mpq7920Control.ViewModel;
using PluginFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Mpq7920Control
{
	public class PluginManager : IPlugin
	{
		private Mpq7920Control _mpqControl;
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
			PluginViewModel pvm = (PluginViewModel)_mpqControl.DataContext;
			pvm.Cleanup();
		}

		public FrameworkElement GetElement(object device, object activeDevice, bool isInternalMode, bool is16BitI2CMode = false, bool isAllRegRead = false)
		{
			_mpqControl = new Mpq7920Control(device, isInternalMode);
			_mpqControl.Tag = this;
			return _mpqControl;
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
				ButtonImage = new BitmapImage(new Uri("/Mpq7920Control;component/Images/Application32.png", UriKind.Relative)),
				ButtonSmallImage = new BitmapImage(new Uri("/Mpq7920Control;component/Images/Application32.png", UriKind.Relative)),
				DockTabImage = new BitmapImage(new Uri("/Mpq7920Control;component/Images/Application32.png", UriKind.Relative)),
				ButtonToolTipText = "Application UI for device."
			};
		}
	}
}
