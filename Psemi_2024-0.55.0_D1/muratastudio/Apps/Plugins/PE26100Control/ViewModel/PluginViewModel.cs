#region Using 

using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using HardwareInterfaces;
using PluginFramework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.ComponentModel;
using GalaSoft.MvvmLight.Command;
using System.Diagnostics;
using System.Xml.Linq;
using PE26100Control.UIControls;
using System.Windows.Data;
using System.Windows;
using PE26100Control.Converters;
using System.Windows.Media;
using System.IO;
using System.Xml.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using Microsoft.Win32;
using DeviceAccess;
using System.Threading;
using System.Collections;

#endregion

namespace PE26100Control.ViewModel
{
    public class PluginViewModel : DependencyObject, ICleanup, INotifyPropertyChanged
    {
        #region Private Members

        MultiRegisterStringValueConverter mc = new MultiRegisterStringValueConverter();
        private TextBlock txtOutputDisplay;
        private RelayCommand _cancelScriptCommand;
        private CancellationTokenSource _cancelSource;
        private string propertyName;
        TextBox txtboxDisplay = new TextBox();
        TextBox txtbox1Display = new TextBox();
        private RelayCommand<object> _writeRegisterCommand;
        private RelayCommand<Register> _writeCommand;
        private RelayCommand<Register> _readCommand;
        private RelayCommand _programCommand;
        private RelayCommand _unlockCommand;
        public IRegister _register;

        public List<Register> _allRegisters { get; private set; }

        private IDevice _device;
        private ObservableCollection<Register> _registers1;
        private ObservableCollection<Register> _registers2;
        private ObservableCollection<Register> _internalRegisters;
        private bool _isInternalMode;
        private RelayCommand<Register.Bit> _toggleBitCommand;
        private RelayCommand _loadRegisterCommand;
        private RelayCommand _saveRegisterCommand;
        private bool _isEditing = false;
        private OpenFileDialog ofd = new OpenFileDialog();
        private SaveFileDialog sfd = new SaveFileDialog();
        private List<CheckBox> ledCheckBoxes = new List<CheckBox>();
        private CustomizeBase _deviceBase;

        private bool _isInputCurrentSenseEnabled = false;

        internal bool _loaded;
        private readonly int _labelWidth = 200;
        private readonly int _listComboBoxWidth = 94;
        private bool _inputResEnabled;
        private bool _outputResEnabled;
        #endregion

        #region Properties

        string[] dynamicRegisters = { "IIN_UC", "IIN_OC", "IIN_UC_WARN", "IIN_OC_WARN", "IIN_MAX", "IOUT_MAX", "IOUT_OC", "IOUT_OC_WARN" };
        private int comboBoxRangeLimit;
        private readonly double bsize = 8;
        private int indexVal;
        private int comboBoxMaxLimit;
        private int comboBoxMinLimit;

        private List<FrameworkElement> _namedElements { get; set; }
        public ObservableCollection<FrameworkElement> ControlControls { get; set; }
        public ObservableCollection<FrameworkElement> TeleControls { get; set; }
        public ObservableCollection<FrameworkElement> Status1Controls { get; set; }
        public ObservableCollection<FrameworkElement> Status2Controls { get; set; }
        public ObservableCollection<FrameworkElement> Status3Controls { get; set; }
        public ObservableCollection<FrameworkElement> Status4Controls { get; set; }
        public ObservableCollection<FrameworkElement> Threshold1Controls { get; set; }
        public ObservableCollection<FrameworkElement> Threshold2Controls { get; set; }
        public ObservableCollection<FrameworkElement> Threshold3Controls { get; set; }
        public ObservableCollection<FrameworkElement> Threshold4Controls { get; set; }
        public ObservableCollection<FrameworkElement> WatchdogControls { get; set; }
        public ObservableCollection<FrameworkElement> TempControls { get; set; }
        public bool IsInputCurrentSenseEnabled
        {
            get
            {
                return _isInputCurrentSenseEnabled;
            }

            set
            {
                if (_isInputCurrentSenseEnabled == value)
                {
                    return;
                }

                var oldValue = _isInputCurrentSenseEnabled;
                _isInputCurrentSenseEnabled = value;
                RaisePropertyChanged("IsInputCurrentSenseEnabled");
            }
        }

        public static readonly DependencyProperty InputResProperty =
            DependencyProperty.Register(name: "InputResValue", propertyType: typeof(decimal), ownerType: typeof(PluginViewModel));
        public static readonly DependencyProperty OutputResProperty =
            DependencyProperty.Register(name: "OutputResValue", propertyType: typeof(decimal), ownerType: typeof(PluginViewModel));

        public event PropertyChangedEventHandler PropertyChanged;

        public decimal OutputResValue
        {

            get => (decimal)GetValue(OutputResProperty);


            set => SetValue(OutputResProperty, value);
        }
        public decimal InputResValue
        {
            get => (decimal)GetValue(InputResProperty);


            set => SetValue(InputResProperty, value);
        }
        public bool InputResEnabled
        {
            get
            {
                return _inputResEnabled;
            }

            set
            {
                if (_inputResEnabled == value)
                {
                    return;
                }

            }
        }
        public bool OutputResEnabled
        {
            get
            {
                return _outputResEnabled;
            }

            set
            {
                if (_outputResEnabled == value)
                {
                    return;
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
                RaisePropertyChanged("IsInternal");
            }
        }
        #endregion

        #region Constructor

        PluginViewModel()
        {

        }
        public PluginViewModel(object device, bool isInternalMode)
        {
            _device = device as IDevice;
            _register = device as IRegister;
            _allRegisters = (device as Device).AllRegisters;
            _registers1 = new ObservableCollection<Register>();
            _registers2 = new ObservableCollection<Register>();
            _internalRegisters = new ObservableCollection<Register>();
            IsInternal = isInternalMode;

            ControlControls = new ObservableCollection<FrameworkElement>();
            TeleControls = new ObservableCollection<FrameworkElement>();
            Status1Controls = new ObservableCollection<FrameworkElement>();
            Status2Controls = new ObservableCollection<FrameworkElement>();
            Status3Controls = new ObservableCollection<FrameworkElement>();
            Status4Controls = new ObservableCollection<FrameworkElement>();
            Threshold1Controls = new ObservableCollection<FrameworkElement>();
            Threshold2Controls = new ObservableCollection<FrameworkElement>();
            Threshold3Controls = new ObservableCollection<FrameworkElement>();
            Threshold4Controls = new ObservableCollection<FrameworkElement>();
            WatchdogControls = new ObservableCollection<FrameworkElement>();
            TempControls = new ObservableCollection<FrameworkElement>();
            _namedElements = new List<FrameworkElement>();

            if (Properties.Settings.Default.InputResValue_mOhms == 0)
            {
                Properties.Settings.Default.InputResValue_mOhms = 4;
                Properties.Settings.Default.Save();
            }

            if (Properties.Settings.Default.OutputResValue_mOhms == 0)
            {
                Properties.Settings.Default.OutputResValue_mOhms = 4;
                Properties.Settings.Default.Save();
            }

            _inputResEnabled = Properties.Settings.Default.InputResEnabled;
            _outputResEnabled = Properties.Settings.Default.OutputResEnabled;

            //_outputResEnabled = true;
                 
            if (InputResValue == 0 && OutputResValue == 0)
            {
                InputResValue = Properties.Settings.Default.InputResValue_mOhms;
                OutputResValue = Properties.Settings.Default.OutputResValue_mOhms;
                NewInputResValue = Properties.Settings.Default.InputResValue_mOhms;
                NewOutputResValue = Properties.Settings.Default.OutputResValue_mOhms;
                mc.InputResValue = NewInputResValue;
                mc.OutputResValue = NewOutputResValue;

            }

            // Create UI
            LoadUiElements(_device.UiElements);

            // Load slave devices
            var dev = device as Device;


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

        public decimal NewOutputResValue { get; private set; }
        public decimal NewInputResValue { get; private set; }


        #endregion

        #region RaisePropertyChanged Function 
        /// 
        /// </summary>
        /// <param name="propertyName"></param>
        private void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));

        }
        #endregion

        #region Public Methods
        public void Cleanup()
        {
            CancelScriptCommand.Execute(new object());
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
            var panels = elements.Descendants("Panel");

            // Get individual panel references
            var Control1Panel = panels.Where(p => p.Attribute("Name").Value == "CONTROL");
            var Status1Panel = panels.Where(p => p.Attribute("Name").Value == "STATUS1");
            var Status2Panel = panels.Where(p => p.Attribute("Name").Value == "STATUS2");
            var Status3Panel = panels.Where(p => p.Attribute("Name").Value == "STATUS3");
            var Status4Panel = panels.Where(p => p.Attribute("Name").Value == "STATUS4");
            var TelePanel = panels.Where(p => p.Attribute("Name").Value == "TELEMETRY");
            var Threshold1Panel = panels.Where(p => p.Attribute("Name").Value == "Threshold1");
            var Threshold2Panel = panels.Where(p => p.Attribute("Name").Value == "Threshold2");
            var Threshold3Panel = panels.Where(p => p.Attribute("Name").Value == "Threshold3");
            var Threshold4Panel = panels.Where(p => p.Attribute("Name").Value == "Threshold4");
            var WatchdogPanel = panels.Where(p => p.Attribute("Name").Value == "WATCHDOG");
            var TempPanel = panels.Where(p => p.Attribute("Name").Value == "SYTSEM TEMPERATURE");

            // Create the Control 1 Panel
            ParseElements(Control1Panel, ControlControls);
            ParseElements(TelePanel, TeleControls);
            ParseElements(Threshold1Panel, Threshold1Controls);
            ParseElements(Threshold2Panel, Threshold2Controls);
            ParseElements(Threshold3Panel, Threshold3Controls);
            ParseElements(Threshold4Panel, Threshold4Controls);
            ParseElements(WatchdogPanel, WatchdogControls);
            ParseElements(TempPanel, TempControls);
            // Create the Status Panel
            ParseElements(Status1Panel, Status1Controls);
            ParseElements(Status2Panel, Status2Controls);
            ParseElements(Status3Panel, Status3Controls);
            ParseElements(Status4Panel, Status4Controls);

            var resourceDictionary = new ResourceDictionary()
            {
                Source = new Uri("/PE26100Control;component/Resources/Styles.xaml", UriKind.Relative)
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
                string label = string.Empty;
                string map = string.Empty;
                string description = string.Empty;
                string toolTipShort = string.Empty;
                string toolTipLong = string.Empty;
                int mask = 0;
                string name = string.Empty;
                string range = string.Empty;
                string minrange = string.Empty;

                name = (item.Attribute("Name") != null)
                    ? item.Attribute("Name").Value : string.Empty;

                description = (item.Attribute("Description") != null)
                    ? item.Attribute("Description").Value : string.Empty;

                label = (item.Attribute("Label") != null)
                    ? item.Attribute("Label").Value : string.Empty;

                map = (item.Attribute("Map") != null)
                    ? item.Attribute("Map").Value : string.Empty;

                range = (item.Attribute("MaxRange") != null)
                    ? item.Attribute("MaxRange").Value : string.Empty;

                minrange = (item.Attribute("MinRange") != null)
                    ? item.Attribute("MinRange").Value : string.Empty;

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
                                    MaximumRange = range.ToString(),
                                    MinimumRange = minrange.ToString(),
                                    Description = description
                                };

                                foreach (var option in item.Elements())
                                {
                                    string optionLabel = option.Attribute("Label").Value;
                                    string optionValue = option.Attribute("Value").Value;
                                    //string optionValue = "None";
                                    objects.Add(new Option { Label = optionLabel, Value = optionValue });
                                }
                                if (listObject.Label == "System Temperature limit for current throttling" || listObject.Label == "System Temperature reporting")
                                {
                                    listLabel.Width = _labelWidth;
                                }
                                else
                                {
                                    listLabel.Width = _labelWidth - 70;
                                }

                                listLabel.Content = label;
                                listStackPanel.Children.Add(listLabel);

                                listComboBox.Name = name;
                                listComboBox.HorizontalAlignment = HorizontalAlignment.Right;
                                listComboBox.VerticalAlignment = VerticalAlignment.Center;
                                listComboBox.ItemsSource = objects;
                                listComboBox.DisplayMemberPath = "Label";
                                listComboBox.SelectedValuePath = "Value";
                                listComboBox.Width = _listComboBoxWidth;
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
                                int pos = Array.IndexOf(dynamicRegisters, map);
                                if (pos > -1 && name == "") listStackPanel.Visibility = Visibility.Collapsed;
                                if (name != null && name == "Vinres1") listStackPanel.Visibility = Visibility.Collapsed;

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
                        bitStatus.FontSize = bsize;
                        bitStatus.Width = 100;

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
            txtOutputDisplay = new TextBlock();

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
                label.Width = 60;
                stackPanel.Children.Add(label);

                Border border = new Border();
                border.CornerRadius = new CornerRadius(4);
                border.BorderThickness = new Thickness(1d);
                border.BorderBrush = Brushes.LightGray;
                border.Background = Brushes.WhiteSmoke;

                DockPanel dockPanel = new DockPanel();
                dockPanel.VerticalAlignment = VerticalAlignment.Center;

                txtOutputDisplay.Name = name;
                txtOutputDisplay.Text = "0";
                txtOutputDisplay.Width = 70;
                txtOutputDisplay.TextAlignment = TextAlignment.Center;
                txtOutputDisplay.ToolTip = "ADC Converted Value";
                dockPanel.Children.Add(txtOutputDisplay);
                txtboxDisplay.Text = mc.InputResValue.ToString();
                txtbox1Display.Text = mc.OutputResValue.ToString();

                var multi1 = new MultiBinding();
                multi1.Converter = mc;
                multi1.ConverterParameter = string.Format("{0}|{1}|{2}", formula, format, unit);
                multi1.UpdateSourceTrigger = UpdateSourceTrigger.Explicit;
                multi1.Bindings.Add(new Binding()
                {
                    Source = txtRegisterDisplay,
                    Path = new PropertyPath("Text")
                });
                multi1.Bindings.Add(new Binding()
                {
                    Source = txtboxDisplay,
                    Path = new PropertyPath("Text")
                });
                multi1.Bindings.Add(new Binding()
                {
                    Source = txtbox1Display,
                    Path = new PropertyPath("Text")
                });
                txtOutputDisplay.SetBinding(TextBlock.TextProperty, multi1);
                Register ledLsb;
                Register ledMsb;
                if (name == "devmode" && IsInternal)
                {
                    ledLsb = _allRegisters.Find(r => r.Name == maps[0]);
                    ledMsb = _allRegisters.Find(r => r.Name == maps[1]);
                }
                else
                {
                    ledLsb = _register.GetRegister(maps[0].Replace("_", ""));
                    ledMsb = _register.GetRegister(maps[1].Replace("_", ""));
                }
                string toolTipText;
                if (ledLsb != null || ledMsb != null)
                {
                    string toolTip = (ledLsb != ledMsb)
                        ? "Registers " + ledLsb.Name + " | " + ledMsb.Name
                        : "Register " + ledLsb.Name;
                    toolTipText = string.Format("{0}", toolTip);
                }
                else
                {
                    toolTipText = "";

                }

                txtRegisterDisplay.Name = name;
                txtRegisterDisplay.Tag = masks;
                txtRegisterDisplay.Width = 50;
                txtRegisterDisplay.Margin = new Thickness(2, 0, 0, 0);
                txtRegisterDisplay.TextAlignment = TextAlignment.Center;
                txtRegisterDisplay.ToolTip = toolTipText;
                dockPanel.Children.Add(txtRegisterDisplay);
                if (ledLsb != null || ledMsb != null)
                {
                    var multi = new MultiBinding();
                    multi.Converter = mc;
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
                }

                DockPanel.SetDock(txtRegisterDisplay, Dock.Left);
                DockPanel.SetDock(txtOutputDisplay, Dock.Left);
                border.Child = dockPanel;
                stackPanel.Children.Add(border);
                if (name == "devmode" && !IsInternal)
                {
                    stackPanel.Visibility = Visibility.Collapsed;
                }
                if (name != null && name == "Vinres1")
                {
                    stackPanel.Visibility = Visibility.Collapsed;
                }
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
                    {
                        ComboValueSelection();
                        CheckTempSelection(bs, lo);
                        if (lo.Map == "MODE" && lo.Mask == "8")
                        {
                            TeleControls[1].Visibility = TeleControls[1].Visibility == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;
                            TeleControls[2].Visibility = TeleControls[2].Visibility == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;
                            IsInputCurrentSenseEnabled = !IsInputCurrentSenseEnabled;

                            if (!IsInputCurrentSenseEnabled)
                                SenseRegValueChanged("txtInput", "5");
                            else
                            {
                                SenseRegValueChanged("txtInput", InputResValue.ToString());
                            }


                        }
                        if (lo.Map == "MODE" && lo.Mask == "6")
                        {
                            IEnumerable<Register> tmp = null;
                            tmp = ((DeviceAccess.Device)_device).AllRegisters.Where(S => S.Name == "MODE");
                            string st = Convert.ToString((int)((byte[])tmp.ToArray()[0].LastReadRawValue)[0], 2);
                            if (st.Length < 3) st = "00" + st;
                            if (st.Substring(st.Length - 3, 2) == "00")
                            {
                                ControlControls[11].IsEnabled = true;
                                ControlControls[11].Visibility = Visibility.Visible;
                                ControlControls[12].Visibility = Visibility.Collapsed;
                                ControlControls[13].IsEnabled = true;
                                ControlControls[14].IsEnabled = true;
                                ControlControls[15].IsEnabled = true;
                            }
                            else if (st.Substring(st.Length - 3, 2) == "10" || st.Substring(st.Length - 3, 2) == "11")
                            {
                                ControlControls[11].Visibility = Visibility.Collapsed;
                                ControlControls[12].IsEnabled = true;
                                ControlControls[12].Visibility = Visibility.Visible;
                                ControlControls[13].IsEnabled = true;
                                ControlControls[14].IsEnabled = true;
                                ControlControls[15].IsEnabled = true;
                                ControlControls[13].IsEnabled = false;
                            }
                            else if (st.Substring(st.Length - 3, 2) == "01")
                            {
                                ControlControls[11].Visibility = Visibility.Collapsed;
                                ControlControls[11].IsEnabled = false;
                                ControlControls[12].IsEnabled = false;
                                ControlControls[12].Visibility = Visibility.Visible;
                                ControlControls[13].IsEnabled = false;
                                ControlControls[14].IsEnabled = false;
                                ControlControls[15].IsEnabled = false;
                            }
                        }

                        CheckComboRangevalues(bs, lo);

                        return;
                    }
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
                _register.ReadRegisterValue(reg);

                if (_deviceBase != null)
                {
                    _deviceBase.CheckDevice(null);
                }
                ComboValueSelection();

                if (lo.Map == "MODE" && lo.Mask == "8")
                {
                    TeleControls[1].Visibility = TeleControls[1].Visibility == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;
                    TeleControls[2].Visibility = TeleControls[2].Visibility == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;
                    IsInputCurrentSenseEnabled = !IsInputCurrentSenseEnabled;

                    if (!IsInputCurrentSenseEnabled)
                        SenseRegValueChanged("txtInput", "5");
                    else
                    {
                        SenseRegValueChanged("txtInput", InputResValue.ToString());
                    }


                }
                if (lo.Map == "MODE" && lo.Mask == "6")
                {
                    IEnumerable<Register> tmp = null;
                    tmp = ((DeviceAccess.Device)_device).AllRegisters.Where(S => S.Name == "MODE");
                    string st = Convert.ToString((int)((byte[])tmp.ToArray()[0].LastReadRawValue)[0], 2);
                    if (st.Length < 3) st = "00" + st;

                    if (st.Substring(st.Length - 3, 2) == "00")
                    {
                        ControlControls[11].IsEnabled = true;
                        ControlControls[11].Visibility = Visibility.Visible;
                        ControlControls[12].Visibility = Visibility.Collapsed;
                        ControlControls[13].IsEnabled = true;
                        ControlControls[14].IsEnabled = true;
                        ControlControls[15].IsEnabled = true;
                    }
                    else if (st.Substring(st.Length - 3, 2) == "10" || st.Substring(st.Length - 3, 2) == "11")
                    {
                        ControlControls[11].Visibility = Visibility.Collapsed;
                        ControlControls[12].IsEnabled = true;
                        ControlControls[12].Visibility = Visibility.Visible;
                        ControlControls[13].IsEnabled = true;
                        ControlControls[14].IsEnabled = true;
                        ControlControls[15].IsEnabled = true;
                        ControlControls[13].IsEnabled = false;
                    }
                    else if (st.Substring(st.Length - 3, 2) == "01")
                    {
                        ControlControls[11].Visibility = Visibility.Collapsed;
                        ControlControls[11].IsEnabled = false;
                        ControlControls[12].IsEnabled = false;
                        ControlControls[12].Visibility = Visibility.Visible;
                        ControlControls[13].IsEnabled = false;
                        ControlControls[14].IsEnabled = false;
                        ControlControls[15].IsEnabled = false;
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
        private void CheckComboRangevalues(BitSelection bs, ListObject lo)
        {
            #region 
            // Setting range limit for comboboxes which mapped to registers VIN,VOUT,VBATT,EXT1,EXT2 
            // When IC is disabled, Refresh click changes lastreadvalue to FF where combobox value doesn't fill and shows blank.
            // Setting default index of comoboxbox, if corresponding register value is greater than limit
            #endregion

            if (lo.Map.Contains("VIN") || lo.Map.Contains("VOUT") || lo.Map.Contains("VBATT") || lo.Map.Contains("EXT"))
            {
                comboBoxRangeLimit = int.Parse(lo.MaximumRange);
                indexVal = int.Parse(lo.Mask);
            }
            switch (lo.Map)
            {
                case "VIN_UV":
                    if (((DeviceAccess.Device)_device).AllRegisters.FirstOrDefault(S => S.Name == "VIN_UV").LastReadValue >= comboBoxRangeLimit)
                    {
                        bs.SelectedIndex = indexVal;
                        if(bs.SelectedIndex == -1)
                        {
                            bs.SelectedValue = indexVal;
                        }
                    }
                    break;
                case "VIN_OV":
                    if (((DeviceAccess.Device)_device).AllRegisters.FirstOrDefault(S => S.Name == "VIN_OV").LastReadValue >= comboBoxRangeLimit)
                    {
                        bs.SelectedIndex = indexVal;
                        if (bs.SelectedIndex == -1)
                        {
                            bs.SelectedValue = indexVal;
                        }
                    }
                    break;
                case "VBATT_UV":
                    if (((DeviceAccess.Device)_device).AllRegisters.FirstOrDefault(S => S.Name == "VBATT_UV").LastReadValue >= comboBoxRangeLimit)
                    {
                        bs.SelectedIndex = indexVal;
                        if (bs.SelectedIndex == -1)
                        {
                            bs.SelectedValue = indexVal;
                        }
                    }
                    break;
                case "VBATT_OV":
                    if (((DeviceAccess.Device)_device).AllRegisters.FirstOrDefault(S => S.Name == "VBATT_OV").LastReadValue >= comboBoxRangeLimit)
                    {
                        bs.SelectedIndex = indexVal;
                        if (bs.SelectedIndex == -1)
                        {
                            bs.SelectedValue = indexVal;
                        }
                    }
                    break;
                case "EXT1_OV":
                    if (((DeviceAccess.Device)_device).AllRegisters.FirstOrDefault(S => S.Name == "EXT1_OV").LastReadValue >= comboBoxRangeLimit)
                    {
                        bs.SelectedIndex = indexVal;
                        if (bs.SelectedIndex == -1)
                        {
                            bs.SelectedValue = indexVal;
                        }
                    }
                    break;
                case "EXT2_OV":
                    if (((DeviceAccess.Device)_device).AllRegisters.FirstOrDefault(S => S.Name == "EXT2_OV").LastReadValue >= comboBoxRangeLimit)
                    {
                        bs.SelectedIndex = indexVal;
                        if (bs.SelectedIndex == -1)
                        {
                            bs.SelectedValue = indexVal;
                        }
                    }
                    break;
                case "VOUT_UV":
                    if (((DeviceAccess.Device)_device).AllRegisters.FirstOrDefault(S => S.Name == "VOUT_UV").LastReadValue >= comboBoxRangeLimit)
                    {
                        bs.SelectedIndex = indexVal;
                        if (bs.SelectedIndex == -1)
                        {
                            bs.SelectedValue = indexVal;
                        }
                    }
                    break;
                case "VOUT_OV":
                    if (((DeviceAccess.Device)_device).AllRegisters.FirstOrDefault(S => S.Name == "VOUT_OV").LastReadValue >= comboBoxRangeLimit)
                    {
                        bs.SelectedIndex = indexVal;
                        if (bs.SelectedIndex == -1)
                        {
                            bs.SelectedValue = indexVal;
                        }
                    }
                    break;
                case "VIN_UV_WARN":
                    if (((DeviceAccess.Device)_device).AllRegisters.FirstOrDefault(S => S.Name == "VIN_UV_WARN").LastReadValue >= comboBoxRangeLimit)
                    {
                        bs.SelectedIndex = indexVal;
                        if (bs.SelectedIndex == -1)
                        {
                            bs.SelectedValue = indexVal;
                        }
                    }
                    break;
                case "VIN_OV_WARN":
                    if (((DeviceAccess.Device)_device).AllRegisters.FirstOrDefault(S => S.Name == "VIN_OV_WARN").LastReadValue >= comboBoxRangeLimit)
                    {
                        bs.SelectedIndex = indexVal;
                        if (bs.SelectedIndex == -1)
                        {
                            bs.SelectedValue = indexVal;
                        }
                    }
                    break;
                case "EXT1_OV_WARN":
                    if (((DeviceAccess.Device)_device).AllRegisters.FirstOrDefault(S => S.Name == "EXT1_OV_WARN").LastReadValue >= comboBoxRangeLimit)
                    {
                        bs.SelectedIndex = indexVal;
                        if (bs.SelectedIndex == -1)
                        {
                            bs.SelectedValue = indexVal;
                        }
                    }
                    break;
                case "EXT2_OV_WARN":
                    if (((DeviceAccess.Device)_device).AllRegisters.FirstOrDefault(S => S.Name == "EXT2_OV_WARN").LastReadValue >= comboBoxRangeLimit)
                    {
                        bs.SelectedIndex = indexVal;
                        if (bs.SelectedIndex == -1)
                        {
                            bs.SelectedValue = indexVal;
                        }
                    }
                    break;
                case "VBATT_UV_WARN":
                    if (((DeviceAccess.Device)_device).AllRegisters.FirstOrDefault(S => S.Name == "VBATT_UV_WARN").LastReadValue >= comboBoxRangeLimit)
                    {
                        bs.SelectedIndex = indexVal;
                        if (bs.SelectedIndex == -1)
                        {
                            bs.SelectedValue = indexVal;
                        }
                    }
                    break;
                case "VBATT_OV_WARN":
                    if (((DeviceAccess.Device)_device).AllRegisters.FirstOrDefault(S => S.Name == "VBATT_OV_WARN").LastReadValue >= comboBoxRangeLimit)
                    {
                        bs.SelectedIndex = indexVal;
                        if (bs.SelectedIndex == -1)
                        {
                            bs.SelectedValue = indexVal;
                        }
                    }
                    break;
                case "VOUT_UV_WARN":
                    if (((DeviceAccess.Device)_device).AllRegisters.FirstOrDefault(S => S.Name == "VOUT_UV_WARN").LastReadValue >= comboBoxRangeLimit)
                    {
                        bs.SelectedIndex = indexVal;
                        if (bs.SelectedIndex == -1)
                        {
                            bs.SelectedValue = indexVal;
                        }
                    }
                    break;
                case "VOUT_OV_WARN":
                    if (((DeviceAccess.Device)_device).AllRegisters.FirstOrDefault(S => S.Name == "VOUT_OV_WARN").LastReadValue >= comboBoxRangeLimit)
                    {
                        bs.SelectedIndex = indexVal;
                        if (bs.SelectedIndex == -1)
                        {
                            bs.SelectedValue = indexVal;
                        }
                    }
                    break;
                default: break;
            }
        }
        private void CheckTempSelection(BitSelection bs, ListObject lo)
        {
            if (lo.Map.Contains("TEMP_OT"))
            {
                comboBoxMaxLimit = int.Parse(lo.MaximumRange);
                comboBoxMinLimit = int.Parse(lo.MinimumRange);
            }
            switch (lo.Map)
            {
                case "TEMP_OT":
                    if ((bs.RegisterSource.LastReadValue > comboBoxMinLimit) && (bs.RegisterSource.LastReadValue < comboBoxMaxLimit))
                    {
                        ((DeviceAccess.Device)_device).AllRegisters.FirstOrDefault(S => S.Name == "TEMP_OT").LastReadValue = comboBoxMaxLimit - 1;
                        bs.SelectedIndex = 0;
                    }
                    break;
                case "TEMP_OT_WARN":
                    if ((bs.RegisterSource.LastReadValue > comboBoxMinLimit) && (bs.RegisterSource.LastReadValue < comboBoxMaxLimit))

                    {
                        ((DeviceAccess.Device)_device).AllRegisters.FirstOrDefault(S => S.Name == "TEMP_OT_WARN").LastReadValue = comboBoxMaxLimit - 1;
                        bs.SelectedIndex = 0;
                    }
                    break;
                default: break;
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
        public void SenseRegValueChanged(string txtName, string val)
        {
            try
            {
                if (val == "" || (decimal)Convert.ToDouble(val) < 0)
                {
                    return;
                }

                int count = 0;

                var comboBoxItemSource = new List<Object>();

                double divValue = 0.0001;

                foreach (var item in _namedElements)
                {
                    if (item is BitSelection)
                    {
                        var bs = item as BitSelection;
                        ListObject lo = bs.Tag as ListObject;

                        if (lo.Map == "IIN_MAX" && txtName == "txtInput")
                        {
                            divValue = 0.0002;
                            count = int.Parse(lo.MaximumRange);
                        }
                        else if (lo.Map == "IOUT_MAX" && txtName == "txtOutput")
                        {
                            count = int.Parse(lo.MaximumRange);
                        }
                    }
                }

                for (int i = 0; i <= count + 1; i++)
                {
                    string sufix = "A";

                    string optionLabel = "";

                    if (comboBoxItemSource.Count > 0 && i <= count)
                    {
                        string prevValueLabel = ((Option)comboBoxItemSource[comboBoxItemSource.Count - 1]).Label;

                        double prevVal = double.Parse(prevValueLabel.Replace("A", ""), System.Globalization.CultureInfo.InvariantCulture);

                        decimal currentVal = (decimal)(prevVal + (divValue / double.Parse(val)) / 0.001);

                        string comboItems = decimal.Parse(currentVal.ToString(), System.Globalization.CultureInfo.InvariantCulture).ToString();

                        if (comboItems.Contains(".") && comboItems.Split('.')[1].Length > 3)
                        {
                            decimal cielValue = Math.Round(decimal.Parse(comboItems.Split('.')[1].Substring(2, 2)) / 10);
                            comboItems = comboItems.Split('.')[0] + "." + comboItems.Split('.')[1].Substring(0, 2) + cielValue;

                        }

                        optionLabel = comboItems;
                    }
                    else if (i > count)
                    {
                        //Adding NA entry to the last of items
                        optionLabel = "N";
                        i = 255;
                    }
                    else
                    {
                        optionLabel = "0";
                    }

                    comboBoxItemSource.Add(new Option { Label = optionLabel + sufix, Value = i.ToString() });
                }

                if (txtName == "txtInput")
                {
                    if (InputResEnabled)
                    {
                        InputResValue = (decimal)Convert.ToDouble(val);
                    }

                    NewInputResValue = (decimal)Convert.ToDouble(val);

                    //Registers - {"IIN_UC","IIN_OC","IIN_UC_WARN","IIN_OC_WARN","IIN_MAX"}

                    ((ItemsControl)((((System.Windows.Controls.Panel)Threshold1Controls[2]).Children)[1])).ItemsSource = comboBoxItemSource;
                    ((ItemsControl)((((System.Windows.Controls.Panel)Threshold1Controls[4]).Children)[1])).ItemsSource = comboBoxItemSource;
                    ((ItemsControl)((((System.Windows.Controls.Panel)Threshold1Controls[17]).Children)[1])).ItemsSource = comboBoxItemSource;
                    ((ItemsControl)((((System.Windows.Controls.Panel)Threshold1Controls[19]).Children)[1])).ItemsSource = comboBoxItemSource;
                    ((ItemsControl)((((System.Windows.Controls.Panel)ControlControls[17]).Children)[1])).ItemsSource = comboBoxItemSource;
                }
                else
                {
                    if (OutputResEnabled)
                    {
                        OutputResValue = (decimal)Convert.ToDouble(val);
                    }

                    NewOutputResValue = (decimal)Convert.ToDouble(val);
                    ((ItemsControl)((((System.Windows.Controls.Panel)Threshold1Controls[6]).Children)[1])).ItemsSource = comboBoxItemSource;
                    ((ItemsControl)((((System.Windows.Controls.Panel)Threshold1Controls[21]).Children)[1])).ItemsSource = comboBoxItemSource;
                    ((ItemsControl)((((System.Windows.Controls.Panel)ControlControls[15]).Children)[1])).ItemsSource = comboBoxItemSource;

                    //Registers - {"IOUT_MAX","IOUT_OC","IOUT_OC_WARN"};

                }
                ComboValueSelection();

                mc.InputResValue = NewInputResValue;
                mc.OutputResValue = NewOutputResValue;
                txtboxDisplay.Text = mc.InputResValue.ToString();
                txtbox1Display.Text = mc.OutputResValue.ToString();
            }
            catch (Exception e)
            {
                MessengerSend(e);
            }
        }
        private void ComboValueSelection()
        {
            try
            {
                foreach (var item in _namedElements)
                {
                    if (item is BitSelection)
                    {
                        var bs = item as BitSelection;
                        ListObject lo = bs.Tag as ListObject;

                        // When IC_Enable is disabled, Checking range as it Exceeds for IOUT and IIN to FF.
                        comboBoxRangeLimit = int.Parse(lo.MaximumRange);

                        indexVal = int.Parse(lo.Mask);

                        foreach (var reg in ((DeviceAccess.Device)_device).AllRegisters.ToList())
                        {
                            if (lo.Map == reg.DisplayName)
                            {
                                string value = ((DeviceAccess.Device)_device).AllRegisters.FirstOrDefault(S => S.Name == lo.Map).LastReadValue.ToString();
                                int lastReadValue = int.Parse(value);

                                switch (lo.Map)
                                {
                                    case "VOUT_REG":
                                        if (lastReadValue >= comboBoxRangeLimit)
                                        {
                                            bs.SelectedIndex = indexVal;

                                            if (bs.SelectedIndex == -1)
                                            {
                                                bs.SelectedValue = indexVal;
                                            }
                                        }
                                        break;
                                    case "IOUT_OC":
                                        if (lastReadValue >= comboBoxRangeLimit)
                                        {
                                            bs.SelectedValue = indexVal;
                                        }
                                        else if (lastReadValue <= comboBoxRangeLimit)
                                        {
                                            bs.SelectedIndex = (int)lastReadValue;
                                        }
                                        else
                                        {
                                            bs.SelectedIndex = 0;
                                        }
                                        break;
                                    case "IOUT_OC_WARN":
                                        if (lastReadValue >= comboBoxRangeLimit)
                                        {
                                            bs.SelectedValue = indexVal;
                                        }
                                        else if (lastReadValue <= comboBoxRangeLimit)
                                        {
                                            bs.SelectedIndex = (int)lastReadValue;
                                        }
                                        else
                                        {
                                            bs.SelectedIndex = 0;
                                        }
                                        break;
                                    case "IOUT_MAX":
                                        if (lastReadValue >= comboBoxRangeLimit)
                                        {
                                            bs.SelectedValue = indexVal;

                                        }
                                        else if (lastReadValue <= comboBoxRangeLimit)
                                        {
                                            bs.SelectedIndex = (int)lastReadValue;
                                        }
                                        else
                                        {
                                            bs.SelectedIndex = 0;
                                        }
                                        break;
                                    case "IIN_MAX":
                                        if (lastReadValue >= comboBoxRangeLimit)
                                        {
                                            bs.SelectedValue = indexVal;
                                        }
                                        else if (lastReadValue <= comboBoxRangeLimit)
                                        {
                                            bs.SelectedIndex = (int)lastReadValue;
                                        }
                                        else
                                        {
                                            bs.SelectedIndex = 0;
                                        }
                                        break;
                                    case "IIN_OC_WARN":
                                        if (lastReadValue >= comboBoxRangeLimit)
                                        {
                                            bs.SelectedValue = indexVal;
                                        }
                                        else if (lastReadValue <= comboBoxRangeLimit)
                                        {
                                            bs.SelectedIndex = (int)lastReadValue;
                                        }
                                        else
                                        {
                                            bs.SelectedIndex = 0;
                                        }
                                        break;
                                    case "IIN_UC_WARN":
                                        if (lastReadValue >= comboBoxRangeLimit)
                                        {
                                            bs.SelectedValue = indexVal;
                                        }
                                        else if (lastReadValue <= comboBoxRangeLimit)
                                        {
                                            bs.SelectedIndex = (int)lastReadValue;
                                        }
                                        else
                                        {
                                            bs.SelectedIndex = 0;
                                        }
                                        break;
                                    case "IIN_OC":
                                        if (lastReadValue >= comboBoxRangeLimit)
                                        {
                                            bs.SelectedValue = indexVal;
                                        }
                                        else if (lastReadValue <= comboBoxRangeLimit)
                                        {
                                            bs.SelectedIndex = (int)lastReadValue;
                                        }
                                        else
                                        {
                                            bs.SelectedIndex = 0;
                                        }
                                        break;
                                    case "IIN_UC":
                                        if (lastReadValue >= comboBoxRangeLimit)
                                        {
                                            bs.SelectedValue = indexVal;
                                        }
                                        else if (lastReadValue <= comboBoxRangeLimit)
                                        {
                                            bs.SelectedIndex = (int)lastReadValue;
                                        }
                                        else
                                        {
                                            bs.SelectedIndex = 0;
                                        }
                                        break;
                                    default:
                                        break;
                                }
                            }
                        }


                    }
                }

            }
            catch (Exception e)
            {
                MessengerSend(e);
            }

        }
        public void TurnOnPrivateModeRegister()
        {

            string regId = "ICENABLE";//0X00
            string hexValue = "01";
            WriteToRegister(regId, hexValue);

            regId = "I2CTOR";//0X4F
            hexValue = "01";
            WriteToRegister(regId, hexValue);

            regId = "MASTERTESTUB";//0XF6
            hexValue = "A5";
            WriteToRegister(regId, hexValue);

            regId = "MASTERTESTLB";//0XF7
            hexValue = "96";
            WriteToRegister(regId, hexValue);

            regId = "TRIMREFS2R";//0X58
            hexValue = "02";
            WriteToRegister(regId, hexValue);

            //Do coding here to verify whether MASTER_TEST_LB response is 0x01. 

            // if it is 0x96, show registers from 0x00 to 0xFE ( if(response == 0x96 && if currentmode == /demowithdev) || if(response == 0x01 && if currentmode == /devmode)
            //_register.Registers.ForEach(
            //    r =>
            //    {
            //        if (r.Private)
            //        {
            //            _internalRegisters.Add(r);
            //        }
            //    }
            //    );

            // If it is not 0x96, show registers from 0x00 to 0x37.
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
        #endregion
    }
    #region Interace and classes
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

        public string MaximumRange { get; set; }
        public string MinimumRange { get; set; }
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

    #endregion

}
