using HardwareInterfaces;
using PE24103i2cControl.ViewModel;
using PluginFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace PE24103i2cControl
{
	public class PluginManager : IPlugin
	{
		private PE24103i2cControl _PE24103i2cControl;
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
			PluginViewModel pvm = (PluginViewModel)_PE24103i2cControl?.DataContext;
			if (pvm != null)
			{
				pvm.IsAllRegistersRead = true;
				pvm.Refresh();
			}
		}

		public void ClosePlugin()
		{
			PluginViewModel pvm = (PluginViewModel)_PE24103i2cControl.DataContext;
			pvm.Cleanup();
		}

		public FrameworkElement GetElement(object device, object activeDevice, bool isInternalMode, bool is16BitI2CMode = false, bool isAllRegRead = false)
		{
			IDevice _device = device as IDevice;

			//Switching -  PE24103 and PE24104
			if (_device.DeviceInfoName.ToString() == "PE24103-R01")
			{
				_PE24103i2cControl = new PE24103i2cControl(device, isInternalMode, isAllRegRead, true);
			}
			else
			{
				//Switching to PE24104
				_PE24103i2cControl = new PE24103i2cControl(device, isInternalMode, isAllRegRead, false);
			}

			_PE24103i2cControl.Tag = this;
			return _PE24103i2cControl;
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
				ButtonImage = new BitmapImage(new Uri("/PE24103i2cControl;component/Images/Application32.png", UriKind.Relative)),
				ButtonSmallImage = new BitmapImage(new Uri("/PE24103i2cControl;component/Images/Application48.png", UriKind.Relative)),
				DockTabImage = new BitmapImage(new Uri("/PE24103i2cControl;component/Images/Application16.png", UriKind.Relative)),
				ButtonToolTipText = "Application UI for device."
			};
		}
	}
}
