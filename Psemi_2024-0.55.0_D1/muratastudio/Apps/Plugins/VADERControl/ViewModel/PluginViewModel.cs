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
using VADERControl.UIControls;
using System.Windows.Data;
using System.Windows;
using VADERControl.Converters;
using System.Windows.Media;
using System.IO;
using System.Xml.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using Microsoft.Win32;
using VADERControl.Properties;
using DeviceAccess;
using System.Reflection;
using System.Threading;
using System.Runtime.CompilerServices;
using Serilog;
using VADERControl.Helpers;
using System.Collections;

namespace VADERControl.ViewModel
{
	public class PluginViewModel : ViewModelBase
	{
		#region Private Members

		private IRegister _register;
		public IDevice _device;
		private bool _isInternalMode;
		private bool _isExport;
		public bool _loaded;
		private RelayCommand<Register.Bit> _toggleBitCommand;
		private RelayCommand _loadRegisterCommand;
		private RelayCommand _saveRegisterCommand;
		private RelayCommand _exportRegisterCommand;
		private RelayCommand<string> _executeScriptCommand;
		private RelayCommand _loadScriptCommand;
		private RelayCommand _cancelScriptCommand;
		private bool _isEditing = false;
		private OpenFileDialog ofd = new OpenFileDialog();

		internal ObservableCollection<ShadowRegister> LoadOTPRegisters()
		{
			try
			{
				ObservableCollection<ShadowRegister> mtpRegisterList = new ObservableCollection<ShadowRegister>();

				if (!String.IsNullOrEmpty(OTPPath))
				{
					using (StreamReader sr = File.OpenText(OTPPath))
					{
						while (!sr.EndOfStream)
						{
							string[] inLine = sr.ReadLine().Split(',');

							if ((inLine[0].StartsWith("confg")) || (inLine[0].StartsWith("config")) || (inLine[0] == "MFR_SPECIFIC_MATRIX"))
							{
								if (inLine[0] == "MFR_SPECIFIC_MATRIX")
								{
									mtpRegisterList?.Add(new ShadowRegister { OTPName = "MFR_SPECIFIC_MATRIX", OTPAddress = "0x0C0", OTPValue = "0x0FE4", IsEditable = true });
								}

								continue;
							}

							try
							{
								if (!String.IsNullOrEmpty(inLine[1]) && inLine[1].StartsWith("0x"))
								{
									string mtpDefaultValue = (inLine[2].ToUpper().Contains("0X")) ? inLine[2].Substring(2) : inLine[2];
									bool isEdit = true;
									string value = string.Empty;

									bool isIndexExists = inLine.ElementAtOrDefault(7) != null;

									if (!isIndexExists || String.IsNullOrEmpty(inLine[7]))
									{
										isIndexExists = inLine.ElementAtOrDefault(8) != null;

										if (!isIndexExists || String.IsNullOrEmpty(inLine[8]))
										{
											isIndexExists = inLine.ElementAtOrDefault(9) != null;
											if (isIndexExists)
											{
												value = inLine[9];
											}
										}
										else
										{
											value = inLine[8];

										}
									}
									else
									{
										value = inLine[7];
									}


									if (isIndexExists)
									{
										if (!String.IsNullOrEmpty(value))
										{
											if (value == "changed")
											{
												isEdit = true;
											}
											else if (value == "no change")
											{
												isEdit = false;
											}
										}

									}

									mtpRegisterList?.Add(new ShadowRegister { OTPName = inLine[0], OTPAddress = inLine[1], OTPValue = inLine[2], IsEditable = isEdit });
								}

							}
							catch (Exception ex)
							{
								Log.Error("RegisterControl PluginViewModel : LoadMTPRegisters 1- " + ex);
								EventLog.GetEventLogs(inLine[0]);
							}
						}

						OTPRegisters = mtpRegisterList;

						int count = 0;

						foreach (var a in OTPRegisters)
						{
							if (count == 0 || count % 8 == 0)
							{
								Pages.Add(a.OTPAddress);
							}
							count++;
						}
					}

					return OTPRegisters;
				}
			}
			catch (Exception ex)
			{
				Log.Error("RegisterControl PluginViewModel : LoadMTPRegisters 2 - " + ex);
				EventLog.GetEventLogs(ex.Message);
			}

			return null;
		}

		internal void Write(MappedRegister reg, string value, PluginViewModel pvm, object ireg)
		{
			throw new NotImplementedException();
		}

		private SaveFileDialog sfd = new SaveFileDialog();
		private List<CheckBox> ledCheckBoxes = new List<CheckBox>();
		private Slider slider = new Slider();
		private TextBox txtLEDIntensity = new TextBox();
		private TextBlock txtLEDBrightness = new TextBlock();
		private CustomizeBase _deviceBase;
		private ScriptManager _scriptMananger;
		private CancellationTokenSource _cancelSource;
		private List<byte[]> _registersToVerify;
		private ObservableCollection<ShadowRegister> _otpRegisters;
		private ObservableCollection<string> _pages;

		#endregion

		#region Properties

		public ObservableCollection<FrameworkElement> StartControls { get; set; }
		public ObservableCollection<FrameworkElement> Event0Controls { get; set; }
		public ObservableCollection<FrameworkElement> Event1Controls { get; set; }
		public ObservableCollection<FrameworkElement> CFGBlankControls { get; set; }
		public ObservableCollection<FrameworkElement> CFGDEGLITCHControls { get; set; }
		public List<Register> registers { get; set; }
		List<ShadowReg> shadowRegisters = new List<ShadowReg>();

		public ObservableCollection<string> Scripts { get; set; }


		public bool CheckOnorOffState
		{
			get
			{
				return _checkOnorOffState;
			}

			set
			{
				if (_checkOnorOffState == value)
				{
					return;
				}

				var oldValue = _checkOnorOffState;
				_checkOnorOffState = value;
				RaisePropertyChanged("CheckOnorOffState", oldValue, value, true);
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

		internal void CheckStartState()
		{
			int val;
			if (_register.Registers[52].DisplayName.ToUpper() == "CFG_START")
			{
				if (CheckOnorOffState)
				{
					val = 1;
				}
				else
				{
					val = 0;
				}
				_register.WriteRegisterBit(_register.Registers[52].Bits[7], (uint)val);
				_register.ReadRegisterValue((_register.Registers[52]));
			}
		}

		public ObservableCollection<MappedRegister> MappedRegisters
		{
			get;
			set;
		}

		private bool _startCheck;
		private bool _checkOnorOffState;

		public bool StartCheck
		{
			get { return _startCheck; }
			set
			{
				if (_startCheck == value)
				{
					return;
				}

				var oldValue = _startCheck;
				_startCheck = value;
				RaisePropertyChanged("StartCheck", oldValue, value, true);
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
			StartControls = new ObservableCollection<FrameworkElement>();
			Event0Controls = new ObservableCollection<FrameworkElement>();
			Event1Controls = new ObservableCollection<FrameworkElement>();
			CFGBlankControls = new ObservableCollection<FrameworkElement>();
			CFGDEGLITCHControls = new ObservableCollection<FrameworkElement>();
			registers = _register.Registers;

			// Create UI
			LoadUiElements(_device.UiElements);
			MappedRegisters = new ObservableCollection<MappedRegister>();

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


		public event PropertyChangedEventHandler PropertyChanged;

		public void OnPropertyChanged([CallerMemberName] string propertyName = "")
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}
		public string OTPPath { get; private set; }
		public ObservableCollection<ShadowRegister> OTPRegisters
		{
			get { return _otpRegisters; }
			private set
			{
				_otpRegisters = value;
				OnPropertyChanged("OTPRegisters");
			}
		}

		public ObservableCollection<string> Pages
		{
			get
			{
				return _pages;
			}
			set
			{
				if (value != _pages)
				{
					_pages = value;
					OnPropertyChanged("Pages");
				}
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

		public void ClearRegistersEvent(object sender, RoutedEventArgs e)
		{
			try
			{
				string regId = "EVENT0";//0X00
				string hexValue = "00";
				WriteToRegister(regId, hexValue);

				regId = "EVENT1";//0X0F4
				hexValue = "00";
				WriteToRegister(regId, hexValue);

			}
			catch (Exception)
			{

			}
		}
		private void WriteToRegister(string regId, string hexValue)
		{
			int decValue = int.Parse(hexValue, System.Globalization.NumberStyles.HexNumber);

			ArrayList masterTestList = new ArrayList();
			masterTestList.Add(regId);
			masterTestList.Add(decValue);

			Register reg = _register?.GetRegister(regId);

			try
			{
				_register.WriteRegister(regId, decValue);
				_register.ReadRegisterValue(reg);
			}
			catch (Exception e)
			{
				throw new Exception("Reading Value from Register (" + reg.DisplayName + ")", e);
			}
		}

		public bool BrowsePath()
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = "CSV files (*.csv)|*.csv";
			openFileDialog.InitialDirectory = @"C:\";
			if (openFileDialog.ShowDialog() == true)
			{
				return true;
			}
			else
				return false;
		}
		private void LoadUiElements(XElement elements)
		{
			// Get reference to main XML Panel node
			var panels = elements.Descendants("Panel");


			if (DeviceName.ToString().ToUpper() == "VADER")
			{
				var Start1Panel = panels.Where(p => p.Attribute("Name").Value == "START");
				var Event1Panel = panels.Where(p => p.Attribute("Name").Value == "EVENT1");
				var Event0Panel = panels.Where(p => p.Attribute("Name").Value == "EVENT0");
				var CFGBlankPanel = panels.Where(p => p.Attribute("Name").Value == "CFG_BLANK");
				var CFGDeglitchPanel = panels.Where(p => p.Attribute("Name").Value == "CFG_DEGLITCH");
				ParseElements(Start1Panel, StartControls);
				// Create the Event1 Panel
				ParseElements(Event1Panel, Event1Controls);

				// Create the Event0 Panel
				ParseElements(Event0Panel, Event0Controls);

				// Create the CFG BLANK Panel
				ParseElements(CFGBlankPanel, CFGBlankControls);

				// Create the CFG DEGLITCH Panel
				ParseElements(CFGDeglitchPanel, CFGDEGLITCHControls);
			}

			var resourceDictionary = new ResourceDictionary()
			{
				Source = new Uri("/VADERControl;component/Resources/Styles.xaml", UriKind.Relative)
			};

			Style style = resourceDictionary["BtnStyle"] as Style;

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
				string label = item.Attribute("Label").Value;
				string map = item.Attribute("Map").Value;
				int mask = 0;
				string name = string.Empty;

				name = (item.Attribute("Name") != null)
					? item.Attribute("Name").Value : string.Empty;

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

				switch (item.Name.LocalName)
				{
					case "List":
						// Create Stack Panel
						var listStackPanel = new StackPanel();
						listStackPanel.Orientation = Orientation.Horizontal;

						// Create Label and ComboBox
						var listLabel = new Label();
						var listComboBox = new BitSelection();

						// Determine if this list control has to span across multiple  registers
						bool multiRegister = item.Attribute("Mask").Value.Contains("|");

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
							//string optionValue = "None";
							objects.Add(new Option { Label = optionLabel, Value = optionValue });
						}
						listLabel.Width = 110;
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
						listComboBox.Width = 120;
						listComboBox.HorizontalContentAlignment = HorizontalAlignment.Center;
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

						if (panelText == "CFG_DEGLITCH")
						{
							var unitLabel = new Label();
							unitLabel.Width = 110;
							unitLabel.Content = "us";
							listStackPanel.Children.Add(unitLabel);
						}

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
					case "CheckBox":
						StackPanel sp1 = new StackPanel();
						sp1.Orientation = Orientation.Horizontal;

						CheckBox cb1 = new CheckBox();
						var lb = new CheckBoxControl();
						mask = ConvertHexToInt(item.Attribute("Mask").Value);
						var lbo = new ListObject
						{
							Label = label,
							Map = map,
							Mask = mask.ToString()
						};

						lb.Name = name;
						lb.HorizontalAlignment = HorizontalAlignment.Right;
						lb.VerticalAlignment = VerticalAlignment.Center;
						lb.Tag = lbo;
						lb.ToolTip = toolTipShort;
						lb.Width = 120;
						lb.HorizontalContentAlignment = HorizontalAlignment.Center;
						lb.RegisterSource1 = _register.GetRegister(map.Split('|')[0].Replace("_", ""));

						var val1 = new Binding
						{
							Converter = new ValueConverter(),
							ConverterParameter = lbo,
							Mode = BindingMode.OneWay
						};
						val1.Source = lb.RegisterSource1;
						val1.Path = new PropertyPath("LastReadValue");
						lb.SetBinding(CheckBoxControl.ValueProperty, val1);

						cb1.VerticalAlignment = VerticalAlignment.Center;
						cb1.BorderBrush = Brushes.Gray;
						cb1.Checked += cb_Checked;
						cb1.Unchecked += cb_Unchecked;
						sp1.Children.Add(cb1);

						control.Add(sp1);

						break;
				}
			}
		}

		private void ClickCheckBoxValue(object sender, RoutedEventArgs e)
		{

			CheckBoxControl bs = sender as CheckBoxControl;
			ListObject lo = bs.Tag as ListObject;
			if (lo.Map == "CFG_START")
			{
				if (lo.Label == "Disable Startup")
				{
					if (bs.RegisterSource1.LastReadString == "01")
					{
						
					}
					else
					{

					}
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
			ofd.Filter = "Register files (*.csv)|*.csv";
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

		private void SaveRegisters()
		{
			sfd.Filter = "Register files (*.csv)|*.csv";
			if (sfd.ShowDialog() == true)
			{
				using (var w = new StreamWriter(sfd.FileName))
				{

					var line = string.Format("{0},{1},{2}", "Register Name", "Register Address", "Register Data");
					w.WriteLine(line);
					foreach (var reg in shadowRegisters)
					{
						string name = reg.Name.ToString();
						string add = reg.Address.ToString();
						string data = reg.Data.ToString();
						if (((DeviceAccess.Device)_register).AllRegisters.Any(s => s.ID == reg.Name.Replace("_", ""))
							&& ((DeviceAccess.Device)_register).AllRegisters.FirstOrDefault(s => s.ID == reg.Name.Replace("_", "")).LastReadString != reg.Data.Split('x')[1])
						{
							data = "0x" + ((DeviceAccess.Device)_register).AllRegisters.FirstOrDefault(s => s.ID == reg.Name.Replace("_", "")).LastReadString;
						}
						var newLine = string.Format("{0},{1},{2}", name, add, data);
						w.WriteLine(newLine);
						w.Flush();
					}
				}
				//File.WriteAllText(sfd.FileName, sfd.FileName.ToString());
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
		public class ShadowReg
		{
			public string Name;
			public string Address;
			public string Data;
		}
		private RegisterMap XmlDeserializeRegisterMap(string fileName)
		{
			RegisterMap map = null;
			//FileStream fs = new FileStream(fileName, FileMode.Open);
			//XmlSerializer formatter = new XmlSerializer(typeof(RegisterMap), new Type[] { typeof(Map), typeof(SystemData) });
			try
			{
				StreamReader reader = null;
				if (File.Exists(fileName))
				{
					reader = new StreamReader(File.OpenRead(fileName));
					while (!reader.EndOfStream)
					{
						var line = reader.ReadLine();
						var values = line.Split(',');
						ShadowReg item = new ShadowReg();
						item.Name = line.Split(',')[0];
						item.Address = line.Split(',')[1];
						item.Data = line.Split(',')[2];
						shadowRegisters.Add(item);
						if(((DeviceAccess.Device)_register).AllRegisters.Any(s => s.ID == item.Name.Replace("_", "")) 
							&& ((DeviceAccess.Device)_register).AllRegisters.FirstOrDefault(s => s.ID == item.Name.Replace("_", "")).LastReadString != item.Data.Split('x')[1])
						{
							WriteToRegister(item.Name.Replace("_", ""), item.Data.Split('x')[1]);
                        }
						//WriteToRegister(regId, hexValue);
					}
					reader.Close();
				}
				else
				{
					Console.WriteLine("File doesn't exist");
				}
				Console.ReadLine();
				//map = (RegisterMap)formatter.Deserialize(fs);
			}
			catch (InvalidOperationException e)
			{
				//map = null;
				MessageBox.Show("Failed to deserialize register map.\r\nReason: " + e.Message);
			}
			finally
			{
				//fs.Close();
			}

			return map;
		}

		private bool IsDigit(char ch)
		{
			string hexCharacter = "0123456789";
			return hexCharacter.Contains(ch);
		}

		private void SaveCheckedSettings(object sender)
		{
			CheckBox cb = sender as CheckBox;
			bool b = (bool)cb.IsChecked;
			if (_register.Registers[52].DisplayName.ToUpper() == "CFG_START")
			{
				int val = 0;
				if (b)
					val = 1;
				_register.WriteRegisterBit(_register.Registers[52].Bits[7], (uint)val);
				_register.ReadRegisterValue((_register.Registers[52]));
			}

		}

		private void MessengerSend(Exception ex)
		{
			if (ex.InnerException != null)
				Debug.Print("Error: " + ex.InnerException.Message + " " + ex.Message);
			else
				Debug.Print("Error: " + ex.Message);
			Messenger.Default.Send(new CommunicationMessage(this, ex));
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

		private void ReadAll()
		{
			Messenger.Default.Send(new NotificationMessage(Notifications.ReadAllNotification));
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
			if (lo.Map == "CFG_DEGLITCH1" || lo.Map == "CFG_DEGLITCH2")
			{
				IEnumerable<Register> tmp = null;
				tmp = ((DeviceAccess.Device)_device).AllRegisters.Where(S => S.ID == "CFGDEGLITCH1");
				string st = Convert.ToString((int)((byte[])tmp.ToArray()[0].LastReadRawValue)[0], 2);
				if (st.Length < 4) st = "000" + st;
				if (lo.Map == "CFG_DEGLITCH1")
				{
					if (st.Substring(st.Length - 4, 2) == "10" || st.Substring(st.Length - 4, 2) == "11")
					{
						((ContentControl)(((Panel)CFGDEGLITCHControls[5]).Children)[2]).Content = "ms";
					}
					else ((ContentControl)(((Panel)CFGDEGLITCHControls[5]).Children)[2]).Content = "us";
				}
				tmp = ((DeviceAccess.Device)_device).AllRegisters.Where(S => S.ID == "CFGDEGLITCH2");
				st = Convert.ToString((int)((byte[])tmp.ToArray()[0].LastReadRawValue)[0], 2);
				if (st.Length < 4) st = "000" + st;
				bool vcc2 = ((DeviceAccess.Device)_device).AllRegisters.FirstOrDefault(S => S.ID == "CFGDEGLITCH2").Bits.FirstOrDefault(X => X.Name.Contains("VCC_OK2")).LastReadValue;
				if (lo.Map == "CFG_DEGLITCH2")
				{
					if (st.Substring(st.Length - 4, 2) == "10" || st.Substring(st.Length - 4, 2) == "11")
					{
						((ContentControl)(((Panel)CFGDEGLITCHControls[9]).Children)[2]).Content = "ms";
					}
					else ((ContentControl)(((Panel)CFGDEGLITCHControls[9]).Children)[2]).Content = "us";
				}
			}

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

	public class ShadowRegister : INotifyPropertyChanged
	{
		private string _address = "0x00";
		private string _name = "";
		private bool isEditable = false;
		private string _lastReadValue;
		private string _mtpDefaultValue;
		private string _rwRegister;

		public string OTPName
		{
			get { return _name; }

			internal set
			{
				if (_name != value)
				{
					_name = value;
					OnPropertyChanged("OTPName");
				}
			}
		}

		public string OTPAddress
		{
			get { return _address; }

			internal set
			{
				if (_address != value)
				{
					_address = value;
					OnPropertyChanged(")TPAddress");
				}
			}
		}

		public string OTPValue
		{
			get { return _lastReadValue; }
			set
			{
				if (_lastReadValue != value)
				{
					_lastReadValue = value;
					OnPropertyChanged("OTPValue");
				}
			}
		}

		public string OTPDefaultValue
		{
			get { return _mtpDefaultValue; }
			set
			{
				if (_mtpDefaultValue != value)
				{
					_mtpDefaultValue = value;
					OnPropertyChanged("OTPDefaultValue");
				}
			}
		}

		public string RWRegister
		{
			get { return _rwRegister; }
			set
			{
				if (_rwRegister != value)
				{
					_rwRegister = value;
					OnPropertyChanged("RWRegister");
				}
			}
		}

		public bool IsEditable
		{
			get { return isEditable; }
			set
			{
				if (isEditable != value)
				{
					isEditable = value;
					OnPropertyChanged("IsEditable");
				}
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public void OnPropertyChanged([CallerMemberName] string propertyName = "")
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}
	}

	public class OTPRWRegister : INotifyPropertyChanged
	{
		private int _slno;
		private string _address = "0x00";
		private string _data = "";

		public int SLNo
		{
			get { return _slno; }

			internal set
			{
				if (_slno != value)
				{
					_slno = value;
					OnPropertyChanged("SLNo");
				}
			}
		}

		public string Address
		{
			get { return _address; }

			internal set
			{
				if (_address != value)
				{
					_address = value;
					OnPropertyChanged("Address");
				}
			}
		}

		public string Data
		{
			get { return _data; }

			internal set
			{
				if (_data != value)
				{
					_data = value;
					OnPropertyChanged("Value");
				}
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public void OnPropertyChanged([CallerMemberName] string propertyName = "")
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}
	}
}
