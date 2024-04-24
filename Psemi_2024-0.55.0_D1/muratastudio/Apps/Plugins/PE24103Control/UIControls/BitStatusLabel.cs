using HardwareInterfaces;
using PE24103Control.Helpers;
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

namespace PE24103Control.UIControls
{
    public class BitStatusLabel : Label, IMappedControl
	{
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

		public Brush OnSetBackgroundColor
		{
			get { return (Brush)GetValue(OnSetBackgroundColorProperty); }
			set { SetValue(OnSetBackgroundColorProperty, value); }
		}

		public Brush OnSetForegroundColor
		{
			get { return (Brush)GetValue(OnSetForegroundColorProperty); }
			set { SetValue(OnSetForegroundColorProperty, value); }
		}

		public Register.Bit BitSource
		{
			get { return (Register.Bit)GetValue(BitSourceProperty); }
			set { SetValue(BitSourceProperty, value); }
		}

		public Register RegisterSource
		{
			get { return (Register)GetValue(RegisterSourceProperty); }
			set { SetValue(RegisterSourceProperty, value); }
		}

		public static DependencyProperty BitSourceProperty =
			DependencyProperty.Register("BitSource", typeof(Register.Bit), typeof(BitStatusLabel), new UIPropertyMetadata());

		public static DependencyProperty RegisterSourceProperty =
			DependencyProperty.Register("RegisterSource", typeof(Register), typeof(BitStatusLabel), new UIPropertyMetadata());

		public static DependencyProperty IsSetProperty =
			DependencyProperty.Register("IsSet", typeof(bool), typeof(BitStatusLabel), new UIPropertyMetadata(false));

		public static readonly DependencyProperty IsErrorProperty =
			DependencyProperty.Register("IsError", typeof(bool), typeof(BitStatusLabel), new PropertyMetadata(false));

		public static readonly DependencyProperty OnSetBackgroundColorProperty =
			DependencyProperty.Register("OnSetBackgroundColorProperty", typeof(Brush), typeof(BitStatusLabel),
			new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0xc7, 0x21, 0x28))));

		public static readonly DependencyProperty OnSetForegroundColorProperty =
			DependencyProperty.Register("OnSetForegroundColorProperty", typeof(Brush), typeof(BitStatusLabel),
			new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0xff, 0xff, 0xff))));

        public MappedRegister Reg { get; set; }
    }
}
