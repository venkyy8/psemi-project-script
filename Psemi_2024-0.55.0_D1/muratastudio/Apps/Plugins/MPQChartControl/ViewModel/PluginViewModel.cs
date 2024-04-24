using DeviceAccess;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using HardwareInterfaces;
using PluginFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using LiveCharts.Configurations;
using System.Windows.Media;
using System.Threading;
using System.Windows.Data;
using System.ComponentModel;
using GalaSoft.MvvmLight.Command;
using System.Diagnostics;
using System.Windows.Controls;
using System.IO;
using Microsoft.Win32;

namespace MpqChartControl.ViewModel
{
	public class PluginViewModel : DependencyObject, ICleanup, INotifyPropertyChanged
	{
		#region Private Members

		private Device _device;
		private IDevice _iDevice;
		private IRegister _iRegister;
		private CustomizeBase _deviceBase;
		private bool _isInternalMode;
		private bool _showLegend = true;
		private int _logEntryCount = 0;

		// VIN Registers
		private Register _regVinOVFault;
		private Register _regVinOVWarn;
		private Register _regVin;
		private Register _regVinUVWarn;

		// VOUT Registers
		private Register _regVout;

		// Temperature
		private Register _regTempOTFault;
		private Register _regTempOTWarn;
		private Register _regTemp;

		// Current Registers
		private Register _regIoutOCFault;
		private Register _regIoutOCWarn;
		private Register _regIout;

		private StreamWriter _logFile;
		private SaveFileDialog sfd = new SaveFileDialog();
		private List<LogData> _logData;

		// Commands
		private RelayCommand _showLegendCommand;
		private RelayCommand _saveLoggedData;
		private RelayCommand _clearLoggedData;

		#endregion

		#region Properties

		public string DeviceName
		{
			get
			{
				return _iDevice.DeviceName;
			}
		}

		public string DisplayName
		{
			get
			{
				return _iDevice.DisplayName;
			}
		}

		public double AxisStep { get; set; }

		public double AxisUnit { get; set; }

		public Func<double, string> DateTimeFormatter { get; set; }

		public Register VinUVWarn
		{
			get { return _regVinUVWarn; }
		}

		public Register VinOVWarn
		{
			get { return _regVinOVWarn; }
		}

		public Register VinOVFault
		{
			get { return _regVinOVFault; }
		}

		public Register Vin
		{
			get { return _regVin; }
		}

		public Register Vout
		{
			get { return _regVout; }
		}

		public Register TempOTWarn
		{
			get { return _regTempOTWarn; }
		}

		public Register TempOTFault
		{
			get { return _regTempOTFault; }
		}

		public Register Temp
		{
			get { return _regTemp; }
		}

		public Register IoutOCWarn
		{
			get { return _regIoutOCWarn; }
		}

		public Register IoutOCFault
		{
			get { return _regIoutOCFault; }
		}

		public Register Iout
		{
			get { return _regIout; }
		}

		public bool ShowLegend
		{
			get { return _showLegend; }

			set
			{
				if (_showLegend == value)
				{
					return;
				}

				_showLegend = value;
				OnPropertyChanged("ShowLegend");
				OnPropertyChanged("ShowLegendInternal");
			}
		}

		public bool ShowLegendInternal
		{
			get { return _showLegend && _isInternalMode; }
		}

		public bool IsInternal
		{
			get { return _isInternalMode; }

			set
			{
				if (_isInternalMode == value)
				{
					return;
				}

				_isInternalMode = value;
				OnPropertyChanged("IsInternal");
				OnPropertyChanged("ShowLegendInternal");
			}
		}

		public bool IsExternal
		{
			get { return !_isInternalMode; }
		}

		public int LogEntryCount
		{
			get { return _logEntryCount; }

			set
			{
				if (_logEntryCount == value)
				{
					return;
				}

				_logEntryCount = value;
				OnPropertyChanged("LogEntryCount");
			}
		}

		#endregion

		#region Constructor

		public PluginViewModel(object device, bool isInternalMode)
		{
			_device = device as Device;
			_iDevice = device as IDevice;
			_iRegister = device as IRegister;
			IsInternal = isInternalMode;

			_logData = new List<PluginViewModel.LogData>();

			// Load registers
			LoadRegister();

			// Get the name of the device without any containing dashes
			string deviceName = _iDevice.DeviceInfoName.Replace("-", "");

			// Get a reference to an available type with the same name
			var customizer = Type.GetType("DeviceAccess." + deviceName + ",DeviceAccess");

			// If found, create an instance of the customizing class and perform its base actions
			if (customizer != null)
			{
				try
				{
					_deviceBase = Activator.CreateInstance(customizer, device) as CustomizeBase;
				}
				catch (Exception)
				{
					throw;
				}
			}

			var mapper = Mappers.Xy<MeasureModel>()
				.X(model => model.DateTime.Ticks)		// Use DateTime.Ticks as X
				.Y(model => model.Value);				// Use the value property as Y

			// Lets save the mapper globally
			Charting.For<MeasureModel>(mapper);

			// AxisStep forces the distance between each separator in the X axis
			AxisStep = TimeSpan.FromSeconds(1).Ticks;

			//AxisUnit forces lets the axis know that we are plotting seconds
			//this is not always necessary, but it can prevent wrong labeling
			AxisUnit = TimeSpan.TicksPerSecond;

			//lets set how to display the X Labels
			DateTimeFormatter = value => new DateTime((long)value).ToString("mm:ss");

			// Register for notification messages
			Messenger.Default.Register<NotificationMessage>(this, HandleNotification);
		}

		#endregion

		#region Public Methods

		public void AddLogData()
		{
			_logData.Add(new LogData
				{
					Date = DateTime.Now,
					Vin = Vin.LastReadValue,
					Vout = Vout.LastReadValue,
					Iout = Iout.LastReadValue,
					Temp = Temp.LastReadValue
				});
			LogEntryCount = _logData.Count;
		}

		public void RefreshPlugin()
		{
		}

		public void Cleanup()
		{
			_logData.Clear();
		}

		#endregion

		#region Commands

		public RelayCommand ShowLegendCommand
		{
			get
			{
				return _showLegendCommand
					?? (_showLegendCommand = new RelayCommand(ExecuteShowLegendCommand));
			}
		}

		public RelayCommand SaveDataCommand
		{
			get
			{
				return _saveLoggedData
					?? (_saveLoggedData = new RelayCommand(ExecuteSaveDataCommand));
			}
		}

		public RelayCommand ClearDataCommand
		{
			get
			{
				return _clearLoggedData
					?? (_clearLoggedData = new RelayCommand(ExecuteClearDataCommand));
			}
		}

		#endregion

		#region Private Methods

		private void ExecuteShowLegendCommand()
		{
			try
			{
				ShowLegend = !ShowLegend;
			}
			catch (Exception ex)
			{
				MessengerSend(ex);
			}
		}

		private void ExecuteSaveDataCommand()
		{
			try
			{
				sfd.Filter = "Log files (*.csv)|*.csv";
				if (sfd.ShowDialog() == true)
				{
					string path = sfd.FileName;
					StringBuilder sb = new StringBuilder();

					using (_logFile = new StreamWriter(path))
					{
						// Header
						sb.AppendLine("Timestamp,VIN,VOUT,IOUT,TEMP");
						foreach (var item in _logData)
						{
							sb.AppendFormat("{0},{1},{2},{3},{4}{5}", item.Date, item.Vin, item.Vout, item.Iout, item.Temp, Environment.NewLine);
						}

						_logFile.Write(sb.ToString());
					}
				}
			}
			catch (Exception ex)
			{
				MessengerSend(ex);
			}
		}

		private void ExecuteClearDataCommand()
		{
			var result = MessageBox.Show("Clear all log entries?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
			if (result == MessageBoxResult.Yes)
			{
				_logData.Clear();
				LogEntryCount = _logData.Count;
			}
		}

		private void LoadRegister()
		{
			// TODO Add to configuration file
			// VIN Registers
			_regVin = _iRegister.Registers.FirstOrDefault(r => r.Name == "READ_VIN");
			_regVin.MinY = 0;
			_regVin.MaxY = 20;
			_regVinOVFault = _iRegister.Registers.FirstOrDefault(r => r.Name == "VIN_OV_FAULT_LIMIT");
			_regVinOVWarn = _iRegister.Registers.FirstOrDefault(r => r.Name == "VIN_OV_WARN_LIMIT");
			_regVinUVWarn = _iRegister.Registers.FirstOrDefault(r => r.Name == "VIN_UV_WARN_LIMIT");

			// VOUT Registers
			_regVout = _iRegister.Registers.FirstOrDefault(r => r.Name == "READ_VOUT");
			_regVout.MinY = 0;
			_regVout.MaxY = 10;

			// Temperature Registers
			_regTemp = _iRegister.Registers.FirstOrDefault(r => r.Name == "READ_TEMPERATURE_1");
			_regTemp.MinY = 0;
			_regTemp.MaxY = 180;
			_regTempOTFault = _iRegister.Registers.FirstOrDefault(r => r.Name == "OT_FAULT_LIMIT");
			_regTempOTWarn = _iRegister.Registers.FirstOrDefault(r => r.Name == "OT_WARN_LIMIT");

			// IOUT Registers
			_regIout = _iRegister.Registers.FirstOrDefault(r => r.Name == "READ_IOUT");
			_regIout.MinY = 0;
			_regIout.MaxY = 50;
			_regIoutOCFault = _iRegister.Registers.FirstOrDefault(r => r.Name == "IOUT_OC_FAULT_LIMIT");
			_regIoutOCWarn = _iRegister.Registers.FirstOrDefault(r => r.Name == "IOUT_OC_WARN_LIMIT");
		}

		private void MessengerSend(Exception ex)
		{
			if (ex.InnerException != null)
				Debug.Print("Error: " + ex.InnerException.Message + " " + ex.Message);
			else
				Debug.Print("Error: " + ex.Message);
			Messenger.Default.Send(new CommunicationMessage(this, ex));
		}

		#endregion

		#region Event Handlers

		private void HandleNotification(NotificationMessage message)
		{
			if (message.Notification == Notifications.CleanupNotification)
			{
				Cleanup();
				Messenger.Default.Unregister(this);
			}
		}

		#endregion

		#region INotifyPropertyChanged implementation

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(string propertyName = null)
		{
			if (PropertyChanged != null)
				PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion

		class LogData
		{
			public DateTime Date { get; set; }

			public double Vin { get; set; }

			public double Vout { get; set; }

			public double Iout { get; set; }

			public double Temp { get; set; }
		}
	}
}
