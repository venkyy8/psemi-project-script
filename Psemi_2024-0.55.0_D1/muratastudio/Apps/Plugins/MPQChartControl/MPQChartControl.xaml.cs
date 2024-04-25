using MpqChartControl.ViewModel;
using HardwareInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
using System.IO;
using LiveCharts.Wpf;

namespace MpqChartControl
{
	/// <summary>
	/// Interaction logic for DocumentViewerControl.xaml
	/// </summary>
	public partial class MpqChart : UserControl
	{
		#region Private Members

		private CartesianChart _vin;
		private CartesianChart _vout;
		private CartesianChart _iout;
		private CartesianChart _temp;

		private PluginViewModel pvm;

		#endregion

		public PluginViewModel Pvm
		{
			get { return pvm; }
		}

		#region Constructors

		public MpqChart()
		{
			InitializeComponent();
		}

		public MpqChart(object device, bool isInternalMode)
		{
			InitializeComponent();
			pvm = new PluginViewModel(device, isInternalMode);
			DataContext = pvm;
			_vin = VinChart;
			_vout = VoutChart;
			_iout = IoutChart;
			_temp = TempChart;
		}

		public void ReloadCharts()
		{
		}

		private void ucChart_Loaded(object sender, RoutedEventArgs e)
		{
			CartesianChart vin = FindVisualChildByName<CartesianChart>(VinDock, "VinChart");
			CartesianChart vout = FindVisualChildByName<CartesianChart>(VoutDock, "VoutChart");
			CartesianChart iout = FindVisualChildByName<CartesianChart>(IoutDock, "IoutChart");
			CartesianChart temp = FindVisualChildByName<CartesianChart>(TempDock, "TempChart");

			VinDock.Children.Remove(vin);
			VoutDock.Children.Remove(vout);
			IoutDock.Children.Remove(iout);
			TempDock.Children.Remove(temp);

			vin.DataContext = pvm;
			vout.DataContext = pvm;
			iout.DataContext = pvm;
			temp.DataContext = pvm;

			VinDock.Children.Add(vin);
			VoutDock.Children.Add(vout);
			IoutDock.Children.Add(iout);
			TempDock.Children.Add(temp);
		}

		public T FindVisualChildByName<T>(DependencyObject parent, string name) where T : DependencyObject
		{
			for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
			{
				var child = VisualTreeHelper.GetChild(parent, i);
				string controlName = child.GetValue(Control.NameProperty) as string;
				if (controlName == name)
				{
					return child as T;
				}
				else
				{
					T result = FindVisualChildByName<T>(child, name);
					if (result != null)
						return result;
				}
			}

			return null;
		}

		#endregion
	}
}
