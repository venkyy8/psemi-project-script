using DeviceAccess;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using HardwareInterfaces;
using Microsoft.Win32;
using MpqControl.Converters;
using MpqControl.UIControls;
using PluginFramework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Xml.Linq;
using System.Xml.Serialization;


namespace MpqControl.ViewModel
{
	public class PluginViewModel : DependencyObject, ICleanup, INotifyPropertyChanged
	{
		#region Private Members

		private IPMBus _protocol;
		private IDevice _device;
		private IRegister _register;
		private CustomizeBase _deviceBase;
		private bool _isInternalMode;
		private readonly int _labelWidth = 90;
		private RelayCommand<Register> _sendByteCommand;
		private RelayCommand _loadRegisterCommand;
		private RelayCommand _saveRegisterCommand;
		private RelayCommand _programCommand;
		private RelayCommand _restoreCommand;
		private bool _isEditing = false;
		private OpenFileDialog ofd = new OpenFileDialog();
		private SaveFileDialog sfd = new SaveFileDialog();
		private List<FrameworkElement> _namedElements { get; set; }

		private Register _regMfrId;
		private Register _regMfrModel;
		private Register _regMfrRevision;
		private Register _regMfr4Digit;

		#endregion

		#region Properties

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

		public ObservableCollection<FrameworkElement> OutputVoltageLimitsControls { get; set; }
		public ObservableCollection<FrameworkElement> InputVoltageLimitsControls { get; set; }
		public ObservableCollection<FrameworkElement> OutputCurrentLimitsControls { get; set; }
		public ObservableCollection<FrameworkElement> TemperatureLimitsControls { get; set; }
		public ObservableCollection<FrameworkElement> ControlControls { get; set; }
		public ObservableCollection<FrameworkElement> SoftStartControls { get; set; }
		public ObservableCollection<FrameworkElement> ConfigControls { get; set; }
		public ObservableCollection<FrameworkElement> StatusControls { get; set; }

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
			}
		}

		public bool IsExternal
		{
			get { return !_isInternalMode; }
		}

		public Register MfrId
		{
			get { return _regMfrId; }
		}

		public Register MfrModel
		{
			get { return _regMfrModel; }
		}

		public Register MfrRevision
		{
			get { return _regMfrRevision; }
		}

		public Register Mfr4Digit
		{
			get { return _regMfr4Digit; }
		}

		#endregion

		#region Constructor

		public PluginViewModel(object device, bool isInternalMode)
		{
			_protocol = (device as Device).Adapter as IPMBus;
			_device = device as IDevice;
			_register = device as IRegister;
			_isInternalMode = isInternalMode;

			OutputVoltageLimitsControls = new ObservableCollection<FrameworkElement>();
			InputVoltageLimitsControls = new ObservableCollection<FrameworkElement>();
			OutputCurrentLimitsControls = new ObservableCollection<FrameworkElement>();
			TemperatureLimitsControls = new ObservableCollection<FrameworkElement>();
			ControlControls = new ObservableCollection<FrameworkElement>();
			SoftStartControls = new ObservableCollection<FrameworkElement>();
			ConfigControls = new ObservableCollection<FrameworkElement>();
			StatusControls = new ObservableCollection<FrameworkElement>();

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

			LoadUiElements(_device.UiElements);

			_regMfrId = _register.Registers.FirstOrDefault(r => r.Name == "MFR_ID");
			_regMfrModel = _register.Registers.FirstOrDefault(r => r.Name == "MFR_MODEL");
			_regMfrRevision = _register.Registers.FirstOrDefault(r => r.Name == "MFR_REVISION");
			_regMfr4Digit = _register.Registers.FirstOrDefault(r => r.Name == "MFR_4_DIGIT");

			// Register for notification messages
			Messenger.Default.Register<NotificationMessage>(this, HandleNotification);
		}

		#endregion

		#region Public Methods

		public void Cleanup()
		{
			// Nothing to cleanup
		}

		#endregion

		#region Private Methods
		private void LoadUiElements(XElement elements)
		{
			// Get reference to main XML Panel node
			var panels = elements.Descendants("Panel");

			// Get individual panel references
			var outputVoltageLimitsControls = panels.Where(p => p.Attribute("Name").Value == "Output Voltage");
			var inputVoltageLimitsControls = panels.Where(p => p.Attribute("Name").Value == "Input Voltage");
			var outputCurrentLimitsControls = panels.Where(p => p.Attribute("Name").Value == "Output Current");
			var temperatureLimitsControls = panels.Where(p => p.Attribute("Name").Value == "Temperature");
			var controlControls = panels.Where(p => p.Attribute("Name").Value == "Control");
			var softStartControls = panels.Where(p => p.Attribute("Name").Value == "Soft Start");
			var configControls = panels.Where(p => p.Attribute("Name").Value == "On - Off Configuration");
			var statusControls = panels.Where(p => p.Attribute("Name").Value == "Status");

			ParseElements(controlControls, ControlControls);

			ParseElements(softStartControls, SoftStartControls);

			ParseElements(configControls, ConfigControls);

			ParseElements(outputVoltageLimitsControls, OutputVoltageLimitsControls);

			ParseElements(inputVoltageLimitsControls, InputVoltageLimitsControls);

			ParseElements(outputCurrentLimitsControls, OutputCurrentLimitsControls);

			ParseElements(temperatureLimitsControls, TemperatureLimitsControls);

			ParseElements(statusControls, StatusControls);
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
							FontWeight = FontWeights.Bold,
							FontStyle = FontStyles.Italic
						};
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
									Path = new PropertyPath("LastReadValueWithoutFormula"),
								});

								multi.Bindings.Add(new Binding()
								{
									Source = regMsb,
									Path = new PropertyPath("LastReadValueWithoutFormula"),
								});

								listComboBox.SetBinding(BitSelection.SelectedValueProperty, multi);

								var val = new Binding();
								val.Source = listComboBox.RegisterSource;
								val.Path = new PropertyPath("LastReadValueError");
								listComboBox.SetBinding(BitSelection.IsErrorProperty, val);

								listComboBox.SelectionChanged += multiBitSelection_SelectionChanged;
								listStackPanel.Children.Add(listComboBox);
								control.Add(listStackPanel);
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
								val.Path = new PropertyPath("LastReadValueWithoutFormula");
								listComboBox.SetBinding(BitSelection.SelectedValueProperty, val);

								val = new Binding();
								val.Source = listComboBox.RegisterSource;
								val.Path = new PropertyPath("LastReadValueError");
								listComboBox.SetBinding(BitSelection.IsErrorProperty, val);

								listComboBox.SelectionChanged += bitSelection_SelectionChanged;

								listStackPanel.Children.Add(listComboBox);

								control.Add(listStackPanel);
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
								for (int i = start; i <= end; i++)
								{
									string optionLabel = string.Format("{0}{1}{2}", hexPrefix, dval, unit);
									objects.Add(new Option { Label = optionLabel, Value = i.ToString() });
									iVal += step;
									dval = format.Contains('X') ? Convert.ToInt32(iVal).ToString(format) : iVal.ToString(format);
								}
							}
							else
							{
								for (int i = end - 1; i >= start; i--)
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

							ItemsPanelTemplate ItemsPanel = new ItemsPanelTemplate();
							var stackPanelTemplate = new FrameworkElementFactory(typeof(VirtualizingStackPanel));
							ItemsPanel.VisualTree = stackPanelTemplate;
							autoListComboBox.ItemsPanel = ItemsPanel;
							autoListComboBox.MinWidth = 60;
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
							val.Path = new PropertyPath("LastReadValueWithoutFormula");
							autoListComboBox.SetBinding(BitSelection.SelectedValueProperty, val);

							val = new Binding();
							val.Source = autoListComboBox.RegisterSource;
							val.Path = new PropertyPath("LastReadValueError");
							autoListComboBox.SetBinding(BitSelection.IsErrorProperty, val);

							autoListComboBox.SelectionChanged += bitSelection_SelectionChanged;

							control.Add(autoListStackPanel);
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
						bitStatus.Width = 120;
						bitStatus.HorizontalAlignment = HorizontalAlignment.Left;
						control.Add(bitStatus);
						break;
					case "FormattedDisplay":
						ConfigureFormattedDisplay(item, control);
						break;
					case "SendButton":
						{
							// Create Stack Panel
							var buttonStackPanel = new StackPanel();
							buttonStackPanel.Orientation = Orientation.Horizontal;

							// Create Label
							var buttonLabel = new Label();

							var sendObject = new SendByteObject { Label = label, Map = map, Description = description };
							var sendButton = new SendByteButton();
							sendButton.Content = "Send Command";
							sendButton.Style = Application.Current.FindResource("ArcticSandButton") as Style;
							sendButton.RegisterSource = _register.GetRegister(map.Split('|')[0].Replace("_", ""));
							sendButton.Tag = sendObject;
							sendButton.Command = SendByteCommand;
							sendButton.CommandParameter = _register.GetRegister(map.Split('|')[0].Replace("_", ""));
							sendButton.DataContext = this;
							sendButton.ToolTip = toolTipShort;
							sendButton.HorizontalAlignment = HorizontalAlignment.Left;
							buttonLabel.Content = label;
							buttonLabel.Width = _labelWidth;
							buttonStackPanel.Children.Add(buttonLabel);
							buttonStackPanel.Children.Add(sendButton);
							control.Add(buttonStackPanel);
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
					this._device.WriteWord(0xE7, 0x2000);
					Thread.Sleep(100);
					this._device.WriteWord(0xE7, 0x1000);
					Thread.Sleep(100);
					this._device.WriteWord(0xE7, 0x4000);
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

		private void ExecuteRestoreCommand()
		{
			try
			{
				var result = MessageBox.Show("Restore device now?", "MTP Memory Restore", MessageBoxButton.YesNo, MessageBoxImage.Question);
				if (result == MessageBoxResult.Yes)
				{
					byte[] data = new byte[0];
					this._device.WriteBlock(0x16, 0, ref data); // Hack for send byte
					Messenger.Default.Send(
						new CommunicationMessage
						{
							MessageType = MessageType.Ok,
							Sender = this,
							TopLevel = new Exception("MTP Command sent successfully!")
						});
					MessageBox.Show("MTP Command sent successfully!", "MTP Memory Restore", MessageBoxButton.OK, MessageBoxImage.None);
				}
			}
			catch (Exception ex)
			{
				MessengerSend(ex);
			}
		}

		private void ExecuteSendByteCommand(Register reg)
		{
			try
			{
				var data = new byte[0];
				this._device.WriteBlock(reg.Address, 0, ref data);
			}
			catch (Exception ex)
			{
				MessengerSend(ex);
			}
		}

		#endregion

		#region Event Handlers

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
					Path = new PropertyPath("LastReadValueWithoutFormula"),
				});

				multi.Bindings.Add(new Binding()
				{
					Source = ledMsb,
					Path = new PropertyPath("LastReadValueWithoutFormula"),
				});

				txtRegisterDisplay.SetBinding(TextBlock.TextProperty, multi);

				DockPanel.SetDock(txtRegisterDisplay, Dock.Left);
				DockPanel.SetDock(txtOutputDisplay, Dock.Left);
				border.Child = dockPanel;
				stackPanel.Children.Add(border);
				control.Add(stackPanel);
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
						bool status = await Task<bool>.Run(() => _register.LoadRegisters(map));
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
				// Read all registers and create the map
				RegisterMap regMap = (RegisterMap)
					await Task<RegisterMap>.Run(() => _register.CreateRegisterMap());

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

				byte lsbOrig = Convert.ToByte(lsbVal.LastReadValueWithoutFormula);
				int lsbNew = (lsbOrig & ~lsbMask) | (lsb & lsbMask);

				byte msbOrig = Convert.ToByte(msbVal.LastReadValueWithoutFormula);
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

			int orig = Convert.ToInt32(reg.LastReadValueWithoutFormula);

			if (val == orig)
				return;

			int x = (orig & ~mask) | (val & mask);

			try
			{
				_isEditing = true;
				_register.WriteRegister(reg, (uint)x);
				_register.ReadRegisterValue(reg);

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

		private void HandleNotification(NotificationMessage message)
		{
			if (message.Notification == Notifications.CleanupNotification)
			{
				Cleanup();
				Messenger.Default.Unregister(this);
			}
		}

		#endregion

		#region Commands

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

		public RelayCommand ProgramCommand
		{
			get
			{
				return _programCommand
					?? (_programCommand = new RelayCommand(ExecuteProgramCommand));
			}
		}

		public RelayCommand RestoreCommand
		{
			get
			{
				return _restoreCommand
					?? (_restoreCommand = new RelayCommand(ExecuteRestoreCommand));
			}
		}

		public RelayCommand<Register> SendByteCommand
		{
			get
			{
				return _sendByteCommand
					?? (_sendByteCommand = new RelayCommand<Register>(ExecuteSendByteCommand));
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

	public class SendByteObject : IUIObjects
	{
		public string Label { get; set; }
		public string Map { get; set; }
		public string Description { get; set; }
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
