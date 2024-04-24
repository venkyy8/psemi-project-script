using HardwareInterfaces;
using PE24103Control.ViewModel;
using PluginFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace PE24103Control
{
	public class PluginManager : IPlugin
	{
		private PE24103Control _control;
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
			PluginViewModel pvm = (PluginViewModel)_control.DataContext;
			pvm.Cleanup();
		}

		public FrameworkElement GetElement(object device, object activeDevice, bool isInternalMode, bool is16BitI2CMode = false, bool isAllRegRead = false)
		{
			bool isMYT0424 = false;

			if (!is16BitI2CMode)
			{
				IDevice _device = device as IDevice;

				//Switching -  PE24103 and PE24104
				if(_device.DeviceInfoName.ToString() == "PE24103-R01")
                {
					_control = new PE24103Control(device, isInternalMode, true, isMYT0424);
				}
				else
                {
					//Switching to PE24104
					_control = new PE24103Control(device, isInternalMode, false);
				}
				_control.Tag = this;
			}
			return _control;
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
				ButtonImage = new BitmapImage(new Uri("/PE24103Control;component/Images/Application32.png", UriKind.Relative)),
				ButtonSmallImage = new BitmapImage(new Uri("/PE24103Control;component/Images/Application32.png", UriKind.Relative)),
				DockTabImage = new BitmapImage(new Uri("/PE24103Control;component/Images/Application32.png", UriKind.Relative)),
				ButtonToolTipText = "Application UI for device."
			};
		}
	}
}
