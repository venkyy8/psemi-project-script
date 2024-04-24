using HardwareInterfaces;
using PE24103i2cControl.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace PE24103i2cControl.UIControls
{
	public class SendByteButton : Button, INotifyPropertyChanged, IMappedControl
	{
        public MappedRegister Reg { get; set; }

		public bool IsSet
		{
			get { return (bool)GetValue(IsSetProperty); }
			set { SetValue(IsSetProperty, value); }
		}

		public bool IsError
		{
			get { return (bool)GetValue(IsErrorProperty); }
			set { SetValue(IsErrorProperty, value); }
		}

		public string RegisterId
		{
			get { return (string)GetValue(RegisterIdProperty); }
			set { SetValue(RegisterIdProperty, value); }
		}

		public double Value
		{
			get { return (double)GetValue(ValueProperty); }
			set
			{ 
				SetValue(ValueProperty, value);
				IsSet = true; // ((int)value & BitSource.Mask) == BitSource.Mask ? true : false;
			}
		}

		public Image Source
		{
			get { return (Image)GetValue(SourceProperty); }
			set { SetValue(SourceProperty, value); }
		}

		public Register RegisterSource
		{
			get { return (Register)GetValue(RegisterSourceProperty); }
			set { SetValue(RegisterSourceProperty, value); }
		}

		// Using a DependencyProperty as the backing store for LockToggle.  This enables animation, styling, binding, etc...
		public static DependencyProperty RegisterIdProperty =
			DependencyProperty.Register("RegisterId", typeof(string), typeof(LedBitButton), new UIPropertyMetadata(string.Empty));

		public static DependencyProperty ValueProperty =
			DependencyProperty.Register("Value", typeof(double), typeof(LedBitButton), new UIPropertyMetadata(0.0));

		public static DependencyProperty SourceProperty =
			DependencyProperty.Register("Source", typeof(Image), typeof(LedBitButton), new UIPropertyMetadata());

		public static DependencyProperty RegisterSourceProperty =
			DependencyProperty.Register("RegisterSource", typeof(Register), typeof(LedBitButton), new UIPropertyMetadata());

		public static DependencyProperty IsSetProperty =
			DependencyProperty.Register("IsSet", typeof(bool), typeof(LedBitButton), new UIPropertyMetadata(false));

		public static readonly DependencyProperty IsErrorProperty =
			DependencyProperty.Register("IsError", typeof(bool), typeof(LedBitButton), new PropertyMetadata(false));

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
