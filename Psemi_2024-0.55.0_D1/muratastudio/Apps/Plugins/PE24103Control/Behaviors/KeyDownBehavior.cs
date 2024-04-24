using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Controls.Primitives;
using System.Windows.Controls;
using System.Windows;
using System.Collections;

namespace PE24103Control.ViewModel
{
	/// <summary>
	/// This key down behavior passes the Tab property as a reference to a register.
	/// </summary>
	public class KeyDownBehavior
	{
		public static readonly DependencyProperty CommandProperty = DependencyProperty.RegisterAttached("Command", typeof(ICommand),
			typeof(KeyDownBehavior), new PropertyMetadata(PropertyChangedCallback));

		public static void PropertyChangedCallback(DependencyObject depObj, DependencyPropertyChangedEventArgs args)
		{
			TextBox tb = (TextBox)depObj;
			if (tb != null)
			{
				tb.KeyDown += tb_KeyDown;
			}
		}

		public static ICommand GetCommand(UIElement element)
		{
			return (ICommand)element.GetValue(CommandProperty);
		}

		public static void SetCommand(UIElement element, ICommand command)
		{
			element.SetValue(CommandProperty, command);
		}

		private static void tb_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				TextBox tb = (TextBox)sender;
				if (!string.IsNullOrEmpty(tb.Text))
				{
					ICommand command = tb.GetValue(CommandProperty) as ICommand;
					if (command != null)
					{
						ArrayList info = new ArrayList();
						info.Add(tb.Tag);
						info.Add(tb.Text);
						command.Execute(info);
					}
				}
				e.Handled = true;
				tb.SelectAll();
			}
		}
	}
}
