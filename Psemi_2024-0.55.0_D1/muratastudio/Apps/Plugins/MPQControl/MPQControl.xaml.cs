﻿using MpqControl.ViewModel;
using HardwareInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;

namespace MpqControl
{
	/// <summary>
	/// Interaction logic for DocumentViewerControl.xaml
	/// </summary>
	public partial class MpqControl : UserControl
	{
		#region Private Members

		private PluginViewModel pvm;

		#endregion

		#region Constructors

		public MpqControl()
		{
			InitializeComponent();
		}

		public MpqControl(object device, bool isInternalMode)
		{
			InitializeComponent();
			pvm = new PluginViewModel(device, isInternalMode);
			IDevice iDevice = device as IDevice;
			DataContext = pvm;
		}

		#endregion
	}
}
