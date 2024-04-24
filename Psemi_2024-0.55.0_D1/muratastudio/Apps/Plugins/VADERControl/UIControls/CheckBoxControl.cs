using HardwareInterfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace VADERControl.UIControls
{
	class CheckBoxControl : CheckBox, INotifyPropertyChanged
	{
        #region Properties
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

		public Register RegisterSource1
		{
			get { return (Register)GetValue(RegisterSource1Property); }
			set { SetValue(RegisterSource1Property, value); }
		}

		public static DependencyProperty ValueProperty =
			DependencyProperty.Register("Value", typeof(int), typeof(CheckBoxControl), new UIPropertyMetadata(0));

		public static DependencyProperty RegisterSource1Property =
			DependencyProperty.Register("RegisterSource", typeof(Register), typeof(CheckBoxControl), new UIPropertyMetadata());

		public static readonly DependencyProperty IsErrorProperty =
			DependencyProperty.Register("IsError", typeof(bool), typeof(CheckBoxControl), new PropertyMetadata(false));

		public event PropertyChangedEventHandler PropertyChanged;
        #endregion
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
