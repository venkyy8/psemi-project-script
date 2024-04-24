using DeviceAccess;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using HardwareInterfaces;
using Microsoft.Win32;
using PE24103Control.Converters;
using PE24103Control.Dialogs;
using PE24103Control.Helpers;
using PE24103Control.UIControls;
using PluginFramework;
using System;
using System.Collections;
using System.Collections.Concurrent;
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
using Serilog;

namespace PE24103Control.ViewModel
{
    public class PluginViewModel : PluginDeviceBase, ICleanup
    {

        #region enum

        public enum PollState
        {
            Off,
            On,
            Polling,
            Error
        }

        #endregion

        #region Private Members

        private Object thisLock = new Object();
        private string[] rname;
        private bool _isR2D2PMBusDevice = true;
        private IPMBus _protocol;
        private IDevice _device;
        private Device _dev;
        private IRegister _register;
        private List<Register> _allRegisters;
        private CustomizeBase _deviceBase;
        private bool _isInternalMode;
        private Visibility _isR2D2PMBus = Visibility.Visible;
        private readonly int _labelWidth = 150;
        private RelayCommand _loadRegisterCommand;
        private RelayCommand _saveRegisterCommand;
        private RelayCommand _refreshCommand;
        private RelayCommand<Register> _sendByteCommand;
        private RelayCommand _togglePollCommand;
        private bool _isEditing = false;
        private OpenFileDialog ofd = new OpenFileDialog();
        private SaveFileDialog sfd = new SaveFileDialog();
        private List<FrameworkElement> _namedElements { get; set; }

        private byte _page = 0;
        private ConcurrentDictionary<int, bool> _isPowerGoodSet;
        private List<PowerIndicatorRegisterInfo> _powerIndicatorRegistersList;
        private List<IOUTRegisterInfo> _ioutRegs;
        private PollState _pollState = PollState.Off;
        private System.Timers.Timer _pollTimer = new System.Timers.Timer();
        private bool _pollActive = false;
        private int _pageDelay = 0;
        private int _activePageDelay = 0;
        private List<int> _pages = new List<int>();
        private bool _refreshEnable = true;

        //public delegate void RefreshEventHandler(MappedRegister reg, string value);
        public event EventHandler RegRefresh;
        public event EventHandler Poll;


        #endregion

        #region Properties

        public int SelectedConfig { get; set; }

        public byte Page
        {
            get
            {
                return _page;
            }
            set
            {
                if (_page == value)
                {
                    return;
                }

                _page = value;
            }
        }

        public List<IOUTRegisterInfo> IOUTRegisters
        {
            get
            {
                return _ioutRegs;
            }
            set
            {
                if (_ioutRegs == value)
                {
                    return;
                }

                _ioutRegs = value;
            }
        }
        public List<PowerIndicatorRegisterInfo> PowerIndicatorRegistersList
        {
            get
            {
                return _powerIndicatorRegistersList;
            }
            set
            {
                if (_powerIndicatorRegistersList == value)
                {
                    return;
                }

                _powerIndicatorRegistersList = value;
            }
        }

        public List<int> Pages
        {
            get
            {
                return _pages;
            }
            set
            {
                if (_pages == value)
                {
                    return;
                }

                _pages = value;
            }
        }

        #region Global

        private int _selectedTab;
        public int SelectedTab
        {
            get { return _selectedTab; }

            set
            {
                if (_selectedTab == value)
                {
                    return;
                }

                _selectedTab = value;
                OnPropertyChanged("SelectedTab");
            }
        }

        private bool _isSelected1;
        public bool IsSelected1
        {
            get { return _isSelected1; }

            set
            {
                if (_isSelected1 == value)
                {
                    return;
                }

                _isSelected1 = value;
                OnPropertyChanged("IsSelected1");
            }
        }

        private bool _isSelected2;
        public bool IsSelected2
        {
            get { return _isSelected2; }

            set
            {
                if (_isSelected2 == value)
                {
                    return;
                }

                _isSelected2 = value;
                OnPropertyChanged("IsSelected2");
            }
        }

        private bool _isSelected3;
        public bool IsSelected3
        {
            get { return _isSelected3; }

            set
            {
                if (_isSelected3 == value)
                {
                    return;
                }

                _isSelected3 = value;
                OnPropertyChanged("IsSelected3");
            }
        }

        private bool _isSelected4;
        public bool IsSelected4
        {
            get { return _isSelected4; }

            set
            {
                if (_isSelected4 == value)
                {
                    return;
                }

                _isSelected4 = value;
                OnPropertyChanged("IsSelected4");
            }
        }

        private bool _isSelected5;
        public bool IsSelected5
        {
            get { return _isSelected5; }

            set
            {
                if (_isSelected5 == value)
                {
                    return;
                }

                _isSelected5 = value;
                OnPropertyChanged("IsSelected5");
            }
        }

        private Visibility _isConfig1;
        public Visibility IsConfig1
        {
            get { return _isConfig1; }

            set
            {
                if (_isConfig1 == value)
                {
                    return;
                }

                _isConfig1 = value;
                OnPropertyChanged("IsConfig1");
            }
        }

        private Visibility _isConfig2;
        public Visibility IsConfig2
        {
            get { return _isConfig2; }

            set
            {
                if (_isConfig2 == value)
                {
                    return;
                }

                _isConfig2 = value;
                OnPropertyChanged("IsConfig2");
            }
        }

        private Visibility _isConfig3;
        public Visibility IsConfig3
        {
            get { return _isConfig3; }

            set
            {
                if (_isConfig3 == value)
                {
                    return;
                }

                _isConfig3 = value;
                OnPropertyChanged("IsConfig3");
            }
        }

        private Visibility _isConfig4;
        public Visibility IsConfig4
        {
            get { return _isConfig4; }

            set
            {
                if (_isConfig4 == value)
                {
                    return;
                }

                _isConfig4 = value;
                OnPropertyChanged("IsConfig4");
            }
        }

        private Visibility _isConfig5;
        private Register reg;
        private Register reg1;

        public Visibility IsConfig5
        {
            get { return _isConfig5; }

            set
            {
                if (_isConfig5 == value)
                {
                    return;
                }

                _isConfig5 = value;
                OnPropertyChanged("IsConfig5");
            }
        }

        #endregion

        public bool RefreshEnable
        {
            get { return _refreshEnable; }

            set
            {
                if (_refreshEnable == value)
                {
                    return;
                }

                _refreshEnable = value;
                OnPropertyChanged("RefreshEnable");
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
                if (!_isR2D2PMBusDevice)
                {
                    return _device.AltDisplayName; ;
                }

                return _device.DisplayName;
            }
        }

        public ObservableCollection<MappedRegister> MappedRegisters
        {
            get;
            set;
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

        public Visibility IsR2D2PMBus
        {
            get { return _isR2D2PMBus; }

            set
            {
                if (_isR2D2PMBus == value)
                {
                    return;
                }

                _isR2D2PMBus = value;
                OnPropertyChanged("IsR2D2PMBus");
            }
        }

        public bool IsExternal
        {
            get { return !_isInternalMode; }
        }

        public PollState PollingState
        {
            get
            {
                return _pollState;
            }

            set
            {
                if (_pollState == value)
                {
                    return;
                }

                var oldValue = _pollState;
                _pollState = value;
                OnPropertyChanged("PollingState");
            }
        }

        public int PageDelay
        {
            get
            {
                return _pageDelay;
            }

            set
            {
                if (_pageDelay == value)
                {
                    return;
                }

                switch (value)
                {
                    case 0:
                        _activePageDelay = 0;
                        break;
                    case 1:
                        _activePageDelay = 10;
                        break;
                    case 2:
                        _activePageDelay = 20;
                        break;
                    case 3:
                        _activePageDelay = 50;
                        break;
                    case 4:
                        _activePageDelay = 100;
                        break;
                    case 5:
                        _activePageDelay = 200;
                        break;
                    case 6:
                        _activePageDelay = 300;
                        break;
                    case 7:
                        _activePageDelay = 400;
                        break;
                    default:
                        break;
                }

                _pageDelay = value;
                OnPropertyChanged("PageDelay");
            }
        }

        public ConcurrentDictionary<int, bool> IsIndicatorSet
        {
            get { return _isPowerGoodSet; }

            set
            {
                if (_isPowerGoodSet == value)
                {
                    return;
                }

                _isPowerGoodSet = value;
            }
        }

        #endregion

        #region Constructor

        public PluginViewModel(object device, bool isInternalMode, bool isPE24103 = true, bool isMYT0424 = false)
        {
            Log.Debug("PluginViewModel - pmbus  execution started. " + "Is PE24103 : " + isPE24103 + " Is MYT0424 : " + isMYT0424);

            if (isPE24103 && isMYT0424)
            {
                _isR2D2PMBusDevice = false;
            }
            _powerIndicatorRegistersList = new List<PowerIndicatorRegisterInfo>();
            _ioutRegs = new List<IOUTRegisterInfo>();
            IsIndicatorSet = new ConcurrentDictionary<int, bool>();
            Pages = new List<int>();

            ConfigurationSelector config = new ConfigurationSelector(isPE24103);

            IsR2D2 = isPE24103;

            if (isPE24103)
            {
                config.Title = "PE24103 - Configuration Selector";
            }
            else
            {
                config.Title = "PE24104 - Configuration Selector";
            }

            config.ShowDialog();

            SelectedConfig = config.Configuration;

            SelectedTab = SelectedConfig - 1;

            LoadConfiguration(SelectedConfig);

            _protocol = (device as Device).Adapter as IPMBus;
            _device = device as IDevice;
            _dev = device as Device;
            _register = device as IRegister;
            _allRegisters = (device as Device).AllRegisters;
            _isInternalMode = isInternalMode;
            // Set up the poll timer
            _pollTimer.Interval = 500;
            _pollTimer.Stop();
            _pollTimer.Elapsed += _pollTimer_Elapsed;
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

            //LoadUiElements(_device.UiElements);

            // Register for notification messages
            Messenger.Default.Register<NotificationMessage>(this, HandleNotification);
        }

        #endregion

        #region Public Methods

        public void Cleanup()
        {
            // Nothing to cleanup
        }

        public void ReadAll(bool dynamicRegistersOnly)
        {
            _page = 0;
            PollingState = PollState.Polling;
            foreach (MappedRegister mr in MappedRegisters)
            {
                mr.PageDelay = _activePageDelay;

                if (!mr.RegisterSource.ReadOnly && dynamicRegistersOnly)
                    continue;

                if (mr.RegisterSource.Access == "SendByte")
                    continue;

                try
                {
                    if (_page != mr.Page)
                    {
                        _page = (byte)mr.Page;
                        mr.Read(true);
                    }
                    else
                    {
                        mr.Read(false);
                    }

                    if (mr.RegisterSource.DisplayName == "READ_VOUT" || mr.RegisterSource.DisplayName == "POWER_GOOD_ON" || mr.RegisterSource.DisplayName == "POWER_GOOD_OFF")
                    {
                        PowerIndicatorRegisterInfo pReg = _powerIndicatorRegistersList.Where(a => (a.RegisterName == mr.RegisterSource.DisplayName && a.Page == mr.Page)).FirstOrDefault();

                        if (pReg == null)
                        {
                            _powerIndicatorRegistersList.Add(new PowerIndicatorRegisterInfo
                            {
                                Page = mr.Page,
                                RegisterName = mr.RegisterSource.DisplayName,
                                RegisterValue = mr.Value
                            });
                        }
                        else
                        {
                            _powerIndicatorRegistersList.FirstOrDefault(a => a.RegisterName == mr.RegisterSource.DisplayName && a.Page == mr.Page).RegisterValue = mr.Value;
                        }
                    }

                    // IOUTRegisters list with page and corresponding IOUT Readings

                    if (mr.RegisterSource.DisplayName == "READ_IOUT")
                    {
                        IOUTRegisterInfo iReg = IOUTRegisters.Where(a => (a.RegisterName == mr.RegisterSource.DisplayName && a.Page == mr.Page)).FirstOrDefault();

                        if (iReg == null)
                        {
                            IOUTRegisters.Add(new IOUTRegisterInfo
                            {
                                Page = mr.Page,
                                RegisterName = mr.RegisterSource.DisplayName,
                                RegisterValue = Convert.ToDouble(mr.RegisterSource.LastReadValue)
                            });

                        }
                        else
                        {
                            IOUTRegisters.FirstOrDefault(a => a.RegisterName == mr.RegisterSource.DisplayName && a.Page == mr.Page).RegisterValue = Convert.ToDouble(mr.RegisterSource.LastReadValue);
                        }
                    }

                }
                catch (Exception e)
                {
                    Log.Error("PE24103Control PluginViewModel : ReadAll failed. Register : " + mr.RegisterSource.DisplayName, e);
                    PollingState = PollState.Error;
                }

                if (_pollActive)
                    PollingState = PollState.On;
                else
                    PollingState = PollState.Off;


            }

            //checks if PE24103 device or not
            AddIOUTReadings();
            Pages = new List<int>();
            if (IsR2D2)
            {
                //setting 4 pages for all 5 Configurations in PE24103 device
                if (SelectedConfig == 1 || SelectedConfig == 2 || SelectedConfig == 3 || SelectedConfig == 4 || SelectedConfig == 5)
                {
                    Pages.Add(1);
                    Pages.Add(2);
                    Pages.Add(3);
                    Pages.Add(4);
                }
            }
            else
            {
                if (SelectedConfig == 1 || SelectedConfig == 2)
                {
                    //setting 2 pages for all 2 Configurations in PE24104 device
                    Pages.Add(1);
                    Pages.Add(2);
                }
            }

            foreach (int page in Pages)
            {
                UpdatePowerGoodIndicatorRegs(page);
            }
        }

        public void AddIOUTReadings()
        {
            if (IsR2D2)
            {

                if (SelectedConfig == 2)
                {
                    //if selected configuration is 2, Iterates through all pages
                    //when page becomes 3,IOUT(Page2)=IOUT(Page2)+IOUT(Page3) IOUTReadings  
                    for (int i = 0; i < IOUTRegisters.Count; i++)
                    {
                        if (IOUTRegisters[i].Page == 3)
                        {
                            IOUTRegisters[i - 1].RegisterValue += IOUTRegisters[i].RegisterValue;
                        }
                    }

                }
                else if (SelectedConfig == 3)
                {
                    //if selected configuration is 3, Iterates through all pages
                    //when page becomes 2,IOUT(Page1)=IOUT(Page1)+IOUT(Page2) IOUTReadings
                    //when page becomes 4,IOUT(Page3)=IOUT(Page3)+IOUT(Page4) IOUTReadings
                    for (int i = 0; i < IOUTRegisters.Count; i++)
                    {
                        if (IOUTRegisters[i].Page == 2 || IOUTRegisters[i].Page == 4)
                        {
                            IOUTRegisters[i - 1].RegisterValue += IOUTRegisters[i].RegisterValue;
                        }
                    }
                }
                else if (SelectedConfig == 4)
                {
                    //if selected configuration is 4,IOUT(Page1)=IOUT(Page1)+IOUT(Page2)+IOUT(Page3) IOUTReadings
                    IOUTRegisters[0].RegisterValue += IOUTRegisters[1].RegisterValue + IOUTRegisters[2].RegisterValue;
                }
                else if (SelectedConfig == 5)
                {
                    //if selected configuration is 5,IOUT(Page1)=IOUT(Page1)+IOUT(Page2)+IOUT(Page3)+IOUT(Page4) IOUTReadings
                    IOUTRegisters[0].RegisterValue += IOUTRegisters[1].RegisterValue + IOUTRegisters[2].RegisterValue + IOUTRegisters[3].RegisterValue;
                }

            }
            else
            {
                if (SelectedConfig == 2)
                {
                    //if selected configuration is 2, Iterates through all pages
                    //when page becomes 2,IOUT(Page1)=IOUT(Page1)+IOUT(Page2) IOUTReadings  
                    for (int i = 0; i < IOUTRegisters.Count; i++)
                    {
                        if (IOUTRegisters[i].Page == 2)
                        {
                            IOUTRegisters[i - 1].RegisterValue += IOUTRegisters[i].RegisterValue;
                        }
                    }

                }
            }

        }

        public void WriteToRegister(int selectedConfig)
        {
                string regId = "MFRSPECIFICMATRIX";
                string hexValue = "";
                string regId1 = "MFRSPECIFICPGOOD";
                string hexValue1 = "";

                if (selectedConfig == 1)
                {
                    hexValue = "0FE4";
                    hexValue1 = "0F0F";
                }
                else if (selectedConfig == 2)
                {
                    hexValue = "0FD4";
                    hexValue1 = "0B0B";
                }
                else if (selectedConfig == 3)
                {
                    hexValue = "0FA0";
                    hexValue1 = "0505";
                }
                else if (selectedConfig == 4)
                {
                    hexValue = "0FC0";
                    hexValue1 = "0909";
                }
                else if (selectedConfig == 5)
                {
                    hexValue = "0F00";
                    hexValue1 = "0101";
                }

                int decValue = int.Parse(hexValue, System.Globalization.NumberStyles.HexNumber);
                int decValue1 = int.Parse(hexValue1, System.Globalization.NumberStyles.HexNumber);

                ArrayList masterTestList = new ArrayList();
                ArrayList masterTestList1 = new ArrayList();
                masterTestList.Add(regId);
                masterTestList.Add(decValue);
                masterTestList1.Add(regId1);
                masterTestList1.Add(decValue1);

                reg = _allRegisters.Find(r => r.ID == regId);
                reg1 = _allRegisters.Find(r => r.ID == regId1);

                try
                {
                    _register.WriteRegister(regId, decValue);
                    _register.ReadRegisterValue(reg);
                    _register.WriteRegister(regId1, decValue1);
                    _register.ReadRegisterValue(reg1);
                }
                catch (Exception e)
                {
                    Log.Error("PE24103Control WriteToRegister : ReadAll failed. selectedConfig : " + selectedConfig, "Register :" + reg.DisplayName, "Data :" + reg.LastReadString + e.Message);

                    throw new Exception("Reading Value from Register (" + reg.DisplayName + ")", e);
                    Log.Error("PE24103Control WriteToRegister : ReadAll failed. selectedConfig : " + selectedConfig, "Register :" + reg1.DisplayName, "Data :" + reg1.LastReadString + e.Message);

                    throw new Exception("Reading Value from Register (" + reg1.DisplayName + ")", e);
                }
        }

        public void Write(MappedRegister reg, string value, PluginViewModel _pvm, IRegister ireg)
        {
            lock (thisLock)
            {
                reg.PageDelay = _activePageDelay;
                reg.Write(ref _page, value);
            }
        }

        public void UpdatePowerGoodIndicatorRegs(int page)
        {
            double vout = 0.0;
            double powerGoodOn = 0.0;
            double powerGoodOff = 0.0;

            List<PowerIndicatorRegisterInfo> pageRegisters = _powerIndicatorRegistersList.Where(a => a.Page == page).ToList();

            if (pageRegisters.Any())
            {
                foreach (PowerIndicatorRegisterInfo reg in pageRegisters)
                {
                    if (reg.RegisterName == "READ_VOUT")
                    {
                        double.TryParse(reg.RegisterValue.ToString(), out vout);
                    }
                    else if (reg.RegisterName == "POWER_GOOD_ON")
                    {
                        double.TryParse(reg.RegisterValue.ToString(), out powerGoodOn);
                    }
                    else if (reg.RegisterName == "POWER_GOOD_OFF")
                    {
                        double.TryParse(reg.RegisterValue.ToString(), out powerGoodOff);
                    }
                }
            }
            if (vout < powerGoodOff)
            {
                IsIndicatorSet[page] = true;
            }
            else
            {
                IsIndicatorSet[page] = false;
            }
        }

        #endregion

        #region Private Methods

        private void LoadConfiguration(int selectedConfig)
        {
            IsSelected1 = IsSelected2 = IsSelected3 = IsSelected4 = IsSelected5 = false;
            IsConfig1 = IsConfig2 = IsConfig3 = IsConfig4 = IsConfig5 = Visibility.Collapsed;

            switch (selectedConfig)
            {
                case 1:
                    IsSelected1 = true;
                    IsConfig1 = Visibility.Visible;
                    break;
                case 2:
                    IsSelected2 = true;
                    IsConfig2 = Visibility.Visible;
                    break;
                case 3:
                    IsSelected3 = true;
                    IsConfig3 = Visibility.Visible;
                    break;
                case 4:
                    IsSelected4 = true;
                    IsConfig4 = Visibility.Visible;
                    break;
                case 5:
                    IsSelected5 = true;
                    IsConfig5 = Visibility.Visible;
                    break;
                default:
                    IsSelected1 = true;
                    IsConfig1 = Visibility.Visible;
                    break;
            }
        }

        private void ExecuteSendByteCommand(Register reg)
        {
            try
            {
                var result = MessageBox.Show("Send command to device?", reg.DisplayName, MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    byte[] data = new byte[0];

                    if (reg.DisplayName == "CLEAR_FAULTS")
                    {
                        for (byte i = 1; i < 6; i++)
                        {
                            byte[] pdata = new byte[1] { i };
                            this._device.WriteBlock(0, 1, ref pdata);
                            this._device.WriteBlock(reg.Address, 0, ref data);
                        }
                    }
                    else
                    {

                        this._device.WriteBlock(reg.Address, 0, ref data);
                    }

                    Messenger.Default.Send(
                        new CommunicationMessage
                        {
                            MessageType = MessageType.Ok,
                            Sender = this,
                            TopLevel = new Exception("Command sent successfully!")
                        });
                    MessageBox.Show("Command sent successfully!", reg.DisplayName, MessageBoxButton.OK, MessageBoxImage.None);
                }
            }
            catch (Exception ex)
            {
                Log.Error("PE24103Control WriteToRegister : ExecuteSendByteCommand failed . Register :" + reg.DisplayName, "Data :" + reg.LastReadString + ex.Message);

                MessengerSend(ex);
            }
        }

        #endregion

        #region Event Handlers

        private async void _pollTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            _pollTimer.Stop();
            await Task.Run(() => ReadAll(true));
            if (Poll != null)
                Poll(this, EventArgs.Empty);
            if (_pollActive)
            {
                _pollTimer.Start();
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
                        Log.Error("Loading register map failed. " + _device.DisplayName + e.Message);

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

                Log.Debug("PE24103Control PluginViewModel : bitSelection_SelectionChanged : Register : " + reg.DisplayName, " Data : " + reg.LastReadString);

                if (_deviceBase != null)
                {
                    _deviceBase.CheckDevice(null);
                }
            }
            catch (Exception ex)
            {
                Log.Error("PE24103Control PluginViewModel : bitSelection_SelectionChanged failed ", ex);

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

        private void Refresh()
        {
            RefreshEnable = false;
            Thread t = new Thread(() =>
            {
                try
                {
                    ReadAll(false);
                    if (RegRefresh != null)
                        RegRefresh(this, EventArgs.Empty);
                }
                finally
                {
                    RefreshEnable = true;
                }
            });
            t.Start();
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

        /// <summary>
        /// Gets the TogglePollingCommand.
        /// </summary>
        public RelayCommand TogglePollingCommand
        {
            get
            {
                return _togglePollCommand
                    ?? (_togglePollCommand = new RelayCommand(
                    () =>
                    {
                        _pollActive = !_pollActive;
                        RefreshEnable = !_pollActive;

                        if (_pollActive)
                        {
                            PollingState = PollState.On;
                            _pollTimer.Start();
                        }
                        else
                        {
                            PollingState = PollState.Off;
                            _pollTimer.Stop();
                        }
                    }));
            }
        }

        /// <summary>
        /// Gets the RefreshCommand.
        /// </summary>
        public RelayCommand RefreshCommand
        {
            get
            {
                return _refreshCommand
                    ?? (_refreshCommand = new RelayCommand(Refresh));
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

        public bool IsR2D2 { get; set; }
        public double Reading { get; private set; }
        public List<Register> AllRegisters
        { get => _allRegisters; set => _allRegisters = value; }

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

    public class PowerIndicatorRegisterInfo
    {
        public string RegisterName { get; set; }

        public double RegisterValue { get; set; }

        public int Page { get; set; }

        public bool IsFaultIndicator { get; set; }
    }

    public class IOUTRegisterInfo
    {
        public string RegisterName { get; set; }

        public double RegisterValue { get; set; }

        public int Page { get; set; }
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
