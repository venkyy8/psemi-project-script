using System;
using System.Collections;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PE24103i2cControl.RegisterControl
{
	public static class LostFocusMessageBehavior
	{
		public static readonly DependencyProperty CommandProperty = DependencyProperty.RegisterAttached("Command", typeof(ICommand),
			typeof(LostFocusMessageBehavior));

		public static ICommand GetCommand(UIElement element)
		{
			return (ICommand)element.GetValue(CommandProperty);
		}

		public static void SetCommand(UIElement element, ICommand command)
		{
			element.SetValue(CommandProperty, command);
		}

		public static bool GetIsLostFocus(DependencyObject obj)
		{
			return (bool)obj.GetValue(IsLostFocusProperty);
		}

		public static void SetIsLostFocus(DependencyObject obj, bool value)
		{
			obj.SetValue(IsLostFocusProperty, value);
		}

		public static readonly DependencyProperty IsLostFocusProperty =
		  DependencyProperty.RegisterAttached("IsLostFocus",
												typeof(bool),
												typeof(LostFocusMessageBehavior),
												new PropertyMetadata(false, OnIsLostFocusChanged));

		private static void OnIsLostFocusChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			// ignoring error checking
			var textBox = (TextBox)sender;

			if ((bool)(e.NewValue))
				textBox.PreviewLostKeyboardFocus += LostFocus;
			else
				textBox.PreviewLostKeyboardFocus -= LostFocus;
		}

		private static void LostFocus(object sender, RoutedEventArgs e)
		{
			TextBox tb = sender as TextBox;
			if (tb.Text.Trim() == "")
			{
				MessageBox.Show("Value cannot be blank", "Invalid Input",
					MessageBoxButton.OK,
					MessageBoxImage.Error);
				e.Handled = true;
			}
			else
			{
				ICommand command = tb.GetValue(CommandProperty) as ICommand;
				if (command != null)
				{
					int value = (int)Convert.ToByte(tb.Text, 16);

					ArrayList info = new ArrayList();
					info.Add(tb.Tag);
					info.Add(value);
					command.Execute(info);
				}
			}
		}
	}
}
