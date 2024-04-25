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

namespace ARCxCCxxControl.DataTransformationControl
{
	/// <summary>
	/// Interaction logic for RegisterTransform.xaml
	/// </summary>
	public partial class RegisterTransform : UserControl
	{
		public static readonly DependencyProperty FormattedTextProperty =
			   DependencyProperty.Register(
				  "FormattedText",
				  typeof(string),
				  typeof(RegisterTransform),
				  new FrameworkPropertyMetadata(null));

		public string FormattedText
		{
			get { return (string)GetValue(FormattedTextProperty); }
			set { SetValue(FormattedTextProperty, value); }
		}

		public static readonly DependencyProperty RegisterTextProperty =
			   DependencyProperty.Register(
				  "RegisterText",
				  typeof(string),
				  typeof(RegisterTransform),
				  new FrameworkPropertyMetadata(null));

		public string RegisterText
		{
			get { return (string)GetValue(RegisterTextProperty); }
			set { SetValue(RegisterTextProperty, value); }
		}

		public static readonly DependencyProperty MaskProperty =
			   DependencyProperty.Register(
				  "Mask",
				  typeof(int),
				  typeof(RegisterTransform),
				  new FrameworkPropertyMetadata(null));

		public int Mask
		{
			get { return (int)GetValue(MaskProperty); }
			set { SetValue(MaskProperty, value); }
		}

		public static readonly DependencyProperty TransformProperty =
			   DependencyProperty.Register(
				  "Transform",
				  typeof(string),
				  typeof(RegisterTransform),
				  new FrameworkPropertyMetadata(null));

		public string Transform
		{
			get { return (string)GetValue(TransformProperty); }
			set { SetValue(TransformProperty, value); }
		}

		public static readonly DependencyProperty DescriptionProperty =
			   DependencyProperty.Register(
				  "Description",
				  typeof(string),
				  typeof(RegisterTransform),
				  new FrameworkPropertyMetadata(null));

		public string Description
		{
			get { return (string)GetValue(DescriptionProperty); }
			set { SetValue(DescriptionProperty, value); }
		}

		public RegisterTransform()
		{
			InitializeComponent();
		}
	}
}
