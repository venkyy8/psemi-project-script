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
	/// Interaction logic for BitRegister.xaml
	/// </summary>
	public partial class BitRegister : UserControl
	{
		public BitRegister()
		{
			InitializeComponent();
			DataContext = this;
		}

		#region Dependency Properties

		public static DependencyProperty BitRegisterNameProperty = DependencyProperty.Register("BitRegisterName", typeof(string), typeof(BitRegister), new PropertyMetadata("Register Name"));

		public static DependencyProperty Bit7NameProperty = DependencyProperty.Register("Bit7Name", typeof(string), typeof(BitRegister), new PropertyMetadata("Bit 7"));
		public static DependencyProperty Bit6NameProperty = DependencyProperty.Register("Bit6Name", typeof(string), typeof(BitRegister), new PropertyMetadata("Bit 6"));
		public static DependencyProperty Bit4NameProperty = DependencyProperty.Register("Bit4Name", typeof(string), typeof(BitRegister), new PropertyMetadata("Bit 4"));
		public static DependencyProperty Bit3NameProperty = DependencyProperty.Register("Bit3Name", typeof(string), typeof(BitRegister), new PropertyMetadata("Bit 3"));
		public static DependencyProperty Bit5NameProperty = DependencyProperty.Register("Bit5Name", typeof(string), typeof(BitRegister), new PropertyMetadata("Bit 5"));
		public static DependencyProperty Bit2NameProperty = DependencyProperty.Register("Bit2Name", typeof(string), typeof(BitRegister), new PropertyMetadata("Bit 2"));
		public static DependencyProperty Bit1NameProperty = DependencyProperty.Register("Bit1Name", typeof(string), typeof(BitRegister), new PropertyMetadata("Bit 1"));
		public static DependencyProperty Bit0NameProperty = DependencyProperty.Register("Bit0Name", typeof(string), typeof(BitRegister), new PropertyMetadata("Bit 0"));

		public static DependencyProperty Bit7DescProperty = DependencyProperty.Register("Bit7Desc", typeof(string), typeof(BitRegister));
		public static DependencyProperty Bit6DescProperty = DependencyProperty.Register("Bit6Desc", typeof(string), typeof(BitRegister));
		public static DependencyProperty Bit5DescProperty = DependencyProperty.Register("Bit5Desc", typeof(string), typeof(BitRegister));
		public static DependencyProperty Bit4DescProperty = DependencyProperty.Register("Bit4Desc", typeof(string), typeof(BitRegister));
		public static DependencyProperty Bit3DescProperty = DependencyProperty.Register("Bit3Desc", typeof(string), typeof(BitRegister));
		public static DependencyProperty Bit2DescProperty = DependencyProperty.Register("Bit2Desc", typeof(string), typeof(BitRegister));
		public static DependencyProperty Bit1DescProperty = DependencyProperty.Register("Bit1Desc", typeof(string), typeof(BitRegister));
		public static DependencyProperty Bit0DescProperty = DependencyProperty.Register("Bit0Desc", typeof(string), typeof(BitRegister));

		public static DependencyProperty Bit7ToolTipProperty = DependencyProperty.Register("Bit7ToolTip", typeof(string), typeof(BitRegister), new PropertyMetadata("See Datasheet"));
		public static DependencyProperty Bit6ToolTipProperty = DependencyProperty.Register("Bit6ToolTip", typeof(string), typeof(BitRegister), new PropertyMetadata("See Datasheet"));
		public static DependencyProperty Bit5ToolTipProperty = DependencyProperty.Register("Bit5ToolTip", typeof(string), typeof(BitRegister), new PropertyMetadata("See Datasheet"));
		public static DependencyProperty Bit4ToolTipProperty = DependencyProperty.Register("Bit4ToolTip", typeof(string), typeof(BitRegister), new PropertyMetadata("See Datasheet"));
		public static DependencyProperty Bit3ToolTipProperty = DependencyProperty.Register("Bit3ToolTip", typeof(string), typeof(BitRegister), new PropertyMetadata("See Datasheet"));
		public static DependencyProperty Bit2ToolTipProperty = DependencyProperty.Register("Bit2ToolTip", typeof(string), typeof(BitRegister), new PropertyMetadata("See Datasheet"));
		public static DependencyProperty Bit1ToolTipProperty = DependencyProperty.Register("Bit1ToolTip", typeof(string), typeof(BitRegister), new PropertyMetadata("See Datasheet"));
		public static DependencyProperty Bit0ToolTipProperty = DependencyProperty.Register("Bit0ToolTip", typeof(string), typeof(BitRegister), new PropertyMetadata("See Datasheet"));

		#endregion

		public string BitRegisterName
		{
			get { return (string)GetValue(BitRegisterNameProperty); }
			set { SetValue(BitRegisterNameProperty, value); }
		}


		#region Bit Description Properties

		public string Bit7Desc
		{
			get { return (string)GetValue(Bit7DescProperty); }
			set { SetValue(Bit7DescProperty, value); }
		}

		public string Bit6Desc
		{
			get { return (string)GetValue(Bit6DescProperty); }
			set { SetValue(Bit6DescProperty, value); }
		}

		public string Bit5Desc
		{
			get { return (string)GetValue(Bit5DescProperty); }
			set { SetValue(Bit5DescProperty, value); }
		}

		public string Bit4Desc
		{
			get { return (string)GetValue(Bit4DescProperty); }
			set { SetValue(Bit4DescProperty, value); }
		}

		public string Bit3Desc
		{
			get { return (string)GetValue(Bit3DescProperty); }
			set { SetValue(Bit3DescProperty, value); }
		}

		public string Bit2Desc
		{
			get { return (string)GetValue(Bit2DescProperty); }
			set { SetValue(Bit2DescProperty, value); }
		}

		public string Bit1Desc
		{
			get { return (string)GetValue(Bit1DescProperty); }
			set { SetValue(Bit1DescProperty, value); }
		}

		public string Bit0Desc
		{
			get { return (string)GetValue(Bit0DescProperty); }
			set { SetValue(Bit0DescProperty, value); }
		}

		#endregion

		#region Bit Name Properties

		public string Bit7Name
		{
			get { return (string)GetValue(Bit7NameProperty); }
			set { SetValue(Bit7NameProperty, value); }
		}

		public string Bit6Name
		{
			get { return (string)GetValue(Bit6NameProperty); }
			set { SetValue(Bit6NameProperty, value); }
		}

		public string Bit5Name
		{
			get { return (string)GetValue(Bit5NameProperty); }
			set { SetValue(Bit5NameProperty, value); }
		}

		public string Bit4Name
		{
			get { return (string)GetValue(Bit4NameProperty); }
			set { SetValue(Bit4NameProperty, value); }
		}

		public string Bit3Name
		{
			get { return (string)GetValue(Bit3NameProperty); }
			set { SetValue(Bit3NameProperty, value); }
		}

		public string Bit2Name
		{
			get { return (string)GetValue(Bit2NameProperty); }
			set { SetValue(Bit2NameProperty, value); }
		}

		public string Bit1Name
		{
			get { return (string)GetValue(Bit1NameProperty); }
			set { SetValue(Bit1NameProperty, value); }
		}

		public string Bit0Name
		{
			get { return (string)GetValue(Bit0NameProperty); }
			set { SetValue(Bit0NameProperty, value); }
		} 

		#endregion

		#region Bit ToolTip Properties

		public string Bit7ToolTip
		{
			get { return (string)GetValue(Bit7ToolTipProperty); }
			set { SetValue(Bit7ToolTipProperty, value); }
		}

		public string Bit6ToolTip
		{
			get { return (string)GetValue(Bit6ToolTipProperty); }
			set { SetValue(Bit6ToolTipProperty, value); }
		}

		public string Bit5ToolTip
		{
			get { return (string)GetValue(Bit5ToolTipProperty); }
			set { SetValue(Bit5ToolTipProperty, value); }
		}

		public string Bit4ToolTip
		{
			get { return (string)GetValue(Bit4ToolTipProperty); }
			set { SetValue(Bit4ToolTipProperty, value); }
		}

		public string Bit3ToolTip
		{
			get { return (string)GetValue(Bit3ToolTipProperty); }
			set { SetValue(Bit3ToolTipProperty, value); }
		}

		public string Bit2ToolTip
		{
			get { return (string)GetValue(Bit2ToolTipProperty); }
			set { SetValue(Bit2ToolTipProperty, value); }
		}

		public string Bit1ToolTip
		{
			get { return (string)GetValue(Bit1ToolTipProperty); }
			set { SetValue(Bit1ToolTipProperty, value); }
		}

		public string Bit0ToolTip
		{
			get { return (string)GetValue(Bit0ToolTipProperty); }
			set { SetValue(Bit0ToolTipProperty, value); }
		}

		#endregion
	}
}
