using PE24106Control.ViewModel;
using PE24106Control.UIControls;
using HardwareInterfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Xml.Linq;

namespace PE24106Control
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class PE24106Control : UserControl
	{
		public PE24106Control(object device, bool isInternalMode)
		{
			InitializeComponent();
			DataContext = new PluginViewModel(device, isInternalMode);
		}
	}
}
