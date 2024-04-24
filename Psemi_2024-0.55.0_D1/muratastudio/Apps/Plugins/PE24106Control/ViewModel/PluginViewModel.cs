using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using HardwareInterfaces;
using PluginFramework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Runtime;
using System.ComponentModel;
using GalaSoft.MvvmLight.Command;
using System.Diagnostics;
using System.Timers;
using System.Xml.Linq;
using PE24106Control.UIControls;
using System.Windows.Data;
using System.Windows;
using PE24106Control.Converters;
using System.Windows.Media;
using System.IO;
using System.Xml.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using Microsoft.Win32;
using PE24106Control.Properties;
using PE24106Control.Dialogs;
using DeviceAccess;
using System.Threading;
using System.Collections;
using System.Xml.XPath;

namespace PE24106Control.ViewModel
{
	public class PluginViewModel : ViewModelBase
	{
		#region Private Members

		private RelayCommand<object> _writeRegisterCommand;
		private RelayCommand<Register> _writeCommand;
		private RelayCommand<Register> _readCommand;
		private RelayCommand _programCommand;
		private RelayCommand _unlockCommand;
		private IRegister _register;
		private IDevice _device;
		private ObservableCollection<Register> _registers1;
		private ObservableCollection<Register> _registers2;
		private bool _isInternalMode;
		private RelayCommand<Register.Bit> _toggleBitCommand;
		private RelayCommand _loadRegisterCommand;
		private RelayCommand _saveRegisterCommand;
		private bool _isEditing = false;
		private OpenFileDialog ofd = new OpenFileDialog();
		private SaveFileDialog sfd = new SaveFileDialog();
		private List<CheckBox> ledCheckBoxes = new List<CheckBox>();
		private CustomizeBase _deviceBase;

		private FrameworkElement _buck1RegistersControl;
		private FrameworkElement _buck2RegistersControl;
		private Visibility _buck1Visibility = Visibility.Collapsed;
		private Visibility _buck2Visibility = Visibility.Collapsed;
		private Visibility _buck2GroupBoxVisibility = Visibility.Collapsed;
		private bool _buck1Enabled = false;
		private bool _buck2Enabled = false;
		private string _buck1Status = "Not Available";
		private string _buck2Status = "Not Available";
		private string _buck1WriteData;
		private string _buck2WriteData;
		private decimal _buck1ExternalResValue;
		private decimal _buck2ExternalResValue;
		private bool _buck1ExternalResEnabled;
		private bool _buck2ExternalResEnabled;

		//private string buck1Header;
		//private string buck2Header;


		private readonly int _labelWidth = 120;
		#endregion

		#region Properties

		public string Buck1Header { get; set;  }

		public string Buck2Header { get; set; }

		public string Buck1StatusHeader { get; set; }

		public string Buck2StatusHeader { get; set; }

		public int SelectedConfig { get; set; }

		enum Configuration
		{
			Config1 = 1,
			Config2 = 2,
			Config3 = 3
		}

		private List<FrameworkElement> _namedElements { get; set; }

		public ObservableCollection<Register> Registers1
		{
			get { return _registers1; }
		}

		public ObservableCollection<Register> Registers2
		{
			get { return _registers2; }
		}

		public ObservableCollection<FrameworkElement> Command1Controls { get; set; }
		public ObservableCollection<FrameworkElement> Command2Controls { get; set; }
		public ObservableCollection<FrameworkElement> FaultsControls { get; set; }
		public ObservableCollection<FrameworkElement> Buck1StatusControls { get; set; }
		public ObservableCollection<FrameworkElement> Buck2StatusControls { get; set; }
		public ObservableCollection<FrameworkElement> AdcControls { get; set; }
		public ObservableCollection<FrameworkElement> AdcConfigControls { get; set; }
		public ObservableCollection<FrameworkElement> ConfigControls { get; set; }

		public FrameworkElement Buck1RegistersControl
		{
			get
			{
				return _buck1RegistersControl;
			}

			set
			{
				if (_buck1RegistersControl == value)
				{
					return;
				}

				var oldValue = _buck1RegistersControl;
				_buck1RegistersControl = value;
				RaisePropertyChanged("Buck1RegistersControl", oldValue, value, true);
			}
		}

		public FrameworkElement Buck2RegistersControl
		{
			get
			{
				return _buck2RegistersControl;
			}

			set
			{
				if (_buck2RegistersControl == value)
				{
					return;
				}

				var oldValue = _buck2RegistersControl;
				_buck2RegistersControl = value;
				RaisePropertyChanged("Buck2RegistersControl", oldValue, value, true);
			}
		}

		public Visibility Buck1Visibility
		{
			get
			{
				return _buck1Visibility;
			}

			set
			{
				if (_buck1Visibility == value)
				{
					return;
				}

				var oldValue = _buck1Visibility;
				_buck1Visibility = value;
				RaisePropertyChanged("Buck1Visibility", oldValue, value, true);
			}
		}

		public Visibility Buck2Visibility
		{
			get
			{
				return _buck2Visibility;
			}

			set
			{
				if (_buck2Visibility == value)
				{
					return;
				}

				var oldValue = _buck2Visibility;
				_buck2Visibility = value;
				RaisePropertyChanged("Buck2Visibility", oldValue, value, true);
			}
		}

		public bool Buck1Enabled
		{
			get
			{
				return _buck1Enabled;
			}

			set
			{
				if (_buck1Enabled == value)
				{
					return;
				}

				var oldValue = _buck1Enabled;
				_buck1Enabled = value;
				RaisePropertyChanged("Buck1Enabled", oldValue, value, true);
			}
		}

		public bool Buck2Enabled
		{
			get
			{
				return _buck2Enabled;
			}

			set
			{
				if (_buck2Enabled == value)
				{
					return;
				}

				var oldValue = _buck2Enabled;
				_buck2Enabled = value;
				RaisePropertyChanged("Buck2Enabled", oldValue, value, true);
			}
		}

		public Visibility Buck2GroupVisibility
		{
			get
			{
				return _buck2GroupBoxVisibility;
			}

			set
			{
				if (_buck2GroupBoxVisibility == value)
				{
					return;
				}

				var oldValue = _buck2GroupBoxVisibility;
				_buck2GroupBoxVisibility = value;
                RaisePropertyChanged("Buck2GroupVisibility", oldValue, value, true);
			}
		}

		public string Buck1Status
		{
			get
			{
				return _buck1Status;
			}

			set
			{
				if (_buck1Status == value)
				{
					return;
				}

				var oldValue = _buck1Status;
				_buck1Status = value;
				RaisePropertyChanged("Buck1Status", oldValue, value, true);
			}
		}

		public string Buck2Status
		{
			get
			{
				return _buck2Status;
			}

			set
			{
				if (_buck2Status == value)
				{
					return;
				}

				var oldValue = _buck2Status;
				_buck2Status = value;
				RaisePropertyChanged("Buck2Status", oldValue, value, true);
			}
		}

		public decimal Buck1ExternalResValue
		{
			get
			{
				return _buck1ExternalResValue;
			}

			set
			{
				if (value == 0)
				{
					return;
				}

				if (_buck1ExternalResValue == value)
				{
					return;
				}

				var oldValue = _buck1ExternalResValue;
				_buck1ExternalResValue = value;
				Properties.Settings.Default.Buck1ExternalResValue_mOhms = value;
				Properties.Settings.Default.Save();
				RebindControls();
				RaisePropertyChanged("Buck1ExternalResValue", oldValue, value, true);
			}
		}

		public decimal Buck2ExternalResValue
		{
			get
			{
				return _buck2ExternalResValue;
			}

			set
			{
				if (value == 0)
				{
					return;
				}

				if (_buck2ExternalResValue == value)
				{
					return;
				}

				var oldValue = _buck2ExternalResValue;
				_buck2ExternalResValue = value;
				Properties.Settings.Default.Buck2ExternalResValue_mOhms = value;
				Properties.Settings.Default.Save();
				RebindControls();
				RaisePropertyChanged("Buck2ExternalResValue", oldValue, value, true);
			}
		}

		public bool Buck1ExternalResEnabled
		{
			get
			{
				return _buck1ExternalResEnabled;
			}

			set
			{
				if (_buck1ExternalResEnabled == value)
				{
					return;
				}

				var oldValue = _buck1ExternalResEnabled;
				_buck1ExternalResEnabled = value;
				Properties.Settings.Default.Buck1ExternalResEnabled = value;
				Properties.Settings.Default.Save();
				RebindControls();
				RaisePropertyChanged("Buck1ExternalResEnabled", oldValue, value, true);
			}
		}

		public bool Buck2ExternalResEnabled
		{
			get
			{
				return _buck2ExternalResEnabled;
			}

			set
			{
				if (_buck2ExternalResEnabled == value)
				{
					return;
				}

				var oldValue = _buck2ExternalResEnabled;
				_buck2ExternalResEnabled = value;
				Properties.Settings.Default.Buck2ExternalResEnabled = value;
				Properties.Settings.Default.Save();
				RebindControls();
				RaisePropertyChanged("Buck2ExternalResEnabled", oldValue, value, true);
			}
		}

		public string DeviceName
		{
			get
			{
				return _device.DeviceName;
			}
		}

		public string DisplayName
		{
			get
			{
				return _device.DisplayName;
			}
		}

		public string Buck1WriteData
		{
			get
			{
				return _buck1WriteData;
			}

			set
			{
				if (_buck1WriteData == value)
				{
					return;
				}

				var oldValue = _buck1WriteData;
				_buck1WriteData = value;
				RaisePropertyChanged("Buck1WriteData", oldValue, value, true);
			}
		}

		public string Buck2WriteData
		{
			get
			{
				return _buck2WriteData;
			}

			set
			{
				if (_buck2WriteData == value)
				{
					return;
				}

				var oldValue = _buck2WriteData;
				_buck2WriteData = value;
				RaisePropertyChanged("Buck2WriteData", oldValue, value, true);
			}
		}

		public bool IsInternal
		{
			get
			{
				return _isInternalMode;
			}

			set
			{
				if (_isInternalMode == value)
				{
					return;
				}

				var oldValue = _isInternalMode;
				_isInternalMode = value;
				RaisePropertyChanged("IsInternal", oldValue, value, true);
			}
		}

		#endregion

		#region Constructors

		public PluginViewModel()
		{
		}

		public PluginViewModel(object device, bool isInternalMode)
		{
			ConfigurationSelector config = new ConfigurationSelector();
			config.ShowDialog();
			SelectedConfig = config.Configuration;

			_device = device as IDevice;
			_register = device as IRegister;
			_registers1 = new ObservableCollection<Register>();
			_registers2 = new ObservableCollection<Register>();
			IsInternal = isInternalMode;

			Command1Controls = new ObservableCollection<FrameworkElement>();
			Command2Controls = new ObservableCollection<FrameworkElement>();
			FaultsControls = new ObservableCollection<FrameworkElement>();
			Buck1StatusControls = new ObservableCollection<FrameworkElement>();
			Buck2StatusControls = new ObservableCollection<FrameworkElement>();
			AdcControls = new ObservableCollection<FrameworkElement>();
			AdcConfigControls = new ObservableCollection<FrameworkElement>();
			ConfigControls = new ObservableCollection<FrameworkElement>();
			_namedElements = new List<FrameworkElement>();

			Buck1RegistersControl = new FrameworkElement();
			Buck2RegistersControl = new FrameworkElement();

			Buck1Visibility = Visibility.Collapsed;
			Buck2Visibility = Visibility.Collapsed;
			Buck2GroupVisibility = Visibility.Collapsed;

			Buck1Enabled = false;
			Buck2Enabled = false;

			// Set the private members to avoid looping back save
			if (Properties.Settings.Default.Buck1ExternalResValue_mOhms == 0)
			{
				Properties.Settings.Default.Buck1ExternalResValue_mOhms = 10;
				Properties.Settings.Default.Save();
			}

			if (Properties.Settings.Default.Buck2ExternalResValue_mOhms == 0)
			{
				Properties.Settings.Default.Buck2ExternalResValue_mOhms = 10;
				Properties.Settings.Default.Save();
			}

			_buck1ExternalResEnabled = Properties.Settings.Default.Buck1ExternalResEnabled;
			_buck2ExternalResEnabled = Properties.Settings.Default.Buck2ExternalResEnabled;
			_buck1ExternalResValue = Properties.Settings.Default.Buck1ExternalResValue_mOhms;
			_buck2ExternalResValue = Properties.Settings.Default.Buck2ExternalResValue_mOhms;

			// Create UI
			LoadUiElements(_device.UiElements);

			// Load slave devices
			var dev = device as Device;
			foreach (SlaveDevice item in dev.SlaveDevices)
			{
				var reg = new RegisterControl.Registers(item, _isInternalMode);
				switch (item.DeviceName)
				{
					case "Buck1":
						Buck1Visibility = Visibility.Visible;
						Buck1Enabled = true;
						Buck1RegistersControl = reg;
						item.Registers.ForEach(r => _registers1.Add(r));
						Buck1Status = "Available";
						break;
					case "Buck2":
						if ((SelectedConfig == (int)Configuration.Config1) || (SelectedConfig == (int)Configuration.Config3))
						{
							Buck2Visibility = Visibility.Hidden;
							Buck2GroupVisibility = Visibility.Hidden;
							Buck2Enabled = true;
						}
						else
                        {
							Buck2Visibility = Visibility.Visible;
							Buck2GroupVisibility = Visibility.Visible;
							Buck2Enabled = true;
						}
						Buck2RegistersControl = reg;
						item.Registers.ForEach(r => _registers2.Add(r));
						Buck2Status = "Available";
						break;
				}
			}

			// Get the name of the device without any containing dashes
			string deviceName = _device.DeviceInfoName.Replace("-", "");

			// Get a reference to an available type with the same name.
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

			// Rebind controls
			RebindControls();

			// Register for notification messages
			Messenger.Default.Register<NotificationMessage>(this, HandleNotification);
		}

		#endregion

		#region Commands

		/// <summary>
		/// Gets the ToggleBitCommand.
		/// </summary>
		public RelayCommand<Register.Bit> ToggleBitCommand
		{
			get
			{
				return _toggleBitCommand
					?? (_toggleBitCommand = new RelayCommand<Register.Bit>(ExecuteToggleBitCommand));
			}
		}

		/// <summary>
		/// Gets the LoadRegisterCommand.
		/// </summary>
		public RelayCommand LoadRegisterCommand
		{
			get
			{
				return _loadRegisterCommand
					?? (_loadRegisterCommand = new RelayCommand(
					() =>
					{
						LoadRegisters();
					}));
			}
		}

		/// <summary>
		/// Gets the SaveRegisterCommand.
		/// </summary>
		public RelayCommand SaveRegisterCommand
		{
			get
			{
				return _saveRegisterCommand
					?? (_saveRegisterCommand = new RelayCommand(
					() =>
					{
						SaveRegisters();
					}));
			}
		}

		/// <summary>
		/// Gets the MyCommand.
		/// </summary>
		public RelayCommand<Register> WriteCommand
		{
			get
			{
				return _writeCommand
					?? (_writeCommand = new RelayCommand<Register>(ExecuteWriteCommand));
			}
		}

		/// <summary>
		/// Gets the MyCommand.
		/// </summary>
		public RelayCommand<Register> ReadCommand
		{
			get
			{
				return _readCommand
					?? (_readCommand = new RelayCommand<Register>(ExecuteReadCommand));
			}
		}

		public RelayCommand<object> WriteRegisterCommand
		{
			get
			{
				return _writeRegisterCommand
					?? (_writeRegisterCommand = new RelayCommand<object>(ExecuteWriteRegisterCommand));
			}
		}

		public RelayCommand ProgramCommand
		{
			get
			{
				return _programCommand
					?? (_programCommand = new RelayCommand(ExecuteProgramCommand));
			}
		}

		public RelayCommand UnlockCommand
		{
			get
			{
				return _unlockCommand
					?? (_unlockCommand = new RelayCommand(ExecuteUnlockCommand));
			}
		}

		#endregion

		#region Public Methods

		public override void Cleanup()
		{
			base.Cleanup();
		}

		#endregion

		#region Private Methods

		private void AddToNamedControls(FrameworkElement control)
		{
			if (!string.IsNullOrEmpty(control.Name))
			{
				_namedElements.Add(control);

				if (control is BitSelection)
				{
					var bs = control as BitSelection;
					bs.SelectionChanged += bs_SelectionChanged;
				}
			}
		}

		void bs_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ModifyControls();
		}

		private void ModifyControls()
		{
			foreach (var item in _namedElements)
			{
				if (item is BitSelection)
				{
					var bs = item as BitSelection;
					switch (bs.Name)
					{
						case "HiAcc":
							var destControl = _namedElements.Find(t => t.Name == "Voltage") as BitSelection;
							if (bs.SelectedIndex == 1) // Enabled
							{
								destControl.SelectedIndex = 1;
								destControl.IsEnabled = false;
							}
							else
							{
								destControl.IsEnabled = true;
							}
							break;
						default:
							break;
					}
				}
			}
		}

		private void RebindControls()
		{
			foreach (var item in _namedElements)
			{
				if (item is TextBlock)
				{
					// Default inductor formula
					string formula = "(((0.1/4095)*x/{0})*{1})/0.001";
					var tb = item as TextBlock;
					TextBlock source = null;
					decimal ohms1 = (decimal)_buck1ExternalResValue / 1000; // Convert to ohms
					decimal ohms2 = (decimal)_buck2ExternalResValue / 1000;
					int factor1 = _buck1ExternalResEnabled ? 1 : 2; // External Resistor = 1, Inductor DCR = 2
					int factor2 = _buck2ExternalResEnabled ? 1 : 2;

					switch (tb.Name)
					{
						case "Iout1_Out":
							source = _namedElements.Find(t => t.Name == "Iout1_Reg") as TextBlock;
							formula = string.Format(formula, ohms1, factor1);
							Binding val1 = new Binding
							{
								Source = source,
								Path = new PropertyPath("Text"),
								Converter = new TransformDisplayConverter(),
								ConverterParameter = string.Format("{0}|{1}|{2}", formula, "N0", "mA"),
								UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
							};
							tb.SetBinding(TextBlock.TextProperty, val1);
							break;
						case "Iout2_Out":
							source = _namedElements.Find(t => t.Name == "Iout2_Reg") as TextBlock;
							formula = string.Format(formula, ohms2, factor2);
							Binding val2 = new Binding
							{
								Source = source,
								Path = new PropertyPath("Text"),
								Converter = new TransformDisplayConverter(),
								ConverterParameter = string.Format("{0}|{1}|{2}", formula, "N0", "mA"),
								UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
							};
							tb.SetBinding(TextBlock.TextProperty, val2);
							break;
						default:
							break;
					}
				}
			}
		}

		private void ExecuteProgramCommand()
		{
			try
			{
				var result = MessageBox.Show("Program device now?", "MTP Memory Write", MessageBoxButton.YesNo, MessageBoxImage.Question);
				if (result == MessageBoxResult.Yes)
				{
					this._device.WriteByte(0x61, 0x61);
					this._device.WriteByte(0x60, 0x90);
					this._device.WriteByte(0x41, 0x37);
					this._device.WriteByte(0x62, 0x80);
					Messenger.Default.Send(
						new CommunicationMessage
						{
							MessageType = MessageType.Ok,
							Sender = this,
							TopLevel = new Exception("MTP Command sent successfully!")
						});
					MessageBox.Show("MTP Command sent successfully!", "MTP Memory Write", MessageBoxButton.OK, MessageBoxImage.None);
				}
			}
			catch (Exception ex)
			{
				MessengerSend(ex);
			}
		}

		private void ExecuteWriteRegisterCommand(object value)
		{
			ArrayList info = value as ArrayList;
			Register reg = info[0] as Register;
			string val = info[1].ToString();
			switch (reg.Device.DeviceName)
			{
				case "Buck1":
					_buck1WriteData = val;
					break;
				case "Buck2":
					_buck2WriteData = val;
					break;
				default:
					return;
			}
			ExecuteWriteCommand(reg);
		}

		private void ExecuteUnlockCommand()
		{
			try
			{
				// Elsa
				this._device.WriteByte(0x41, 0x37);

				// Olaf1
				this._device.WriteByte(0x57, 0x41);
				this._device.WriteByte(0x58, 0x37);
				this._device.WriteByte(0x56, 0x08);

				// Olaf2
				this._device.WriteByte(0x5C, 0x41);
				this._device.WriteByte(0x5D, 0x37);
				this._device.WriteByte(0x5B, 0x08);
			}
			catch (Exception ex)
			{
				MessengerSend(ex);
			}
		}


		private void ExecuteReadCommand(Register reg)
		{
			try
			{
				if (reg == null)
					return;

				Thread.Sleep(100); // Force a delay to prevent enter key from repeated button clicks
				IRegister register = reg.Device as IRegister;
				register.ReadRegisterValue(reg);
			}
			catch (Exception ex)
			{
				MessengerSend(ex);
			}
		}

		private void ExecuteWriteCommand(Register reg)
		{
			try
			{
				if (reg == null)
					return;

				IRegister register = reg.Device as IRegister;
				string writeData = string.Empty;
				switch (reg.Device.DeviceName)
				{
					case "Buck1":
						if (string.IsNullOrEmpty(Buck1WriteData))
						{
							return;
						}
						writeData = _buck1WriteData;
						break;
					case "Buck2":
						if (string.IsNullOrEmpty(Buck2WriteData))
						{
							return;
						}
						writeData = _buck2WriteData;
						break;
					default:
						return;
				}

				Thread.Sleep(100); // Force a delay to prevent enter key from repeated button clicks
				var val = Convert.ToDouble(Convert.ToByte(writeData, 16));

				if (reg.LastReadValue != (double)val)
				{
					reg.LastReadValue = val; // Small hack to change the binding so if the write fails the drop down selection will change back.
					register.WriteRegisterValue(reg, val);
					register.ReadRegisterValue(reg);

					if (_deviceBase != null)
					{
						_deviceBase.CheckDevice(null);
					}
				}
			}
			catch (Exception ex)
			{
				MessengerSend(ex);
			}
		}

		private void ExecuteToggleBitCommand(Register.Bit bit)
		{
			Register reg = _register.GetRegister(bit.RegisterID);
			int x = (((int)reg.LastReadValue & (int)bit.Mask) == (int)bit.Mask) ? 0 : 1;

			try
			{
				_register.WriteRegisterBit(bit, (uint)x);
				_register.ReadRegisterValue(reg);

				// Check the device for errors
				if (_deviceBase != null)
				{
					_deviceBase.CheckDevice(null);
				}
			}
			catch (Exception ex)
			{
				MessengerSend(ex);
			}
		}

		private void LoadUiElements(XElement elements)
		{
			// Get reference to main XML Panel node
			IEnumerable<XElement> panels = null;

			if (SelectedConfig == (int)Configuration.Config1)
			{
				panels = (elements.XPathSelectElement("Config1Panels")).Descendants("Panel");
				Buck1Header = panels.Where(p => p.Attribute("Name").Value == "Buck1 Control").Select(p => p.Attribute("Header")).First().Value;
				Buck1StatusHeader = panels.Where(p => p.Attribute("Name").Value == "Slave1 Status").Select(p => p.Attribute("Header")).First().Value;
				Buck2Visibility = Visibility.Collapsed;
				Buck2GroupVisibility = Visibility.Collapsed;
				Buck2Enabled = false;
			}
			else if (SelectedConfig == (int)Configuration.Config2)
			{
				panels = (elements.XPathSelectElement("Config2Panels")).Descendants("Panel");
				Buck1Header = panels.Where(p => p.Attribute("Name").Value == "Buck1 Control").Select(p => p.Attribute("Header")).First().Value;
				Buck2Header = panels.Where(p => p.Attribute("Name").Value == "Buck2 Control").Select(p => p.Attribute("Header")).First().Value;
				Buck1StatusHeader = panels.Where(p => p.Attribute("Name").Value == "Slave1 Status").Select(p => p.Attribute("Header")).First().Value;
				Buck2StatusHeader = panels.Where(p => p.Attribute("Name").Value == "Slave2 Status").Select(p => p.Attribute("Header")).First().Value;
			}
			else if (SelectedConfig == (int)Configuration.Config3)
			{
				panels = (elements.XPathSelectElement("Config3Panels")).Descendants("Panel");
				Buck1Header = panels.Where(p => p.Attribute("Name").Value == "Buck1 Control").Select(p => p.Attribute("Header")).First().Value;
				Buck1StatusHeader = panels.Where(p => p.Attribute("Name").Value == "Slave1 Status").Select(p => p.Attribute("Header")).First().Value;
				Buck2Visibility = Visibility.Collapsed;
				Buck2GroupVisibility = Visibility.Collapsed;
				Buck2Enabled = false;
			}
			else
			{
				panels = elements.Descendants("Panel");
				Buck1Header = panels.Where(p => p.Attribute("Name").Value == "Buck1 Control").Select(p => p.Attribute("Header")).First().Value;
				Buck2Header = panels.Where(p => p.Attribute("Name").Value == "Buck2 Control").Select(p => p.Attribute("Header")).First().Value;
			}

			// Get individual panel references
			var command1Panel = panels.Where(p => p.Attribute("Name").Value == "Buck1 Control");
			var command2Panel = panels.Where(p => p.Attribute("Name").Value == "Buck2 Control");
			var faultsPanel = panels.Where(p => p.Attribute("Name").Value == "Faults");
			var adcPanel = panels.Where(p => p.Attribute("Name").Value == "ADC Output");
			var adcConfigPanel = panels.Where(p => p.Attribute("Name").Value == "ADC Configuration");
			var configPanel = panels.Where(p => p.Attribute("Name").Value == "Configuration");
			var buck1StatusPanel = panels.Where(p => p.Attribute("Name").Value == "Slave1 Status");
			var buck2StatusPanel = panels.Where(p => p.Attribute("Name").Value == "Slave2 Status");

			// Create the Control 1 Panel
			ParseElements(command1Panel, Command1Controls);

			// Create the Control 2 Panel
			ParseElements(command2Panel, Command2Controls);

			// Create the Faults Panel
			ParseElements(faultsPanel, FaultsControls);

			// Create the Buck1 Status Panel
			ParseElements(buck1StatusPanel, Buck1StatusControls);

			// Create the Buck2 Status Panel
			ParseElements(buck2StatusPanel, Buck2StatusControls);

			// Create the ADC Output Panel
			ParseElements(adcPanel, AdcControls);

			// Create the ADC Configuration Panel
			ParseElements(adcConfigPanel, AdcConfigControls);

			// Create the Config Panel
			ParseElements(configPanel, ConfigControls);
		}

		private void ParseElements(IEnumerable<XElement> elements, ObservableCollection<FrameworkElement> control)
		{
			if (elements.Count() == 0)
				return;

			// Get the root node for its attributes
			var root = (XElement)elements.ElementAt(0);

			string panelText = root.Attribute("Name").Value;
			Orientation orientation = (root.Attribute("Orientation").Value == "Vertical")
				? Orientation.Vertical : Orientation.Horizontal;

			foreach (var item in elements.Elements())
			{
				bool isPrivate = item.Attribute("Private") == null ? false : bool.Parse(item.Attribute("Private").Value);
				if (isPrivate && !_isInternalMode)
					continue;

				var objects = new List<Object>();
				string label = string.Empty;
				string map = string.Empty;
				string description = string.Empty;
				string toolTipShort = string.Empty;
				string toolTipLong = string.Empty;
				int mask = 0;
				string name = string.Empty;

				name = (item.Attribute("Name") != null)
					? item.Attribute("Name").Value : string.Empty;

				description = (item.Attribute("Description") != null)
					? item.Attribute("Description").Value : string.Empty;

				label = (item.Attribute("Label") != null)
					? item.Attribute("Label").Value : string.Empty;

				map = (item.Attribute("Map") != null)
					? item.Attribute("Map").Value : string.Empty;

				toolTipShort = string.Format("{0}{1}{2}",
							map.Split('|')[0],
							Environment.NewLine,
							description);

				toolTipLong = string.Format("{0}{1}{2}{3}{4}",
							map.Split('|')[0],
							Environment.NewLine,
							label,
							Environment.NewLine,
							description);

				switch (item.Name.LocalName)
				{
					case "Label":
						var content = item.Attribute("Content").Value;
						var alignment = (HorizontalAlignment)Enum.Parse(typeof(HorizontalAlignment), item.Attribute("Align").Value);
						var labelControl = new Label
						{
							Name = name,
							Content = content,
							HorizontalAlignment = alignment,
							FontWeight = FontWeights.Bold
						};
						AddToNamedControls(labelControl);
						control.Add(labelControl);
						break;
					case "List":
						{
							// Create Stack Panel
							var listStackPanel = new StackPanel();
							listStackPanel.Orientation = Orientation.Horizontal;

							// Create Label and ComboBox
							var listLabel = new Label();
							var listComboBox = new BitSelection();

							// Determine if this list control has to span across multiple registers
							bool multiRegister = item.Attribute("Mask").Value.Contains("|");

							if (multiRegister)
							{
								string[] maps = item.Attribute("Map").Value.Split('|');
								string[] masks = item.Attribute("Mask").Value.Split('|');


								label = item.Attribute("Label").Value;
								var sMap = item.Attribute("Map").Value;
								var sMask = item.Attribute("Mask").Value;
								description = item.Attribute("Description").Value;

								var listObject = new ListObject
								{
									Label = label,
									Map = sMap,
									Mask = sMask,
									Description = description
								};

								foreach (var option in item.Elements())
								{
									string optionLabel = option.Attribute("Label").Value;
									string optionValue = option.Attribute("Value").Value;
									objects.Add(new Option { Label = optionLabel, Value = optionValue });
								}

								listLabel.Width = _labelWidth;
								listLabel.Content = label;
								listStackPanel.Children.Add(listLabel);

								listComboBox.Name = name;
								listComboBox.HorizontalAlignment = HorizontalAlignment.Right;
								listComboBox.VerticalAlignment = VerticalAlignment.Center;
								listComboBox.ItemsSource = objects;
								listComboBox.DisplayMemberPath = "Label";
								listComboBox.SelectedValuePath = "Value";
								listComboBox.Tag = listObject;

								listComboBox.RegisterSource = _register.GetRegister(maps[0].Replace("_", ""));

								Register regLsb = _register.GetRegister(maps[0].Replace("_", ""));
								Register regMsb = _register.GetRegister(maps[1].Replace("_", ""));

								string toolTip = string.Format("{0}{1}{2}{3}{4}",
								regLsb.DisplayName,
								Environment.NewLine,
								regMsb.DisplayName,
								Environment.NewLine,
								description);
								listComboBox.ToolTip = toolTip;

								var multi = new MultiBinding();
								multi.Converter = new MultiRegisterValueConverter();
								multi.ConverterParameter = masks;
								multi.UpdateSourceTrigger = UpdateSourceTrigger.Explicit;
								multi.Bindings.Add(new Binding()
								{
									Source = regLsb,
									Path = new PropertyPath("LastReadValue"),
								});

								multi.Bindings.Add(new Binding()
								{
									Source = regMsb,
									Path = new PropertyPath("LastReadValue"),
								});

								listComboBox.SetBinding(BitSelection.SelectedValueProperty, multi);

								var val = new Binding();
								val.Source = listComboBox.RegisterSource;
								val.Path = new PropertyPath("LastReadValueError");
								listComboBox.SetBinding(BitSelection.IsErrorProperty, val);

								listComboBox.SelectionChanged += multiBitSelection_SelectionChanged;
								listStackPanel.Children.Add(listComboBox);
								control.Add(listStackPanel);
								AddToNamedControls(listComboBox);
							}
							else
							{
								mask = ConvertHexToInt(item.Attribute("Mask").Value);
								var listObject = new ListObject
								{
									Label = label,
									Map = map,
									Mask = mask.ToString(),
									Description = description
								};

								foreach (var option in item.Elements())
								{
									string optionLabel = option.Attribute("Label").Value;
									string optionValue = option.Attribute("Value").Value;
									objects.Add(new Option { Label = optionLabel, Value = optionValue });
								}

								listLabel.Width = _labelWidth;
								listLabel.Content = label;
								listStackPanel.Children.Add(listLabel);

								listComboBox.Name = name;
								listComboBox.HorizontalAlignment = HorizontalAlignment.Right;
								listComboBox.VerticalAlignment = VerticalAlignment.Center;
								listComboBox.ItemsSource = objects;
								listComboBox.DisplayMemberPath = "Label";
								listComboBox.SelectedValuePath = "Value";
								listComboBox.Tag = listObject;
								listComboBox.ToolTip = toolTipShort;
								listComboBox.RegisterSource = _register.GetRegister(map.Split('|')[0].Replace("_", ""));

								var val = new Binding
								{
									Converter = new ValueConverter(),
									ConverterParameter = listObject,
									Mode = BindingMode.OneWay
								};
								val.Source = listComboBox.RegisterSource;
								val.Path = new PropertyPath("LastReadValue");
								listComboBox.SetBinding(BitSelection.SelectedValueProperty, val);

								val = new Binding();
								val.Source = listComboBox.RegisterSource;
								val.Path = new PropertyPath("LastReadValueError");
								listComboBox.SetBinding(BitSelection.IsErrorProperty, val);

								listComboBox.SelectionChanged += bitSelection_SelectionChanged;

								listStackPanel.Children.Add(listComboBox);

								control.Add(listStackPanel);
								AddToNamedControls(listComboBox);
							}
							break;
						}
					case "AutoList":
						{
							decimal initValue = decimal.Parse(item.Attribute("InitValue").Value);
							decimal step = decimal.Parse(item.Attribute("Step").Value);
							string unit = item.Attribute("Unit").Value;
							string direction = item.Attribute("Direction").Value;
							string format = item.Attribute("Format").Value;
							int valueIndex = item.Attribute("ValueIndex") != null ? int.Parse(item.Attribute("ValueIndex").Value) : 0;

							// Create Stack Panel
							var autoListStackPanel = new StackPanel();
							autoListStackPanel.Orientation = Orientation.Horizontal;

							// Create Label and ComboBox
							var autoListLabel = new Label();
							var autoListComboBox = new BitSelection();

							mask = ConvertHexToInt(item.Attribute("Mask").Value);
							var autoListObject = new ListObject
							{
								Label = label,
								Map = map,
								Mask = mask.ToString(),
								Description = description
							};

							int numberOfBits = CountSetBits(mask);
							int start = valueIndex;
							int end = (int)Math.Pow(2d, numberOfBits);
							string hexPrefix = format.Contains('X') ? "0x" : "";
							var iVal = initValue;
							string dval = format.Contains('X') ? Convert.ToInt32(iVal).ToString(format) : iVal.ToString(format);

							if (direction.ToLower() == "asc")
							{
								for (int i = start; i < end; i++)
								{
									string optionLabel = string.Format("{0}{1}{2}", hexPrefix, dval, unit);
									objects.Add(new Option { Label = optionLabel, Value = i.ToString() });
									iVal += step;
									dval = format.Contains('X') ? Convert.ToInt32(iVal).ToString(format) : iVal.ToString(format);
								}
							}
							else
							{
								for (int i = end - 1; i > start; i--)
								{
									string optionLabel = string.Format("{0}{1}{2}", hexPrefix, dval, unit);
									objects.Add(new Option { Label = optionLabel, Value = i.ToString() });
									iVal -= step;
									dval = format.Contains('X') ? Convert.ToInt32(iVal).ToString(format) : iVal.ToString(format);
								}
							}

							autoListLabel.Width = _labelWidth;
							autoListLabel.Content = label;
							autoListStackPanel.Children.Add(autoListLabel);

							autoListComboBox.Name = name;
							autoListComboBox.HorizontalAlignment = HorizontalAlignment.Right;
							autoListComboBox.VerticalAlignment = VerticalAlignment.Center;
							autoListComboBox.ItemsSource = objects;
							autoListComboBox.DisplayMemberPath = "Label";
							autoListComboBox.SelectedValuePath = "Value";
							autoListComboBox.SelectedIndex = 0;
							autoListComboBox.Tag = autoListObject;
							autoListStackPanel.Children.Add(autoListComboBox);
							autoListComboBox.ToolTip = toolTipShort;

							autoListComboBox.RegisterSource = _register.GetRegister(map.Split('|')[0].Replace("_", ""));

							var val = new Binding
							{
								Converter = new ValueConverter(),
								ConverterParameter = autoListObject,
								Mode = BindingMode.OneWay
							};
							val.Source = autoListComboBox.RegisterSource;
							val.Path = new PropertyPath("LastReadValue");
							autoListComboBox.SetBinding(BitSelection.SelectedValueProperty, val);

							val = new Binding();
							val.Source = autoListComboBox.RegisterSource;
							val.Path = new PropertyPath("LastReadValueError");
							autoListComboBox.SetBinding(BitSelection.IsErrorProperty, val);

							autoListComboBox.SelectionChanged += bitSelection_SelectionChanged;

							control.Add(autoListStackPanel);
							AddToNamedControls(autoListComboBox);
							break;
						}
					case "Toggle":
						{
							var toggleObject = new ToggleObject { Label = label, Map = map, Description = description };
							var ledBitButton = new LedBitButton();
							ledBitButton.Content = label;
							ledBitButton.RegisterSource = _register.GetRegister(map.Split('|')[0].Replace("_", ""));
							ledBitButton.BitSource = _register.GetRegisterBit(map.Split('|')[0].Replace("_", "") + "_" + map.Split('|')[1].Replace("_", ""));
							ledBitButton.Tag = toggleObject;
							ledBitButton.Command = ToggleBitCommand;
							ledBitButton.DataContext = this;
							ledBitButton.ToolTip = toolTipShort;
							ledBitButton.HorizontalAlignment = HorizontalAlignment.Left;

							var val = new Binding();
							val.Source = ledBitButton.BitSource;
							val.Path = new PropertyPath("LastReadValue");
							ledBitButton.SetBinding(LedBitButton.IsSetProperty, val);

							val = new Binding();
							val.Source = ledBitButton.BitSource;
							ledBitButton.SetBinding(LedBitButton.CommandParameterProperty, val);

							val = new Binding();
							val.Source = ledBitButton.RegisterSource;
							val.Path = new PropertyPath("LastReadValueError");
							ledBitButton.SetBinding(LedBitButton.IsErrorProperty, val);

							StackPanel sp = new StackPanel();
							sp.Orientation = Orientation.Horizontal;
							sp.Children.Add(ledBitButton);

							CheckBox cb = new CheckBox();
							cb.Tag = ledBitButton;
							cb.Name = ledBitButton.BitSource.DisplayName;
							cb.VerticalAlignment = VerticalAlignment.Center;
							cb.BorderBrush = Brushes.Gray;
							//cb.Checked += cb_Checked;
							//cb.Unchecked += cb_Unchecked;
							ledCheckBoxes.Add(cb);
							sp.Children.Add(cb);

							control.Add(sp);
							break;
						}
					case "BitStatus":
						var bitStatusObject = new BitStatusObject { Label = label, Map = map, Description = description };
						var bitStatus = new BitStatusLabel();
						bitStatus.RegisterSource = _register.GetRegister(map.Split('|')[0].Replace("_", ""));
						bitStatus.BitSource = _register.GetRegisterBit(map.Split('|')[0].Replace("_", "") + "_" + map.Split('|')[1].Replace("_", ""));
						bitStatus.Tag = bitStatusObject;
						bitStatus.Content = map.Split('|')[1];
						bitStatus.ToolTip = toolTipLong;

						bitStatus.DataContext = this;

						var set = new Binding();
						set.Source = bitStatus.BitSource;
						set.Path = new PropertyPath("LastReadValue");
						bitStatus.SetBinding(BitStatusLabel.IsSetProperty, set);

						set = new Binding();
						set.Source = bitStatus.RegisterSource;
						set.Path = new PropertyPath("LastReadValueError");
						bitStatus.SetBinding(BitStatusLabel.IsErrorProperty, set);

						XAttribute onBackColor = item.Attribute("OnBackColor");
						if (onBackColor != null)
						{
							bitStatus.OnSetBackgroundColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString(onBackColor.Value));
						}

						XAttribute onForeColor = item.Attribute("OnForeColor");
						if (onForeColor != null)
						{
							bitStatus.OnSetForegroundColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString(onForeColor.Value));
						}

						control.Add(bitStatus);
						break;
					case "FormattedDisplay":
						ConfigureFormattedDisplay(item, control);
						break;
				}
			}
		}

		private void ConfigureFormattedDisplay(XElement item, ObservableCollection<FrameworkElement> control)
		{
			var txtRegisterDisplay = new TextBlock();
			var txtOutputDisplay = new TextBlock();
			string labelText = string.Empty;
			string[] maps = null;
			string[] masks = null;
			string formula = string.Empty;
			string format = string.Empty;
			string description = string.Empty;
			string unit = string.Empty;
			string name = string.Empty;
			if (item != null)
			{
				labelText = item.Attribute("Label").Value;
				maps = item.Attribute("Map").Value.Split('|');
				masks = item.Attribute("Mask").Value.Split('|');
				formula = item.Attribute("Transform").Value;
				format = item.Attribute("Format").Value;
				unit = item.Attribute("Unit").Value;
				description = item.Attribute("Description").Value;

				// Create a unique name using the map string
				if (item.Attribute("Name") != null)
				{
					name = item.Attribute("Name").Value;
				}

				int lsb = ConvertHexToInt(masks[0]);
				int msb = ConvertHexToInt(masks[1]);

				var stackPanel = new StackPanel();
				stackPanel.Orientation = Orientation.Horizontal;

				var label = new Label();
				label.Content = labelText;
				label.Width = _labelWidth;
				stackPanel.Children.Add(label);

				Border border = new Border();
				border.CornerRadius = new CornerRadius(4);
				border.BorderThickness = new Thickness(1d);
				border.BorderBrush = Brushes.LightGray;
				border.Background = Brushes.WhiteSmoke;

				DockPanel dockPanel = new DockPanel();
				dockPanel.VerticalAlignment = VerticalAlignment.Center;

				txtOutputDisplay.Name = name + "_Out";
				txtOutputDisplay.Text = "0";
				txtOutputDisplay.Width = 60;
				txtOutputDisplay.TextAlignment = TextAlignment.Center;
				txtOutputDisplay.ToolTip = "ADC Converted Value";
				dockPanel.Children.Add(txtOutputDisplay);

				Binding val = new Binding
				{
					Source = txtRegisterDisplay,
					Path = new PropertyPath("Text"),
					Converter = new TransformDisplayConverter(),
					ConverterParameter = string.Format("{0}|{1}|{2}", formula, format, unit),
					UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
				};
				txtOutputDisplay.SetBinding(TextBlock.TextProperty, val);

				Register ledLsb = _register.GetRegister(maps[0].Replace("_", ""));
				Register ledMsb = _register.GetRegister(maps[1].Replace("_", ""));

				string toolTip = (ledLsb != ledMsb)
					? "Registers " + ledLsb.Name + " | " + ledMsb.Name
					: "Register " + ledLsb.Name;
				string toolTipText = string.Format("{0}", toolTip);

				txtRegisterDisplay.Name = name + "_Reg";
				txtRegisterDisplay.Tag = masks;
				txtRegisterDisplay.Width = 50;
				txtRegisterDisplay.Margin = new Thickness(2, 0, 0, 0);
				txtRegisterDisplay.TextAlignment = TextAlignment.Center;
				txtRegisterDisplay.ToolTip = toolTipText;
				dockPanel.Children.Add(txtRegisterDisplay);

				var multi = new MultiBinding();
				multi.Converter = new MultiRegisterStringValueConverter();
				multi.ConverterParameter = masks;
				multi.UpdateSourceTrigger = UpdateSourceTrigger.Explicit;
				multi.Bindings.Add(new Binding()
				{
					Source = ledLsb,
					Path = new PropertyPath("LastReadValue"),
				});

				multi.Bindings.Add(new Binding()
				{
					Source = ledMsb,
					Path = new PropertyPath("LastReadValue"),
				});

				txtRegisterDisplay.SetBinding(TextBlock.TextProperty, multi);

				DockPanel.SetDock(txtRegisterDisplay, Dock.Left);
				DockPanel.SetDock(txtOutputDisplay, Dock.Left);
				border.Child = dockPanel;
				stackPanel.Children.Add(border);
				control.Add(stackPanel);

				if (!string.IsNullOrEmpty(name))
				{
					AddToNamedControls(txtOutputDisplay);
					AddToNamedControls(txtRegisterDisplay);
				}
			}
		}

		private int CountSetBits(int value)
		{
			return Convert.ToString(value, 2).ToCharArray().Count(c => c == '1');
		}

		private int ConvertHexToInt(string hexValue)
		{
			string hex = (hexValue.ToUpper().Contains("0X")) ? hexValue.Substring(2) : hexValue;
			return Convert.ToInt32(hex, 16);
		}

		private int GetMaskedValue(int mask, int value)
		{
			int i = 0;
			for (i = 0; i < 8; i++)
			{
				if ((mask & 1) == 1)
				{
					break;
				}
				else
				{
					mask = (mask >> 1);
				}
			}

			return value << i;
		}

		private async void LoadRegisters()
		{
			ofd.Filter = "Register files (*.map)|*.map";
			if (ofd.ShowDialog() == true)
			{
				RegisterMap map = XmlDeserializeRegisterMap(ofd.FileName);
				if (map == null)
				{
					return;
				}

				if (map.DeviceName == _device.DisplayName)
				{
					try
					{
						// Open system registers
						_register.WriteRegister(0x41, 0x37);

						bool status = await Task<bool>.Run(() => _register.LoadRegisters(map));

						// Close system registers
						_register.WriteRegister(0x41, 0xFF);
					}
					catch (Exception e)
					{
						MessageBox.Show("Loading register map failed.\r\nReason: " + e.Message);
					}
				}
			}
		}

		private async void SaveRegisters()
		{
			sfd.Filter = "Register files (*.map)|*.map";
			if (sfd.ShowDialog() == true)
			{
				// Open system registers
				_register.WriteRegister(0x41, 0x37);

				// Read all registers and create the map
				RegisterMap regMap = (RegisterMap)
					await Task<RegisterMap>.Run(() => _register.CreateRegisterMap());

				// Close system registers
				_register.WriteRegister(0x41, 0xFF);

				// Serialize the data to file
				XmlSerializeRegisterMap(sfd.FileName, regMap);
			}
		}

		private void SerializeRegisterMap(string fileName, RegisterMap map)
		{
			FileStream fs = new FileStream(fileName, FileMode.Create);
			BinaryFormatter formatter = new BinaryFormatter();
			try
			{
				formatter.Serialize(fs, map);
			}
			catch (SerializationException e)
			{
				MessageBox.Show("Failed to serialize register map.\r\nReason: " + e.Message);
			}
			finally
			{
				fs.Close();
			}
		}

		private void XmlSerializeRegisterMap(string fileName, RegisterMap map)
		{
			FileStream fs = new FileStream(fileName, FileMode.Create);
			XmlSerializer formatter = new XmlSerializer(typeof(RegisterMap), new Type[] { typeof(Map), typeof(SystemData) });
			try
			{
				formatter.Serialize(fs, map);
			}
			catch (InvalidOperationException e)
			{
				MessageBox.Show("Failed to serialize register map.\r\nReason: " + e.Message);
			}
			finally
			{
				fs.Close();
			}
		}

		private RegisterMap DeserializeRegisterMap(string fileName)
		{
			RegisterMap map = null;
			FileStream fs = new FileStream(fileName, FileMode.Open);
			try
			{
				BinaryFormatter formatter = new BinaryFormatter();
				map = (RegisterMap)formatter.Deserialize(fs);
			}
			catch (SerializationException e)
			{
				Console.WriteLine("Failed to deserialize register map.\r\nReason: " + e.Message);
			}
			finally
			{
				fs.Close();
			}
			return map;
		}

		private RegisterMap XmlDeserializeRegisterMap(string fileName)
		{
			RegisterMap map = null;
			FileStream fs = new FileStream(fileName, FileMode.Open);
			XmlSerializer formatter = new XmlSerializer(typeof(RegisterMap), new Type[] { typeof(Map), typeof(SystemData) });
			try
			{
				map = (RegisterMap)formatter.Deserialize(fs);
			}
			catch (InvalidOperationException e)
			{
				map = null;
				MessageBox.Show("Failed to deserialize register map.\r\nReason: " + e.Message);
			}
			finally
			{
				fs.Close();
			}

			return map;
		}

		private bool IsDigit(char ch)
		{
			string hexCharacter = "0123456789";
			return hexCharacter.Contains(ch);
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

		private void multiBitSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (_isEditing)
				return;

			try
			{
				// Get the value
				ushort i = 0;

				if (e.AddedItems.Count > 0)
				{
					if (e.AddedItems[0] is Option)
					{
						Option o = e.AddedItems[0] as Option;
						i = ushort.Parse(o.Value.ToString());
					}
				}

				// Get the masks for each register
				var listObject = (ListObject)(((Control)sender).Tag);
				var maps = listObject.Map.Split('|');
				var masks = listObject.Mask.Split('|');

				// Convert the xml string values to integers
				List<int> maskValues = new List<int>();
				foreach (object o in (Array)masks)
				{
					maskValues.Add(ConvertHexToInt(o.ToString()));
				}

				// How many bits in the LSB
				int numberOfBits = CountSetBits(maskValues[0]);
				int shift = 8 - numberOfBits;
				int lsbMask = maskValues[0];
				int msbMask = maskValues[1];
				byte lsb = (byte)(((i << shift) & lsbMask));
				byte msb = (byte)(i >> numberOfBits & 0xFF);

				var lsbVal = _register.Registers.First(r => r.DisplayName.Equals(maps[0]));
				var msbVal = _register.Registers.First(r => r.DisplayName.Equals(maps[1]));

				byte lsbOrig = (byte)lsbVal.LastReadValue;
				int lsbNew = (lsbOrig & ~lsbMask) | (lsb & lsbMask);

				byte msbOrig = (byte)msbVal.LastReadValue;
				int msbNew = (msbOrig & ~msbMask) | (msb & msbMask);

				// Write/Read the registers.
				_isEditing = true;
				_register.WriteRegister(maps[0], lsbNew);
				_register.WriteRegister(maps[1], msbNew);
				_register.ReadRegisterValue(maps[0]);
				_register.ReadRegisterValue(maps[1]);

				if (_deviceBase != null)
				{
					_deviceBase.CheckDevice(null);
				}
			}
			catch (Exception ex)
			{
				MessengerSend(ex);
			}
			finally
			{
				_isEditing = false;
			}
		}

		private void bitSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (_isEditing)
				return;

			BitSelection bs = sender as BitSelection;
			ListObject lo = bs.Tag as ListObject;

			if (bs.SelectedValue == null)
				return;

			// This check is needed to prevent rewriting the previous value when binding calls this event
			// during a register change from another source.
			if (e.RemovedItems.Count > 0)
			{
				if (e.RemovedItems[0] is Option)
				{
					Option o = e.RemovedItems[0] as Option;
					if (bs.SelectedValue.ToString() == o.Value)
						return;
				}
			}

			int val = int.Parse(bs.SelectedValue.ToString());
			int mask = int.Parse(lo.Mask);

			val = GetMaskedValue(mask, val);

			Register reg = bs.RegisterSource as Register;

			byte orig = (byte)reg.LastReadValue;

			//Adding if condition to make it work even when disabled is selected
			if (reg.Name == "DACCONFIG" && mask == 16)
			{; } 
			else if (val == orig)
				return;

			int x = (orig & ~mask) | (val & mask);

			try
			{
				_isEditing = true;
				_register.WriteRegister(reg, (uint)x);
				_register.ReadRegisterValue(reg);

				if (_deviceBase != null)
				{
					_deviceBase.ModifyPlugin(null, ConfigControls);
					_deviceBase.CheckDevice(null);

					//Code addition by Irfan
					if (reg.Name == "DACCONFIG" && mask == 16)
					{
						_deviceBase.ModifyDeviceConfig(bs.SelectedValue, SelectedConfig);
					}
				}
			}
			catch (Exception ex)
			{
				MessengerSend(ex);
			}
			finally
			{
				_isEditing = false;
			}
		}

		private void HandleNotification(NotificationMessage message)
		{
			if (message.Notification == Notifications.CleanupNotification)
			{
				Cleanup();
				Messenger.Default.Unregister(this);
			}
		}

		#endregion
	}

	public interface IUIObjects
	{
		string Label { get; set; }
		string Map { get; set; }
		string Description { get; set; }
	}

	public class ListObject : IUIObjects
	{
		public string Mask { get; set; }

		public string Description { get; set; }

		public string Label { get; set; }

		public string Map { get; set; }
	}

	public class ToggleObject : IUIObjects
	{
		public string Label { get; set; }
		public string Map { get; set; }
		public string Description { get; set; }
	}

	public class BitStatusObject : IUIObjects
	{
		public string Label { get; set; }
		public string Map { get; set; }
		public string Description { get; set; }
	}

	public class Option
	{
		public string Label { get; set; }
		public string Value { get; set; }
	}
}
