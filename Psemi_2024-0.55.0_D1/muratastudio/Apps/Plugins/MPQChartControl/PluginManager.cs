using MpqChartControl.ViewModel;
using PluginFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace MpqChartControl
{
	public class PluginManager : IPlugin
	{
		private PluginViewModel pvm;
		private MpqChart _mpqChartControl;
		private PluginInfo _info;
		public PluginInfo GetPluginInfo
		{
			get { return _info; }
		}

		public void Log()
		{
			if (pvm != null)
			{
				pvm.AddLogData();
			}
		}

		public void RefreshPlugin()
		{
			pvm.RefreshPlugin();
			_mpqChartControl.ReloadCharts();
		}

		public void ClosePlugin()
		{
			PluginViewModel pvm = (PluginViewModel)_mpqChartControl.DataContext;
			pvm.Cleanup();
		}

		public FrameworkElement GetElement(object device, object activeDevice, bool isInternalMode, bool is16BitI2CMode = false, bool isAllRegRead = false)
		{
			_mpqChartControl = new MpqChart(device, isInternalMode);
			_mpqChartControl.Tag = this;
			this.pvm = _mpqChartControl.Pvm;
			return _mpqChartControl;
		}

		public PluginManager()
		{
			_info = new PluginInfo
			{
				AssemblyName = Assembly.GetExecutingAssembly().GetName().ToString(),
				AssemblyVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString(),
				TabName = "Tools",
				PanelName = "Monitor",
				ButtonName = "Charting",
				ButtonImage = new BitmapImage(new Uri("/MpqChartControl;component/Images/Chart32.png", UriKind.Relative)),
				ButtonSmallImage = new BitmapImage(new Uri("/MpqChartControl;component/Images/Chart32.png", UriKind.Relative)),
				DockTabImage = new BitmapImage(new Uri("/MpqChartControl;component/Images/Chart32.png", UriKind.Relative)),
				ButtonToolTipText = "System power monitor."
			};
		}
	}
}
