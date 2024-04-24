using System;
using System.Collections.Generic;
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

namespace PE24103i2cControl.RegisterControl
{
	/// <summary>
	/// Interaction logic for Registers.xaml
	/// </summary>
	public partial class Registers : UserControl
	{
		public Registers()
		{
			InitializeComponent();
		}

		public Registers(object device, bool isInternalMode)
		{
			InitializeComponent();
			DataContext = new ViewModel(device, isInternalMode);
			this.Loaded += Registers_Loaded;
		}

		void Registers_Loaded(object sender, RoutedEventArgs e)
		{
			ViewModel pvm = this.DataContext as ViewModel;
			pvm._loaded = true;
		}
		private void dataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
		}

		private void dataGrid_PreviewMouseDown(object sender, MouseButtonEventArgs e)
		{
		}
	}
}
