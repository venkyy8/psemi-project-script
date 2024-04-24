using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;


namespace AdapterControl
{
	public class ImageButton : Button
	{
		public ImageSource Source
		{
			get { return (ImageSource)GetValue(SourceProperty); }
			set { SetValue(SourceProperty, value); }
		}
			public static DependencyProperty SourceProperty =
				DependencyProperty.Register("Source", typeof(ImageSource), typeof(ImageButton), new UIPropertyMetadata());		
	}
}
