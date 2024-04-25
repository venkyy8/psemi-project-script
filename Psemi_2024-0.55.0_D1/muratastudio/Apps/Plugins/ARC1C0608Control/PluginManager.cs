using ARC1C0608Control.ViewModel;
using PluginFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ARC1C0608Control
{
	public class PluginManager : IPlugin
	{
		private ARC1C0608Control _ARC1C0608Control;
		private PluginInfo _info;
		PluginViewModel _pluginViewModel=null;
		public PluginInfo GetPluginInfo
		{
			get { return _info; }
		}

        public void Log()
        {
        }

		public void RefreshPlugin()
		{
			_pluginViewModel.CheckSumCalculation();
		}

		public void ClosePlugin()
		{
			PluginViewModel pvm = (PluginViewModel)_ARC1C0608Control.DataContext;
			pvm.Cleanup();
		}

		public FrameworkElement GetElement(object device, object activeDevice, bool isInternalMode, bool is16BitI2CMode = false, bool isAllRegRead = false)
		{
			_ARC1C0608Control = new ARC1C0608Control(device, isInternalMode);
			_pluginViewModel = (PluginViewModel)_ARC1C0608Control.DataContext;
			_ARC1C0608Control.Tag = this;
			return _ARC1C0608Control;
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
				ButtonImage = new BitmapImage(new Uri("/ARCxC0608Control;component/Images/Application32.png", UriKind.Relative)),
				ButtonSmallImage = new BitmapImage(new Uri("/ARCxC0608Control;component/Images/Application48.png", UriKind.Relative)),
				DockTabImage = new BitmapImage(new Uri("/ARCxC0608Control;component/Images/Application16.png", UriKind.Relative)),
				ButtonToolTipText = "Application UI for ARCxCxxxx device variants."
			};
		}
	}
}
