using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PE24106Control.ViewModel
{
	public static class LostFocusMessageBehavior
	{
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
		}
	}
}
