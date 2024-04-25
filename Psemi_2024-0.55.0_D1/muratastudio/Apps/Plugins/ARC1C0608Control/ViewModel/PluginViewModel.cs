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
using GalaSoft.MvvmLight.Command;
using System.Diagnostics;
using System.Xml.Linq;
using ARC1C0608Control.UIControls;
using System.Windows.Data;
using System.Windows;
using ARC1C0608Control.Converters;
using System.Windows.Media;
using System.IO;
using System.Xml.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using Microsoft.Win32;
using DeviceAccess;
using System.Reflection;
using System.Threading;

namespace ARC1C0608Control.ViewModel
{
	public class PluginViewModel : ViewModelBase
	{
		#region Private Members

		private IRegister _register;
		private IDevice _device;
		private bool _isInternalMode;
		private bool _isExport;
		private RelayCommand<Register.Bit> _toggleBitCommand;
		private RelayCommand _loadRegisterCommand;
		private RelayCommand _saveRegisterCommand;
		private RelayCommand _exportRegisterCommand;
		private RelayCommand<string> _executeScriptCommand;
		private RelayCommand _loadScriptCommand;
		private RelayCommand _cancelScriptCommand;
		private bool _isEditing = false;
		private OpenFileDialog ofd = new OpenFileDialog();
		private SaveFileDialog sfd = new SaveFileDialog();
		private List<CheckBox> ledCheckBoxes = new List<CheckBox>();
		private Slider slider = new Slider();
		private TextBox txtLEDIntensity = new TextBox();
		private TextBlock txtLEDBrightness = new TextBlock();
		private CustomizeBase _deviceBase;
		private ScriptManager _scriptMananger;
		private CancellationTokenSource _cancelSource;
		private List<byte[]> _registersToVerify;
		private Register reg;
		private string _checkSumValue;

        public List<Register> RegistersList { get; private set; }

        private int _decValues;
        private uint _regAddress;

        #endregion

        #region Properties

        public ObservableCollection<FrameworkElement> StatusControls { get; set; }
		public ObservableCollection<FrameworkElement> CommandControls { get; set; }
		public ObservableCollection<FrameworkElement> ConfigControls { get; set; }
		public ObservableCollection<string> Scripts { get; set; }
		public List<int> CheckSumValues { get; private set; }
		public int CalculatedCheckSumValue { get; private set; }
		public string CheckSumValue
		{
			get { return _checkSumValue; }

			private set
			{
				if (_checkSumValue != value)
				{
					_checkSumValue = value;
					RaisePropertyChanged("CheckSumValue");
				}
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

		public bool IsExport
		{
			get
			{
				return _isExport;
			}

			set
			{
				if (_isExport == value)
				{
					return;
				}

				var oldValue = _isExport;
				_isExport = value;
				RaisePropertyChanged("IsExport", oldValue, value, true);
			}
		}

		#endregion

		#region Constructors

		public PluginViewModel()
		{

		}

		public PluginViewModel(object device, bool isInternalMode)
		{
			_device = device as IDevice;
			_register = device as IRegister;
			IsInternal = _isInternalMode = isInternalMode;
			StatusControls = new ObservableCollection<FrameworkElement>();
			CommandControls = new ObservableCollection<FrameworkElement>();
			ConfigControls = new ObservableCollection<FrameworkElement>();
			Scripts = new ObservableCollection<string>();
			_checkSumValue = string.Empty;
			RegistersList = _register.Registers;
			// This is a hack to identify a specific device.
			if (_device.DeviceInfoName == "ARC3C0845-R01")
			{
				IsExport = true;
			}

			// Create UI
			LoadUiElements(_device.UiElements);

			// Bind
			//txtLEDIntensity.TextChanged += txtLEDIntensity_TextChanged;

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

			// Register for notification messages
			Messenger.Default.Register<NotificationMessage>(this, HandleNotification);

			if (_deviceBase != null)
			{
				_deviceBase.CheckDevice(ConfigControls.ToList());
			}
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

		public RelayCommand ExportCommand
		{
			get
			{
				return _exportRegisterCommand
					?? (_exportRegisterCommand = new RelayCommand(ExecuteExportCommand));
			}
		}

		/// <summary>
		/// Gets the ExecuteScript.
		/// </summary>
		public RelayCommand<string> ExecuteScriptCommand
		{
			get
			{
				return _executeScriptCommand
					?? (_executeScriptCommand = new RelayCommand<string>(ExecuteExecuteScript));
			}
		}

		/// <summary>
		/// Gets the CancelScriptCommand.
		/// </summary>
		public RelayCommand CancelScriptCommand
		{
			get
			{
				return _cancelScriptCommand
					?? (_cancelScriptCommand = new RelayCommand(
					() =>
					{
						if (_cancelSource != null)
						{
							_cancelSource.Cancel();
						}
					}));
			}
		}

		/// <summary>
		/// Gets the LoadScript.
		/// </summary>
		public RelayCommand LoadScriptCommand
		{
			get
			{
				return _loadScriptCommand
					?? (_loadScriptCommand = new RelayCommand(
					() =>
					{
						var scriptPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\Scripts";
						OpenFileDialog openFileDialog = new OpenFileDialog();
						openFileDialog.Filter = "Script files (*.xml)|*.xml";
						openFileDialog.InitialDirectory = scriptPath;
						if (openFileDialog.ShowDialog() == true)
						{
							Scripts.Clear();
							_scriptMananger = new ScriptManager();
							try
							{
								_scriptMananger.Load(openFileDialog.FileName);

								foreach (var item in _scriptMananger.Scripts)
								{
									Scripts.Add(item.Name);
								}
							}
							catch (Exception ex)
							{
								MessageBox.Show("Error loading script file.\n\rError: " + ex.Message,
									"Script Loader", MessageBoxButton.OK, MessageBoxImage.Error);
							}
						}
					}));
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

		private void ExecuteExportCommand()
		{
			// This is a stripped down version of a true export to intel hex file format.
			// Since the output is always only one line we will simply construct the line of data in this method.
			try
			{
				byte[] data = new byte[21];
				data[0] = 0x10;     // Length
				data[1] = 0x00;     // Starting address MSB
				data[2] = 0x00;     // Starting address LSB
				data[3] = 0x00;     // Type Data = 0
									// Next 12 bytes Load from registers
				data[16] = 0x00;    // Register 0x0C
				data[17] = 0x00;    // Register 0x0D
				data[18] = 0x00;    // Register 0x0E
				data[19] = 0x00;    // Register 0x0F - This byte is set from the question Yes = 0x90, No = 0x00 (Default)
				data[20] = 0x00;    // Checksum, calculated at the end

				var result = MessageBox.Show("Include programming data for MTP?", "Export Registers to Intel Hex", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
				if (result == MessageBoxResult.Yes)
				{
					data[19] = 0x90;
				}
				else if (result == MessageBoxResult.Cancel)
				{
					return;
				}

				// Open a save file dialog box
				sfd.Filter = "Intel Hex files (*.hex)|*.hex";
				if (sfd.ShowDialog() == true)
				{
					// Read the data
					byte[] readData = new byte[15];
					this._device.ReadBlock(0x00, 12, ref readData);
					Array.Copy(readData, 0, data, 4, 12);
					int csum = 0;
					for (int i = 0; i < data.Length; i++)
					{
						csum += data[i];
					}
					data[20] = (byte)(0x100 - (csum & 0xff));

					// Write the file
					using (StreamWriter sw = File.CreateText(sfd.FileName))
					{
						StringBuilder sb = new StringBuilder();
						sb.Append(":");
						for (int i = 0; i < data.Length; i++)
						{
							sb.AppendFormat("{0:X2}", data[i]);
						}
						sw.Write(sb.ToString());
					}

					// Send message to log
					Messenger.Default.Send(
						new CommunicationMessage
						{
							MessageType = MessageType.Ok,
							Sender = this,
							TopLevel = new Exception("Intel Hex file created successfully!")
						});
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
			var panels = elements.Descendants("Panel");

			// Get individual panel references
			var statusPanel = panels.Where(p => p.Attribute("Name").Value == "Status");
			var commandPanel = panels.Where(p => p.Attribute("Name").Value == "Command");
			var configPanel = panels.Where(p => p.Attribute("Name").Value == "Configuration");

			// Create the LED Status Panel
			ParseElements(statusPanel, StatusControls);

			// Create the LED String Panel
			ParseElements(commandPanel, CommandControls);

			// Create the LED Config Panel
			ParseElements(configPanel, ConfigControls);

			StackPanel sp = new StackPanel();
			sp.Orientation = Orientation.Horizontal;

			var resourceDictionary = new ResourceDictionary()
			{
				Source = new Uri("/ARCxC0608Control;component/Resources/Styles.xaml", UriKind.Relative)
			};

			Style style = resourceDictionary["BtnStyle"] as Style;

			// Create LED ON and OFF buttons
			Button btnLedOn = new Button();
			btnLedOn.Content = "LEDs On";
			btnLedOn.Width = 60;
			btnLedOn.Margin = new Thickness(5, 5, 0, 0);
			btnLedOn.ToolTip = "Turns ON all LEDs that are checked.";
			btnLedOn.Style = style;
			btnLedOn.Click += btnLedOn_Click;

			Button btnLedOff = new Button();
			btnLedOff.Content = "LEDs Off";
			btnLedOff.Width = 60;
			btnLedOff.Margin = new Thickness(5, 5, 0, 0);
			btnLedOff.ToolTip = "Turns OFF all LEDs.";
			btnLedOff.Style = style;
			btnLedOff.Click += btnLedOff_Click;

			sp.Children.Add(btnLedOn);
			sp.Children.Add(btnLedOff);

			CommandControls.Add(sp);

			// Load default values
			for (int i = 0; i < ledCheckBoxes.Count; i++)
			{
				bool propertyValue = (bool)Properties.Settings.Default.GetType()
					.GetProperty(ledCheckBoxes[i].Name)
					.GetValue(Properties.Settings.Default);
				ledCheckBoxes[i].IsChecked = propertyValue;
			}
		}

		private void ConfigureLedIntensitySlider(XElement item, ObservableCollection<FrameworkElement> control)
		{
			string labelText = string.Empty;
			string[] maps = null;
			string[] masks = null;
			string description = string.Empty;
			if (item != null)
			{
				labelText = item.Attribute("Label").Value;
				maps = item.Attribute("Map").Value.Split('|');
				masks = item.Attribute("Mask").Value.Split('|');
				description = item.Attribute("Description").Value;

				int lsb = ConvertHexToInt(masks[0]);
				int msb = ConvertHexToInt(masks[1]);

				int numberOfBits = CountSetBits(lsb);
				numberOfBits += CountSetBits(msb);
				int sliderMax = (int)Math.Pow(2d, numberOfBits);
				double sliderTick = (double)sliderMax / 256;

				var stackPanel = new StackPanel();
				stackPanel.Name = "LEDIntStackPanel";
				stackPanel.Orientation = Orientation.Horizontal;

				var label = new Label();
				label.Content = labelText;
				label.Width = 160;
				stackPanel.Children.Add(label);

				slider.Name = "slValue";
				slider.Minimum = 0;
				slider.Maximum = sliderMax - 1;
				slider.TickFrequency = sliderTick;
				slider.Width = 180;
				slider.Tag = masks;
				slider.IsSnapToTickEnabled = true;
				slider.TickPlacement = System.Windows.Controls.Primitives.TickPlacement.BottomRight;
				slider.LostMouseCapture += slider_LostMouseCapture;

				DockPanel dockPanel = new DockPanel();
				dockPanel.VerticalAlignment = VerticalAlignment.Center;

				txtLEDBrightness.Text = "0";
				txtLEDBrightness.Width = 60;
				txtLEDBrightness.Foreground = Brushes.Gray;
				txtLEDBrightness.TextAlignment = TextAlignment.Center;
				dockPanel.Children.Add(txtLEDBrightness);

				Binding val = new Binding
				{
					Source = txtLEDIntensity,
					Path = new PropertyPath("Text"),
					Converter = new ValueToPercentConverter(),
					ConverterParameter = sliderMax - 1,
					UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
				};
				txtLEDBrightness.SetBinding(TextBlock.TextProperty, val);

				txtLEDIntensity.Tag = masks;
				txtLEDIntensity.Width = 60;
				txtLEDIntensity.Margin = new Thickness(5, 0, 0, 0);
				txtLEDIntensity.TextAlignment = TextAlignment.Center;
				txtLEDIntensity.PreviewTextInput += txtLEDIntensity_PreviewTextInput;
				txtLEDIntensity.TextChanged += txtLEDIntensity_TextChanged;
				txtLEDIntensity.PreviewKeyDown += txtLEDIntensity_KeyDown;
				txtLEDIntensity.GotFocus += txtLEDIntensity_GotFocus;
				txtLEDIntensity.LostFocus += txtLEDIntensity_LostFocus;
				txtLEDIntensity.ToolTip = "Use Up and Down arrow keys for +/- 1.";
				dockPanel.Children.Add(txtLEDIntensity);
				dockPanel.Children.Add(slider);

				val = new Binding
				{
					Source = slider,
					Path = new PropertyPath("Value"),
					UpdateSourceTrigger = UpdateSourceTrigger.Explicit,
				};
				txtLEDIntensity.SetBinding(TextBox.TextProperty, val);

				Register ledLsb = _register.GetRegister(maps[0].Replace("_", ""));
				Register ledMsb = _register.GetRegister(maps[1].Replace("_", ""));

				string toolTip = string.Format("{0}{1}{2}{3}{4}",
				ledLsb.DisplayName,
				Environment.NewLine,
				ledMsb.DisplayName,
				Environment.NewLine,
				description);
				slider.ToolTip = toolTip;

				var multi = new MultiBinding();
				multi.Converter = new MultiRegisterValueConverter();
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

				slider.SetBinding(Slider.ValueProperty, multi);

				DockPanel.SetDock(txtLEDIntensity, Dock.Right);
				DockPanel.SetDock(txtLEDBrightness, Dock.Right);
				stackPanel.Children.Add(dockPanel);
				control.Add(stackPanel);
			}
		}

		private void ParseElements(IEnumerable<XElement> elements, ObservableCollection<FrameworkElement> control)
		{
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
				string label = item.Attribute("Label").Value;
				string map = item.Attribute("Map").Value;

				string description = (item.Attribute("Description") != null)
					? item.Attribute("Description").Value : string.Empty;

				string toolTipShort = string.Format("{0}{1}{2}",
							map.Split('|')[0],
							Environment.NewLine,
							description);

				string toolTipLong = string.Format("{0}{1}{2}{3}{4}",
							map.Split('|')[0],
							Environment.NewLine,
							label,
							Environment.NewLine,
							description);

				int mask = 0;

				switch (item.Name.LocalName)
				{
					case "List":
						// Create Stack Panel
						var listStackPanel = new StackPanel();
						listStackPanel.Orientation = Orientation.Horizontal;

						// Create Label and ComboBox
						var listLabel = new Label();
						var listComboBox = new BitSelection();

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

						listLabel.Width = 160;
						listLabel.Content = label;
						listStackPanel.Children.Add(listLabel);

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

						break;
					case "AutoList":
						decimal initValue = decimal.Parse(item.Attribute("InitValue").Value);
						decimal step = decimal.Parse(item.Attribute("Step").Value);
						string unit = item.Attribute("Unit").Value;
						string direction = item.Attribute("Direction").Value;
						string format = item.Attribute("Format").Value;

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
						int start = 0;
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

						autoListLabel.Width = 160;
						autoListLabel.Content = label;
						autoListStackPanel.Children.Add(autoListLabel);

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

						val = new Binding
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
						break;
					case "Toggle":
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

						val = new Binding();
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
						cb.Checked += cb_Checked;
						cb.Unchecked += cb_Unchecked;
						ledCheckBoxes.Add(cb);
						sp.Children.Add(cb);

						control.Add(sp);
						break;
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
					case "Slider":
						ConfigureLedIntensitySlider(item, control);
						break;
				}
			}
		}
		public void CheckSumCalculation()
		{
			//Reading values of regs 0x00 to 0x0B and converting to decimal inorder to calculate the checksum value
			//Calculated checksum value is then converted back to hexa 

			try
			{
				CheckSumValues = new List<int>();

				if (CheckSumValues.Any())
				{
					CheckSumValues.Clear();
				}
			
				if (_register != null)
				{
					int regcount = 12;

					foreach (Register register in RegistersList)
					{
						RegistersList = RegistersList.OrderBy(n => n.Address == register.Address).ToList(); // sorted RegistersList in asc order
					}
					//foreach (var item in RegistersList) // Code to test if address in registerlist is sorted in ascending order.
					//{
					//	Console.WriteLine(item.Address.ToString());
					//}
					CalculatedCheckSumValue = 0;
					foreach (Register register in RegistersList)
					{
						_regAddress = register.Address;

						if (_regAddress < regcount)
						{
							_decValues = ReadRegisterValue(register);
							CheckSumValues.Add(_decValues);
						}
						else
						{
							break;
						}
					}

					foreach (int checksumvalue in CheckSumValues)
					{
						CalculatedCheckSumValue += checksumvalue;
					}
				}
				
				CheckSumValue = CalculatedCheckSumValue.ToString("X2");
			}
			catch(Exception ex)
            {
				MessengerSend(ex);
			}
		}

        private int ReadRegisterValue(Register register)
        {
			int decval = (int)((DeviceAccess.Device)_device).AllRegisters.FirstOrDefault(S => S.Address == register.Address).LastReadValue;
			return decval;
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

		private void ReadAll()
		{
			Messenger.Default.Send(new NotificationMessage(Notifications.ReadAllNotification));
		}
		private void WriteLedIntensity(object sender)
		{
			try
			{
				// Get the slider value
				ushort i = (ushort)(slider.Value);

				// Get the masks for each register
				var masks = ((Control)sender).Tag;

				// Convert the xml string values to integers
				List<int> maskValues = new List<int>();
				foreach (object o in (Array)masks)
				{
					maskValues.Add(ConvertHexToInt(o.ToString()));
				}

				var ditherEnabled = (byte)_register.ReadRegisterValue("WLEDISETLSB") & 0x02;


				// How many bits in the LSB
				int numberOfBits = CountSetBits(maskValues[0]);
				int shift = 8 - numberOfBits;
				int lsbMask = maskValues[0];

				//OR operation removed.
				//below code is commented for fixing the value change in slide bar
				//value while moving the scroll, once click is release, the slide bar
				//value alwasy increment by 1. 
				//byte lsb = (byte)((i << shift) | ditherEnabled);

				byte lsb = (byte)((i << shift));

				byte msb = (byte)(i >> numberOfBits & 0xFF);

				// Write/Read the registers.
				_register.WriteRegister("WLEDISETLSB", lsb);
				_register.WriteRegister("WLEDISETMSB", msb);
				_register.ReadRegisterValue("WLEDISETLSB");
				_register.ReadRegisterValue("WLEDISETMSB");

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

		private void SaveCheckedSettings(object sender)
		{
			CheckBox cb = sender as CheckBox;
			bool b = (bool)cb.IsChecked;

			Properties.Settings.Default.GetType()
					.GetProperty(cb.Name).SetValue(Properties.Settings.Default, b);

			Properties.Settings.Default.Save();
		}

		private void MessengerSend(Exception ex)
		{
			if (ex.InnerException != null)
				Debug.Print("Error: " + ex.InnerException.Message + " " + ex.Message);
			else
				Debug.Print("Error: " + ex.Message);
			Messenger.Default.Send(new CommunicationMessage(this, ex));
		}

		private async void ExecuteExecuteScript(string parameter)
		{
			// Clear the applicaiton log
			Messenger.Default.Send(new NotificationMessage(Notifications.CleanupNotification));

			var result = new ProgrammingMessageResult
			{
				Message = "MTP Programmed successfully!",
				Type = MessageType.Ok
			};

			if (_scriptMananger != null)
			{
				_registersToVerify = new List<byte[]>();

				await Task<ProgrammingMessageResult>.Run(() =>
				{
					// Read all registers
					ReadAll();
					Thread.Sleep(1000);

					// Run the script
					RunScript(parameter);

					// Refresh registers after programming
					ReadAll();
					Thread.Sleep(1000);

					var verifyMTP = VerifyMTP();
					if (!verifyMTP)
					{
						result.Message = "MTP_WRITE_DNE bit is not set.";
						result.Type = MessageType.Error;
					}

					var verifyRigisterData = VerifyRigisterData();
					if (!verifyRigisterData)
					{
						result.Message = "Registers did not verify!";
						result.Type = MessageType.Error;
					}
				});
			}
			else
			{
				result.Message = "Must load script before programming the MTP.";
				result.Type = MessageType.Warning;
			}

			Messenger.Default.Send(
				new CommunicationMessage
				{
					MessageType = result.Type,
					Sender = this,
					TopLevel = new Exception(result.Message)
				});
		}

		private void RunScript(string parameter)
		{
			if (parameter == null)
			{
				return;
			}

			_cancelSource = new CancellationTokenSource();
			Script script = _scriptMananger.Scripts.Find(s => s.Name == parameter);

			if (script == null)
				return;

			foreach (var item in script.Actions)
			{
				switch (item.Operation)
				{
					case OperationType.Delay:
						var delayAction = item as DelayAction;
						Thread.Sleep(delayAction.WaitMs);
						break;
					case OperationType.Read:
					case OperationType.Write:
						var transaction = item as Transaction;

						// There is no support for changing the I2C address since this code
						// was migrated from the protocol plug-in.
						// At this time, we are not going to display the bytes written or read
						// Instead, we will just manually read the bytes to refresh the display.
						for (int i = 0; i < transaction.RepeatCount; i++)
						{
							if (item.Operation == OperationType.Write)
							{
								// Store the register address and value for verification
								_registersToVerify.Add(new byte[] { transaction.RegisterAddress, transaction.Data[0] });
								byte[] writeData = transaction.Data;
								_device.WriteBlock(transaction.RegisterAddress, transaction.Data.Length, ref writeData);
							}
							else
							{
								byte[] readBytes = new byte[transaction.Length];
								_device.ReadBlock(transaction.RegisterAddress, transaction.Length, ref readBytes);
							}

							Thread.Sleep(transaction.InnerDelayMs);

							if (_cancelSource.IsCancellationRequested)
							{
								return;
							}
						}
						break;
				}
			}
		}

		private bool VerifyMTP()
		{
			// Verify MTP_WRITE_DNE bit is set
			var mtpWriteDne = _register.GetRegisterBit("STATUS2_MTPWRITEDNE");
			if (mtpWriteDne != null)
			{
				return mtpWriteDne.LastReadValue;
			}
			else
			{
				return false;
			}
		}

		private bool VerifyRigisterData()
		{
			bool match = true;
			foreach (var item in _registersToVerify)
			{
				var register = _register.Registers.Find(a => a.Address == item[0]);
				if (!register.ReadOnly)
				{
					if (register.LastReadValue != Convert.ToDouble(item[1]))
					{
						match = false;
						break;
					}
				}
			}
			return match;
		}

		#endregion

		#region Event Handlers

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

			if (val == orig)
				return;

			int x = (orig & ~mask) | (val & mask);

			try
			{
				_isEditing = true;
				_register.WriteRegister(reg, (uint)x);

				if (_deviceBase != null)
				{
					_deviceBase.ModifyDeviceConfig(lo.Label, reg, ConfigControls.ToList());
					_deviceBase.ModifyPlugin(null, ConfigControls);
					_deviceBase.CheckDevice(null);
				}

				Thread.Sleep(10);
				_register.ReadRegisterValue(reg);
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

		private void txtLEDIntensity_LostFocus(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(txtLEDIntensity.Text))
			{
				txtLEDIntensity.Text = slider.Value.ToString();
			}
			else
			{
				BindingExpression be = txtLEDIntensity.GetBindingExpression(TextBox.TextProperty);
				be.UpdateSource();
				WriteLedIntensity(sender);
			}
		}

		private void txtLEDIntensity_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (string.IsNullOrEmpty(txtLEDIntensity.Text.Trim()))
			{
				return;
			}

			if (int.Parse(txtLEDIntensity.Text) > slider.Maximum)
			{
				txtLEDIntensity.Text = slider.Maximum.ToString();
			}
		}

		void txtLEDIntensity_GotFocus(object sender, RoutedEventArgs e)
		{
			txtLEDIntensity.SelectAll();
		}

		private void txtLEDIntensity_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
		{
			if (e.Key == System.Windows.Input.Key.Up)
			{
				if (slider.Value != slider.Maximum)
				{
					slider.Value += 1;
					WriteLedIntensity(sender);
				}
			}
			else if (e.Key == System.Windows.Input.Key.Down)
			{
				if (slider.Value != slider.Minimum)
				{
					slider.Value -= 1;
					WriteLedIntensity(sender);
				}
			}
			else if (e.Key == System.Windows.Input.Key.Escape)
			{
				txtLEDIntensity.Text = slider.Value.ToString();
			}
			else if (e.Key == System.Windows.Input.Key.Enter)
			{
				if (string.IsNullOrEmpty(txtLEDIntensity.Text))
				{
					txtLEDIntensity.Text = slider.Value.ToString();
					txtLEDIntensity.SelectAll();
				}
				else
				{
					BindingExpression be = txtLEDIntensity.GetBindingExpression(TextBox.TextProperty);
					be.UpdateSource();
					WriteLedIntensity(sender);
					txtLEDIntensity.SelectAll();
				}
			}
		}

		private void txtLEDIntensity_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
		{
			e.Handled = e.Text.Any(ch => !IsDigit(ch));
		}

		private void cb_Checked(object sender, RoutedEventArgs e)
		{
			SaveCheckedSettings(sender);
		}

		private void cb_Unchecked(object sender, RoutedEventArgs e)
		{
			SaveCheckedSettings(sender);
		}

		private void btnLedOff_Click(object sender, RoutedEventArgs e)
		{
			int value = 0;
			foreach (var item in ledCheckBoxes)
			{
				if (item.IsChecked == true)
				{
					LedBitButton o = item.Tag as LedBitButton;
					ToggleObject t = o.Tag as ToggleObject;
					value += (int)Math.Pow(2d, (Convert.ToDouble(t.Map.Substring(t.Map.Length - 1)) - 1));
				}
			}

			try
			{
				// Invert the ON values to OFF
				value = ~value;
				var currentValue = (int)_register.ReadRegisterValue("LEDEN");
				var newValue = currentValue & value;

				_register.WriteRegister("LEDEN", newValue);
				_register.ReadRegisterValue("LEDEN");

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

		private void btnLedOn_Click(object sender, RoutedEventArgs e)
		{
			int value = 0;
			foreach (var item in ledCheckBoxes)
			{
				if (item.IsChecked == true)
				{
					LedBitButton o = item.Tag as LedBitButton;
					ToggleObject t = o.Tag as ToggleObject;
					value += (int)Math.Pow(2d, (Convert.ToDouble(t.Map.Substring(t.Map.Length - 1)) - 1));
				}
			}

			if (value == 0)
			{
				MessageBox.Show("Please check the LEDs that should be turned on together.",
					"LED ON Control", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			else
			{
				try
				{
					var currentValue = (int)_register.ReadRegisterValue("LEDEN");
					var newValue = currentValue | value;

					_register.WriteRegister("LEDEN", newValue);
					_register.ReadRegisterValue("LEDEN");

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
		}

		private void slider_LostMouseCapture(object sender, System.Windows.Input.MouseEventArgs e)
		{
			WriteLedIntensity(sender);
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

	public class ProgrammingMessageResult
	{
		public string Message { get; set; }

		public string Type { get; set; }
	}
}