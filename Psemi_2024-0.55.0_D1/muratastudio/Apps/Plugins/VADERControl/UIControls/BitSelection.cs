using HardwareInterfaces;
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

namespace VADERControl.UIControls
{
	public class BitSelection : ComboBox, INotifyPropertyChanged
	{
		public int Value
		{
			get { return (int)GetValue(ValueProperty); }
			set { SetValue(ValueProperty, value); }
		}

		public bool IsError
		{
			get { return (bool)GetValue(IsErrorProperty); }
			set { SetValue(IsErrorProperty, value); }
		}

		public Register RegisterSource
		{
			get { return (Register)GetValue(RegisterSourceProperty); }
			set { SetValue(RegisterSourceProperty, value); }
		}

		public static DependencyProperty ValueProperty =
			DependencyProperty.Register("Value", typeof(int), typeof(BitSelection), new UIPropertyMetadata(0));

		public static DependencyProperty RegisterSourceProperty =
			DependencyProperty.Register("RegisterSource", typeof(Register), typeof(BitSelection), new UIPropertyMetadata());
		
		public static readonly DependencyProperty IsErrorProperty =
			DependencyProperty.Register("IsError", typeof(bool), typeof(BitSelection), new PropertyMetadata(false));

		public event PropertyChangedEventHandler PropertyChanged;

		private void OnPropertyChanged(string info)
		{
			PropertyChangedEventHandler handler = PropertyChanged;
			if (handler != null)
			{
				handler(this, new PropertyChangedEventArgs(info));
			}
		}
	}
}
