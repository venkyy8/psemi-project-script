using AdapterControl.ViewModel;
using HardwareInterfaces;
using PluginFramework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DeviceAccess;

namespace AdapterControl
{
	/// <summary>
	/// Interaction logic for AdapterControl.xaml
	/// </summary>
	public partial class AdapterControl : UserControl
	{
		PluginViewModel pvm;
		private Device _device;

		public AdapterControl(object device, object activeDevice, bool isInternalMode, bool is16BitI2CMode = false)
		{
			InitializeComponent();
			pvm = new PluginViewModel(device, activeDevice, isInternalMode, is16BitI2CMode);
			_device = device as Device;
			DataContext = pvm;
			LogViewer.DataContext = pvm.LogEntries;

		}
	}
}
