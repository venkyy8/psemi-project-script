using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PE26100Control.ViewModel
{
	public static class HexDigitsOnlyBehavior
	{
		public static bool GetIsHexDigitOnly(DependencyObject obj)
		{
			return (bool)obj.GetValue(IsHexDigitOnlyProperty);
		}

		public static void SetIsHexDigitOnly(DependencyObject obj, bool value)
		{
			obj.SetValue(IsHexDigitOnlyProperty, value);
		}

		public static readonly DependencyProperty IsHexDigitOnlyProperty =
		  DependencyProperty.RegisterAttached("IsHexDigitOnly",
												typeof(bool),
												typeof(HexDigitsOnlyBehavior),
												new PropertyMetadata(false, OnIsHexDigitOnlyChanged));

		private static void OnIsHexDigitOnlyChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			// ignoring error checking
			var textBox = (TextBox)sender;
			var isHexDigitOnly = (bool)(e.NewValue);

			if (isHexDigitOnly)
				textBox.PreviewTextInput += BlockNonHexDigitCharacters;
			else
				textBox.PreviewTextInput -= BlockNonHexDigitCharacters;
		}

		private static void BlockNonHexDigitCharacters(object sender, TextCompositionEventArgs e)
		{
			e.Handled = e.Text.Any(ch => !IsHexDigit(ch));
		}

		private static bool IsHexDigit(char ch)
		{
			string hexCharacter = "0123456789ABCDEFabcdef";
			return hexCharacter.Contains(ch);
		}
	}
}
