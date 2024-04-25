using DocumentViewerControl.ViewModel;
using PluginFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace DocumentViewerControl
{
	public class PluginManager : IPlugin
	{
		private DocumentViewerControl _documentViewerControl;
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
			PluginViewModel pvm = (PluginViewModel)_documentViewerControl.DataContext;
			pvm.Cleanup();
		}

		public FrameworkElement GetElement(object device, object activeDevice, bool isInternalMode, bool is16BitI2CMode = false, bool isAllRegRead = false)
		{
			_documentViewerControl = new DocumentViewerControl(device, isInternalMode);
			_documentViewerControl.Tag = this;
			return _documentViewerControl;
		}

		public PluginManager()
		{
			_info = new PluginInfo 
			{
				AssemblyName = Assembly.GetExecutingAssembly().GetName().ToString(),
				AssemblyVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString(),
				TabName = "Help",
				PanelName = "Information",
				ButtonName = "Datasheet",
				ButtonImage = new BitmapImage(new Uri("/DocumentViewerControl;component/Images/Document32.png", UriKind.Relative)),
				ButtonSmallImage = new BitmapImage(new Uri("/DocumentViewerControl;component/Images/Document48.png", UriKind.Relative)),
				DockTabImage = new BitmapImage(new Uri("/DocumentViewerControl;component/Images/Document16.png", UriKind.Relative)),
				ButtonToolTipText = "Device information document."
			};
		}
	}
}
