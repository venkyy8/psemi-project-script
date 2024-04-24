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

namespace RegisterControl.Controls
{
	/// <summary>
	/// Interaction logic for Bit.xaml
	/// </summary>
	public partial class Bit : UserControl
	{
		public Bit()
		{
			InitializeComponent();
			DataContext = this;
		}

		public static DependencyProperty BitDescProperty = DependencyProperty.Register("BitDesc", typeof(string), typeof(Bit));
		public static DependencyProperty BitToolTipProperty = DependencyProperty.Register("BitToolTip", typeof(string), typeof(Bit));
		public static DependencyProperty BitNameProperty = DependencyProperty.Register("BitName", typeof(string), typeof(Bit));


		public string BitDesc
		{
			get { return (string)GetValue(BitDescProperty); }
			set { SetValue(BitDescProperty, value); }
		}

		public string BitToolTip
		{
			get { return (string)GetValue(BitToolTipProperty); }
			set { SetValue(BitToolTipProperty, value); }
		}

		public string BitName
		{
			get { return (string)GetValue(BitNameProperty); }
			set { SetValue(BitNameProperty, value); }
		}
	}
}
