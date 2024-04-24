using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace PE24106Control.RegisterControl
{
	public class LockableToggleButton : ToggleButton
	{
		protected override void OnToggle()
		{
			if (!LockToggle)
			{
				base.OnToggle();
			}
		}

		public bool LockToggle
		{
			get { return (bool)GetValue(LockToggleProperty); }
			set { SetValue(LockToggleProperty, value); }
		}

		public bool IsError
		{
			get { return (bool)GetValue(IsErrorProperty); }
			set { SetValue(IsErrorProperty, value); }
		}

		public static readonly DependencyProperty LockToggleProperty =
			DependencyProperty.Register("LockToggle", typeof(bool), typeof(LockableToggleButton), new UIPropertyMetadata(false));

		public static readonly DependencyProperty IsErrorProperty =
			DependencyProperty.Register("IsError", typeof(bool), typeof(LockableToggleButton), new PropertyMetadata(false));
	}
}
