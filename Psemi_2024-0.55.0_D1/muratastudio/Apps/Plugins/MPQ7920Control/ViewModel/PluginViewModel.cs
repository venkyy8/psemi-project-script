using DeviceAccess;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using HardwareInterfaces;
using Microsoft.Win32;
using Mpq7920Control.Converters;
using Mpq7920Control.UIControls;
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
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Xml.Linq;
using System.Xml.Serialization;


namespace Mpq7920Control.ViewModel
{
	public class PluginViewModel : PluginDeviceBase, ICleanup
	{
		#region Private Members

		private IPMBus _protocol;
		private IDevice _device;
		private IRegister _register;
		private CustomizeBase _deviceBase;
		private bool _isInternalMode;
		private readonly int _labelWidth = 150;
		private RelayCommand _programCommand;
		private RelayCommand _generateKeyCommand;
		private RelayCommand _loadRegisterCommand;
		private RelayCommand _saveRegisterCommand;
		private bool _isEditing = false;
		private OpenFileDialog ofd = new OpenFileDialog();
		private SaveFileDialog sfd = new SaveFileDialog();
		private List<FrameworkElement> _namedElements { get; set; }

		private string _password;
		private string _passwordFilePath;
		private Register _regMTPConfig;
		private Register _regMTPRevision;
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

		public ObservableCollection<FrameworkElement> BuckPanel1Controls { get; set; }
		public ObservableCollection<FrameworkElement> BuckPanel2Controls { get; set; }
		public ObservableCollection<FrameworkElement> BuckPanel3Controls { get; set; }
		public ObservableCollection<FrameworkElement> BuckPanel4Controls { get; set; }
		public ObservableCollection<FrameworkElement> ControlPanel0Controls { get; set; }
		public ObservableCollection<FrameworkElement> ControlPanel1Controls { get; set; }
		public ObservableCollection<FrameworkElement> ControlPanel2Controls { get; set; }
		public ObservableCollection<FrameworkElement> MTPControls { get; set; }
		public ObservableCollection<FrameworkElement> StatusControls { get; set; }

		public Register MTPConfig
		{
			get { return _regMTPConfig; }
		}

		public Register MTPRevision
		{
			get { return _regMTPRevision; }
		}

		public string Password
		{
			get { return _password; }

			set
			{
				if (_password == value)
				{
					return;
				}

				_password = value;
				OnPropertyChanged("Password");
			}
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
			}
		}

		public bool IsExternal
		{
			get { return !_isInternalMode; }
		}

		#endregion

		#region Constructor

		public PluginViewModel(object device, bool isInternalMode)
		{
			_protocol = (device as Device).Adapter as IPMBus;
			_device = device as IDevice;
			_register = device as IRegister;
			_isInternalMode = isInternalMode;

			_regMTPConfig = _register.GetRegister("MTPCTL1");
			_regMTPRevision = _register.GetRegister("MTPCTL2");

			BuckPanel1Controls = new ObservableCollection<FrameworkElement>();
			BuckPanel2Controls = new ObservableCollection<FrameworkElement>();
			BuckPanel3Controls = new ObservableCollection<FrameworkElement>();
			BuckPanel4Controls = new ObservableCollection<FrameworkElement>();
			ControlPanel0Controls = new ObservableCollection<FrameworkElement>();
			ControlPanel1Controls = new ObservableCollection<FrameworkElement>();
			ControlPanel2Controls = new ObservableCollection<FrameworkElement>();
			MTPControls = new ObservableCollection<FrameworkElement>();
			StatusControls = new ObservableCollection<FrameworkElement>();

			var passwordFileName = _device.DeviceName + ".dat";
			_passwordFilePath = Path.Combine(System.Reflection.Assembly.GetExecutingAssembly().Location, "../" + passwordFileName);

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
			var controlPanel0Controls = panels.Where(p => p.Attribute("Name").Value == "Ctl0");
			var controlPanel1Controls = panels.Where(p => p.Attribute("Name").Value == "Ctl1");
			var controlPanel2Controls = panels.Where(p => p.Attribute("Name").Value == "Ctl2");
			var mtpControls = panels.Where(p => p.Attribute("Name").Value == "MTP");
			ParseElements(controlPanel0Controls, ControlPanel0Controls, Orientation.Vertical, HorizontalAlignment.Left);
			ParseElements(controlPanel1Controls, ControlPanel1Controls, Orientation.Vertical, HorizontalAlignment.Left);
			ParseElements(controlPanel2Controls, ControlPanel2Controls, Orientation.Vertical, HorizontalAlignment.Left);
			ParseElements(mtpControls, MTPControls);

			var buckPanel1Controls = panels.Where(p => p.Attribute("Name").Value == "Buck1");
			var buckPanel2Controls = panels.Where(p => p.Attribute("Name").Value == "Buck2");
			var buckPanel3Controls = panels.Where(p => p.Attribute("Name").Value == "Buck3");
			var buckPanel4Controls = panels.Where(p => p.Attribute("Name").Value == "Buck4");
			ParseElements(buckPanel1Controls, BuckPanel1Controls);
			ParseElements(buckPanel2Controls, BuckPanel2Controls);
			ParseElements(buckPanel3Controls, BuckPanel3Controls);
			ParseElements(buckPanel4Controls, BuckPanel4Controls);

			var statusControls = panels.Where(p => p.Attribute("Name").Value == "Status");
			ParseElements(statusControls, StatusControls);
		}

		private void ParseElements(IEnumerable<XElement> elements,
			ObservableCollection<FrameworkElement> control,
			Orientation listOrientation = Orientation.Horizontal,
			HorizontalAlignment hAlign = HorizontalAlignment.Right,
			VerticalAlignment vAlign = VerticalAlignment.Center)
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
							listStackPanel.Orientation = listOrientation;

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
								listLabel.Foreground = Brushes.DarkGray;
								listLabel.FontWeight = FontWeights.Medium;
								listLabel.HorizontalAlignment = hAlign;
								listStackPanel.Children.Add(listLabel);

								listComboBox.Name = name;
								listComboBox.HorizontalAlignment = hAlign;
								listComboBox.VerticalAlignment = vAlign;
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
								listLabel.Foreground = Brushes.DarkGray;
								listLabel.FontWeight = FontWeights.Medium;
								listLabel.HorizontalAlignment = hAlign;
								listStackPanel.Children.Add(listLabel);

								listComboBox.Name = name;
								listComboBox.HorizontalAlignment = hAlign;
								listComboBox.VerticalAlignment = vAlign;
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
							autoListStackPanel.Orientation = listOrientation;

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
							autoListLabel.Foreground = Brushes.DarkGray;
							autoListLabel.FontWeight = FontWeights.Medium;
							autoListLabel.HorizontalAlignment = hAlign;
							autoListStackPanel.Children.Add(autoListLabel);

							ItemsPanelTemplate ItemsPanel = new ItemsPanelTemplate();
							var stackPanelTemplate = new FrameworkElementFactory(typeof(VirtualizingStackPanel));
							ItemsPanel.VisualTree = stackPanelTemplate;
							autoListComboBox.ItemsPanel = ItemsPanel;
							autoListComboBox.MinWidth = 60;
							autoListComboBox.Name = name;
							autoListComboBox.HorizontalAlignment = hAlign;
							autoListComboBox.VerticalAlignment = vAlign;
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
				}
			}
		}

		private void ExecuteGenerateKeyCommand()
		{
			var result = MessageBox.Show("Generate password key file now?", "MTP Generate Password", MessageBoxButton.YesNo, MessageBoxImage.Question);
			if (result == MessageBoxResult.Yes)
			{
				GeneratePasswordHash(Password);
			}
		}

		private void ExecuteProgramCommand()
		{
			try
			{
				if (VerifyPassword(Password))
				{
					var result = MessageBox.Show("Program device now?", "MTP Memory Write", MessageBoxButton.YesNo, MessageBoxImage.Question);
					if (result == MessageBoxResult.Yes)
					{
						this._device.WriteByte(MTPConfig.Address, (byte)MTPConfig.LastReadValue);
						Thread.Sleep(100);
						this._device.WriteByte(MTPRevision.Address, (byte)MTPRevision.LastReadValue);
						Thread.Sleep(100);
						this._device.WriteByte(0x26, 0x79);
						Thread.Sleep(100);
						this._device.WriteByte(0x26, 0x20);
						Thread.Sleep(100);
						this._device.WriteByte(0x25, 0x80);
						Thread.Sleep(100);
						this._device.WriteByte(0x25, 0xC0);

						_register.ReadRegisterValue(MTPConfig);
						_register.ReadRegisterValue(MTPRevision);

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
				else
				{
					MessageBox.Show("MTP password invalid!", "MTP Memory Write", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "MTP Memory Write", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void ExecuteRestoreCommand()
		{
		}

		private void ExecuteSendByteCommand(Register reg)
		{
		}

		private void GeneratePasswordHash(string password)
		{
			if (string.IsNullOrEmpty(password))
			{
				MessageBox.Show("MTP password cannot be blank.", "MTP Generate Password", MessageBoxButton.OK, MessageBoxImage.Error);
			}
			else
			{
				byte[] salt;
				new RNGCryptoServiceProvider().GetBytes(salt = new byte[16]);

				var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000);
				byte[] hash = pbkdf2.GetBytes(20);

				byte[] hashBytes = new byte[36];
				Array.Copy(salt, 0, hashBytes, 0, 16);
				Array.Copy(hash, 0, hashBytes, 16, 20);

				try
				{
					using (StreamWriter sw = File.CreateText(_passwordFilePath))
					{
						sw.Write(Convert.ToBase64String(hashBytes));
						sw.Flush();
					}

					MessageBox.Show("MTP password generated successfully", "MTP Generate Password", MessageBoxButton.OK, MessageBoxImage.None);

				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.Message, "MTP Generate Password", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		private bool VerifyPassword(string password)
		{
			if (!File.Exists(_passwordFilePath))
			{
				throw new Exception("MTP programming is not setup. Please contact muRata.");
			}

			if (password == null)
			{
				return false;
			}

			bool match = false;
			try
			{
				string passwordHash = "";
				using (StreamReader sr = File.OpenText(_passwordFilePath))
				{
					passwordHash = sr.ReadToEnd();
				}

				/* Extract the bytes */
				byte[] hashBytes = Convert.FromBase64String(passwordHash);
				/* Get the salt */
				byte[] salt = new byte[16];
				Array.Copy(hashBytes, 0, salt, 0, 16);
				/* Compute the hash on the password the user entered */
				var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000);
				byte[] hash = pbkdf2.GetBytes(20);
				/* Compare the results */
				for (int i = 0; i < 20; i++)
				{
					if (hashBytes[i + 16] != hash[i])
					{
						return match;
					}
				}
				match = true;
			}
			catch (Exception)
			{
				throw;
			}

			return match;
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

		public RelayCommand GenerateKeyCommand
		{
			get
			{
				return _generateKeyCommand
					?? (_generateKeyCommand = new RelayCommand(ExecuteGenerateKeyCommand));
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
