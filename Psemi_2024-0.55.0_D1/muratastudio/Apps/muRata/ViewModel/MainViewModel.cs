using muRata.Helpers;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Microsoft.Practices.ServiceLocation;
using PluginFramework;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using AdapterAccess.Adapters;
using DeviceAccess;
using AdapterAccess;
using System.Collections.Generic;
using HardwareInterfaces;
using Ionic.Zip;
using System.Linq;
using System.IO;
using System.IO.Pipes;
using System.Xml.Linq;
using System.Xml.XPath;
using GalaSoft.MvvmLight.Messaging;
using System.Threading.Tasks;
using System.Timers;
using System.Diagnostics;
using Microsoft.Win32;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using System.Text;
using AdapterAccess.Protocols;
using System.Collections;
//using log4net;
using log4net.Config;
using Serilog;
using System.ComponentModel;
using Serilog.Events;

namespace muRata.ViewModel
{
    public enum PollState
    {
        Off,
        On,
        Polling,
        Error
    }

    public class MainViewModel : ViewModelBase
    {
        #region Private Members

       // public static ILog Log = LogManager.GetLogger(typeof(MainViewModel));
        public event EventHandler AllRegistersRead;
        private RelayCommand<IPlugin> _showPlugin;
        private RelayCommand _enumerateHardware;
        private RelayCommand _loadPluginsCommand;
        private RelayCommand<Window> _exitCommand;
        private RelayCommand<object> _getActivePlugins;
        private RelayCommand<bool> _refreshCommand;
        private RelayCommand _unlockCommand;
        private RelayCommand _togglePollCommand;
        private RelayCommand _clearLogCommand;
        private RelayCommand<int> _changeAdapter;
        private RelayCommand<int[]> _changeDevice;
        private RelayCommand _loadRegisterCommand;
        private RelayCommand _saveRegisterCommand;

        private AppManager _appManager = new AppManager();
        private DeviceManager _deviceManager;
        private AdapterManager _adapterManager = new AdapterManager();

        private List<Adapter> _availableAdapters;
        private List<Device> _availableDevices;

        private IAdapter _activeAdapter = null;
        private Device _activeDevice = null;
        private CustomizeBase _deviceBase;
        private ObservableCollection<Register> _registers;
        private PollState _pollState = PollState.Off;
        private Timer _pollTimer = new Timer();
        private int _pollFreq = 0;
        private bool _pollActive = false;
        private bool _isInternalMode = false;
        private bool _isSilentMode = false;
        private bool _is16BitI2CMode = false;
        private bool _isMTAMode = false;
        private bool _isDemoMode = false;
        private bool _isLogMode = false;
        private bool _isEnabled = true;
        private bool _i2cAutoSlaveAddress = false;
        private bool _isDeviceIdHidden = false;
        private volatile bool _closing = false;
        private bool _statusOnlyRead;

        private OpenFileDialog ofd = new OpenFileDialog();
        private SaveFileDialog sfd = new SaveFileDialog();

        private System.Threading.Thread _serverThread = null;

        private System.Threading.CancellationTokenSource _cancelSource;
        private NamedPipeServerStream _pipeServer = null;
        private static int _pipeBufferSize = 256;
        private const string PIPE_NAME = "studio01";

        #endregion

        #region private members

        private bool _information;
        private bool _debug;
        private bool _enabled;
        private bool _error = true;

        #endregion

        #region Public Constants

        public const string PollingFreqPropertyName = "PollingFreq";
        public const string PollingStatePropertyName = "PollingState";
        public const string ActiveAdapterPropertyName = "ActiveAdapter";
        public const string ActiveDevicePropertyName = "ActiveDevice";
        public const string IsInternalModePropertyName = "IsInternalMode";
        public const string IsSilentModePropertyName = "IsSilentMode";
        public const string IsDemoModePropertyName = "IsDemoMode";
        public const string IsEnabledPropertyName = "IsEnabled";
        private const string PollReadOnlyRegistersPropertyName = "PollReadOnlyRegisters";
        public const string Is16BitI2CModePropertyName = "Is16BitI2CMode";
        public const string IsMTAModePropertyName = "IsMTACMode";
        public const string TabVisiblePropertyName = "TabVisible";

        #endregion

        #region Public Properties

        public bool IncludeI2c = true;

        public bool IncludePmBus = true;

        public string DeviceInfoName = string.Empty;

        public List<string> DeviceInfoNameList
        {
            get;
            set;
        }

        public byte? ForcedI2cAddress = null;
        public Visibility _tabVisible = Visibility.Collapsed;


        public bool IsAllRegistersRead
        {
            get;
            set;
        }

        public bool PollReadOnlyRegisters
        {
            get
            {
                return _statusOnlyRead;
            }

            set
            {
                if (_statusOnlyRead == value)
                {
                    return;
                }

                var oldValue = _statusOnlyRead;
                _statusOnlyRead = value;
                Properties.Settings.Default.StatusOnlyRead = _statusOnlyRead;
                Properties.Settings.Default.Save();
                RaisePropertyChanged(PollReadOnlyRegistersPropertyName, oldValue, value, true);
            }
        }

        /// <summary>
        /// Sets and gets the PollingState property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// This property's value is broadcasted by the MessengerInstance when it changes.
        /// </summary>
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
                RaisePropertyChanged(PollingStatePropertyName, oldValue, value, true);
            }
        }

        public int PollingFreq
        {
            get
            {
                return _pollFreq;
            }

            set
            {
                if (_pollFreq == value)
                {
                    return;
                }

                if (value == 0)
                    _pollTimer.Interval = 500;
                if (value == 1)
                    _pollTimer.Interval = 1000;
                if (value == 2)
                    _pollTimer.Interval = 2000;
                if (value == 3)
                    _pollTimer.Interval = 5000;

                Properties.Settings.Default.PollingIntervalIndex = value;
                Properties.Settings.Default.Save();

                var oldValue = _pollFreq;
                _pollFreq = value;
                RaisePropertyChanged(PollingFreqPropertyName, oldValue, value, true);
            }
        }

        public ObservableCollection<Register> Registers
        {
            get { return _registers; }
        }

        /// <summary>
        /// Sets and gets the Device property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public Device ActiveDevice
        {
            get
            {
                return _activeDevice;
            }

            set
            {
                if (_activeDevice == value)
                {
                    return;
                }

                _activeDevice = value;
                SetIsEnabled();
                RaisePropertyChanged(ActiveDevicePropertyName);

                // Sets the customizable device based on the active device
                SetDeviceBase();
            }
        }

        /// <summary>
        /// Sets and gets the IsInternalMode property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IsInternalMode
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

                _isInternalMode = value;
                RaisePropertyChanged(IsInternalModePropertyName);
            }
        }

        public bool Is16BitI2CMode
        {
            get
            {
                return _is16BitI2CMode;
            }

            set
            {
                if (_is16BitI2CMode == value)
                {
                    return;
                }

                _is16BitI2CMode = value;
                RaisePropertyChanged(Is16BitI2CModePropertyName);
            }
        }

        public bool IsMTAMode
        {
            get
            {
                return _isMTAMode;
            }

            set
            {
                if (_isMTAMode == value)
                {
                    return;
                }

                _isMTAMode = value;
                RaisePropertyChanged(IsMTAModePropertyName);
            }
        }

        /// <summary>
        /// Sets and gets the IsSilentMode property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IsSilentMode
        {
            get
            {
                return _isSilentMode;
            }

            set
            {
                if (_isSilentMode == value)
                {
                    return;
                }

                _isSilentMode = value;
                RaisePropertyChanged(IsSilentModePropertyName);
            }
        }

        /// <summary>
        /// Sets and gets the IsDemoMode property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IsDemoMode
        {
            get
            {
                return _isDemoMode;
            }

            set
            {
                if (_isDemoMode == value)
                {
                    return;
                }

                _isDemoMode = value;
                RaisePropertyChanged(IsDemoModePropertyName);
            }
        }

        public bool IsEnabled
        {
            get
            {
                return _isEnabled;
            }

            private set
            {
                if (_isEnabled == value)
                {
                    return;
                }

                _isEnabled = value;
                RaisePropertyChanged(IsEnabledPropertyName);
            }
        }

        /// <summary>
        /// Sets and gets the ActiveAdapter property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public IAdapter ActiveAdapter
        {
            get
            {
                return _activeAdapter;
            }

            set
            {
                if (_activeAdapter == value)
                {
                    return;
                }

                _activeAdapter = value;
                SetIsEnabled();
                RaisePropertyChanged(ActiveAdapterPropertyName);
            }
        }

        public ObservableDictionary<Adapter, List<Device>> AdaptersAttachedDevices
        {
            get;
            private set;
        }

        public ObservableCollection<Adapter> ApplicationAdapters
        {
            get;
            private set;
        }

        public ObservableCollection<Adapter> Adapters
        {
            get;
            private set;
        }

        public ObservableCollection<Device> Devices
        {
            get;
            private set;
        }

        public ObservableCollection<IPlugin> AllAvailablePlugins
        {
            get;
            private set;
        }

        public ObservableCollection<IPlugin> ActivePlugins
        {
            get;
            private set;
        }

        public ObservableCollection<IMessageBase> Messages
        {
            get;
            private set;
        }

        #endregion

        #region Commands

        /// <summary>
        /// Gets the RefreshPluginsCommand.
        /// </summary>
        public RelayCommand LoadPluginsCommand
        {
            get
            {
                return _loadPluginsCommand
                    ?? (_loadPluginsCommand = new RelayCommand(
                    () =>
                    {
                        AllAvailablePlugins.Clear();

                        var host = ServiceLocator.Current.GetInstance<IPluginHost>();
                        //host.Clear();

                        var bootstrapper = ServiceLocator.Current.GetInstance<Bootstrapper>();

                        try
                        {
                            bootstrapper.LoadPlugins();
                        }
                        catch (Exception ex)
                        {
                            SplashScreenHelper.Hide();
                            MessageBox.Show(ex.Message);
                            return;
                        }

                        foreach (var plugin in bootstrapper.Plugins)
                        {
                            AllAvailablePlugins.Add(plugin);
                        }
                    }));
            }
        }

        /// <summary>
        /// Gets the EnumerateHardware.
        /// </summary>
        public RelayCommand EnumerateHardware
        {
            get
            {
                return _enumerateHardware
                    ?? (_enumerateHardware = new RelayCommand(
                    () =>
                    {
                        List<CommunicationMessage> messages = new List<CommunicationMessage>();

                        try
                        {
                            AdaptersAttachedDevices.Clear();
                            ApplicationAdapters.Clear();
                            Adapters.Clear();
                            Devices.Clear();

                            _deviceManager = new DeviceManager(_adapterManager, _isInternalMode, _appManager.DeviceInfo, _appManager.IdentityRegisters, _appManager.IdentityRegistersTwoBytes);
                            
                            if(_is16BitI2CMode)
                            {
                                _deviceManager.Is16BitAddressing = true;
                            }

                            SplashScreenHelper.ShowText("Enumerating USB Devices...");

                            Log.Information("Enumerating USB Devices...");

                            if (_isDemoMode)
                            {
                                _availableAdapters = _adapterManager.GetAllAvailableVirtualAdapters();
                            }
                            else
                            {
                                _availableAdapters = _adapterManager.GetAllAvailableAdapters(true, IncludeI2c, IncludePmBus);
                            }

                            if (_availableAdapters.Count == 0)
                            {
                                // No Adapters found
                                HandleMessageNotification(
                                    new CommunicationMessage
                                    {
                                        MessageType = MessageType.Error,
                                        Sender = this,
                                        TopLevel = new Exception("No available adapters were found.")
                                    });

                                Log.Error("No Available adapters are found!");
                                return;
                            }

                            Log.Debug("Available adapter is " +_availableAdapters[0].ToString());

                            // Get adpater plugin information
                            _adapterManager.GetPluginInfo(Properties.Settings.Default.I2cBitRate);

                            int adapterIndex = 0;


                            _availableAdapters.Reverse();

                            _availableAdapters.ForEach(a =>
                            {
                                // These are for the rest of the application
                                if (_isDemoMode)
                                {
                                    _availableDevices = _deviceManager.GetVirtualDevices(a, DeviceInfoNameList, _is16BitI2CMode, _isMTAMode);
                                }
                                else
                                {
                                    if (_i2cAutoSlaveAddress)
                                    {
                                        // Obsolete - Would need revisiting
                                        _availableDevices = _deviceManager.GetAllDevicesAutoAddress(a, DeviceInfoName, IsSilentMode, _isDeviceIdHidden);
                                    }
                                    else
                                    {
                                        // Check PMBob min FW version
                                        if (a is PMBobAdapter)
                                        {
                                            var pmbob = a as PMBobAdapter;
                                            if (!pmbob.DoesFwMeetRequirement())
                                            {
                                                messages.Add(
                                                    new CommunicationMessage
                                                    {
                                                        MessageType = MessageType.Warning,
                                                        Sender = this,
                                                        TopLevel = new Exception(string.Format("PMBob adapter {0} does not meet the minimum FW version requirement V03R01. Please update before use.", pmbob.AdapterSerialNumber))
                                                    });
                                                _availableDevices = new List<Device>();
                                            }
                                            else
                                            {
                                                _availableDevices = _deviceManager.GetAllDevices(a, ForcedI2cAddress, DeviceInfoName, IsSilentMode, _isDeviceIdHidden, IncludeI2c, IncludePmBus);
                                                
                                                if (!_availableDevices.Any())
                                                {
                                                    Log.Debug("No devices found");
                                                }
                                                else
                                                {
                                                    Log.Information("Available Device :", _availableDevices.Count);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            _availableDevices = _deviceManager.GetAllDevices(a, ForcedI2cAddress, DeviceInfoName, IsSilentMode, _isDeviceIdHidden, IncludeI2c, IncludePmBus, _isMTAMode);
                                        
                                        }
                                    }
                                }

                                IAdapter iA = a as IAdapter;
                                string adapterMessage = string.Format("Connected adapter {0} S/N {1}", iA.AdapterName, iA.AdapterSerialNumber);

                                Log.Information("Adapter : " + adapterMessage);

                                messages.Add(
                                    new CommunicationMessage
                                    {
                                        MessageType = MessageType.Ok,
                                        Sender = this,
                                        TopLevel = new Exception(adapterMessage)
                                    });

                                // Show adapter found on Splash
                                SplashScreenHelper.ShowText(adapterMessage);

                                System.Threading.Thread.Sleep(150);

                                if (_availableDevices.Count > 0)
                                {
                                    AdaptersAttachedDevices.Add(a, _availableDevices);
                                    ApplicationAdapters.Add(a);

                                    foreach (Device item in _availableDevices)
                                    {
                                        string message = (_isDemoMode)
                                            ? "Found device - Virtual " + item.DisplayName
                                            : string.Format("Found device - {0} on adapter {1} via {2}", item.DisplayName, item.Adapter.AdapterName, item.Protocol.Name);

                                        messages.Add(
                                            new CommunicationMessage
                                            {
                                                MessageType = MessageType.Ok,
                                                Sender = this,
                                                TopLevel = new Exception(message)
                                            });

                                        Log.Information("Device  : " + item.DeviceInfoName);
                                    }

                                    // This is solely for the Protocol plugin
                                    Adapters.Add(a);
                                    adapterIndex = Adapters.IndexOf(a);
                                }
                                else
                                {
                                    // No devices found
                                    var protocol = a.SupportedProtocols[0].Name;
                                    messages.Add(
                                        new CommunicationMessage
                                        {
                                            MessageType = MessageType.Warning,
                                            Sender = this,
                                            TopLevel = new Exception(string.Format("No devices were found on adapter {0} S/N {1} via {2}", iA.AdapterName, iA.AdapterSerialNumber, protocol))
                                        });

                                    Log.Error("No devices were found on adapter {0} S/N {1} via {2}", iA.AdapterName, iA.AdapterSerialNumber, protocol);


                                    // This is solely for the Protocol plugin
                                    // NOTE: I2C will always be the only available protocol since the current code will only open the adapter to a single protocol
                                    // This should fucntion as one adapter 2 protocols instead of same adapter listed twice with one protocol each.
                                    //Adapters.Add(a);
                                    //var supportedProtocol = a.SupportedProtocols.First();
                                    //if (supportedProtocol.Name == "I2C")
                                    //    new I2cProtocol(0x30, 100000, a, AdapterMode.Shared);
                                    //else
                                    //    new PMBusProtocol(0x30, 100000, a, AdapterMode.Shared);
                                    //adapterIndex = Adapters.IndexOf(a);
                                }
                            });

                            // Find the first adapter that is connected to a device.
                            // If there are none, then set the active adapter to the first.
                            if (AdaptersAttachedDevices.Count > 0)
                            {
                                ActiveAdapter = (IAdapter)AdaptersAttachedDevices.Keys.ElementAt(0);
                                AdaptersAttachedDevices.ElementAt(0).Value.ForEach(d => Devices.Add(d));
                                ActiveDevice = Devices[0];

                                if (ActiveDevice.IsParameterized)
                                {
                                    ActiveDevice.Registers.ForEach(r => _registers.Add(r));
                                }
                                else
                                {
                                    throw new Exception("Unknown device: " + DeviceInfoName);
                                    Log.Error("Unknown device: ", DeviceInfoName);
                                }
                            }
                            else
                            {
                                // If there are no devices, only adapters then just load the first.
                                ActiveAdapter = (IAdapter)_availableAdapters[0];
                            }
                        }
                        catch (Exception ex)
                        {
                            HandleMessageNotification(new CommunicationMessage(this, ex));
                        }

                        foreach (var item in messages)
                        {
                            if (AdaptersAttachedDevices.Count > 0)
                            {
                                item.SupressWarning = true;
                            }
                            HandleMessageNotification(item);
                        }
                    }));
            }
        }

        /// <summary>
        /// Gets the ShowPluginCommand.
        /// </summary>
        public RelayCommand<IPlugin> ShowPluginCommand
        {
            get
            {
                return _showPlugin
                    ?? (_showPlugin = new RelayCommand<IPlugin>(ExecuteShowPluginCommand));
            }
        }

        /// <summary>
        /// Gets the ExitCommand.
        /// </summary>
        public RelayCommand<Window> ExitCommand
        {
            get
            {
                return _exitCommand
                    ?? (_exitCommand = new RelayCommand<Window>(ExecuteExitCommand));
            }
        }

        /// <summary>
        /// Gets the GetAvailablePluginsCommand.
        /// </summary>
        public RelayCommand<object> GetActivePluginsCommand
        {
            get
            {
                return _getActivePlugins
                    ?? (_getActivePlugins = new RelayCommand<object>(ExecuteGetAvailablePluginsCommand));
            }
        }

        /// <summary>
        /// Gets the RefreshCommand.
        /// </summary>
        public RelayCommand<bool> RefreshCommand
        {
            get
            {
                return _refreshCommand
                    ?? (_refreshCommand = new RelayCommand<bool>(ReadAllRegisters));
            }
        }

        /// <summary>
        /// Gets the RefreshCommand.
        /// </summary>
        public RelayCommand UnlockCommand
        {
            get
            {
                return _unlockCommand
                    ?? (_unlockCommand = new RelayCommand(
                    () =>
                    {
                        UnlockDevice();
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
        /// Gets the ClearLogCommand.
        /// </summary>
        public RelayCommand ClearLogCommand
        {
            get
            {
                return _clearLogCommand
                    ?? (_clearLogCommand = new RelayCommand(
                        () =>
                        {
                            Messages.Clear();
                        }
                        ));
            }
        }

        /// <summary>
        /// Gets the ChangeDevice.
        /// </summary>
        public RelayCommand<int> ChangeAdapterCommand
        {
            get
            {
                return _changeAdapter
                    ?? (_changeAdapter = new RelayCommand<int>(ExecuteChangeAdapterCommand));
            }
        }

        public RelayCommand<int[]> ChangeDeviceCommand
        {
            get
            {
                return _changeDevice
                    ?? (_changeDevice = new RelayCommand<int[]>(ExecuteChangeDeviceCommand));
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

        public Visibility TabVisible {
            get
            {
                return _tabVisible;
            }

            set
            {
                if (_tabVisible == value)
                {
                    return;
                }
                _tabVisible = value;
                RaisePropertyChanged(TabVisiblePropertyName);
            }
        }

        #endregion

        #region Logging properties 

        //public bool IsLoggingEnabled
        //{
        //    get { return _enabled; }
        //    set
        //    {
        //        _enabled = value;

        //        UpdateLogging();

        //        OnPropertyChanged("IsLoggingEnabled");
        //    }
        //}

        //private void UpdateLogging()
        //{
        //    if (!IsLoggingEnabled)
        //    {
        //        Serilog.Log.CloseAndFlush();
        //    }
        //}

        //public bool IsInfo
        //{
        //    get { return _information; }
        //    set
        //    {
        //        _information = value;
        //        OnPropertyChanged("IsInfo");
        //    }
        //}
        //public bool IsDebug
        //{
        //    get { return _debug; }
        //    set
        //    {
        //        _debug = value;
        //        OnPropertyChanged("IsDebug");
        //    }
        //}
        //public bool IsError
        //{
        //    get { return _error; }
        //    set
        //    {
        //        _error = value;
        //        OnPropertyChanged("IsError");
        //    }
        //}

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel()
        {
            AdaptersAttachedDevices = new ObservableDictionary<Adapter, List<DeviceAccess.Device>>();
            ApplicationAdapters = new ObservableCollection<Adapter>();
            Adapters = new ObservableCollection<Adapter>();
            Devices = new ObservableCollection<Device>();
            AllAvailablePlugins = new ObservableCollection<IPlugin>();
            ActivePlugins = new ObservableCollection<IPlugin>();
            Messages = new ObservableCollection<IMessageBase>();
            _registers = new ObservableCollection<Register>();
            _statusOnlyRead = Properties.Settings.Default.StatusOnlyRead;
            
            Messenger.Default.Register<NotificationMessage>(this, HandleNotification);
            Messenger.Default.Register<CommunicationMessage>(this, HandleMessageNotification);

            // Set up the poll timer
            _pollTimer.Interval = 500;
            PollingFreq = Properties.Settings.Default.PollingIntervalIndex;
            _pollTimer.Stop();
            _pollTimer.Elapsed += _pollTimer_Elapsed;

            // Set the initial document folder
            string myDocumentsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            ofd.InitialDirectory = myDocumentsFolder;
            sfd.InitialDirectory = myDocumentsFolder;
            
            try
            {
                GetApplicationInfo();
            }
            catch (Exception e)
            {
                HandleMessageNotification(new CommunicationMessage(this, e));
            }
        }

        #endregion

        #region Public Methods

        public void ReStart()
        {
            // Disable polling
            if (_pollActive)
            {
                TogglePollingCommand.Execute(new object());
            }

            // Wait until it is stopped
            while (_pollActive)
            { }

            // Make all the adapters available
            foreach (Adapter adapter in Adapters)
            {
                adapter.Close();
            }

            // Reinitialize collections and variabled
            AdaptersAttachedDevices.Clear();
            ApplicationAdapters.Clear();
            Adapters.Clear();
            Devices.Clear();
            AllAvailablePlugins.Clear();
            ActivePlugins.Clear();

            _registers.Clear();
            ActiveAdapter = null;
            ActiveDevice = null;

            _adapterManager = new AdapterManager();

            // Call Startup
            Startup(null);
        }

        public void Startup(Action callback)
        {
            try
            {
                if (EnumerateHardware.CanExecute(new object()))
                {
                    SplashScreenHelper.ShowText("Scanning hardware...");
                    System.Threading.Thread.Sleep(250);
                    EnumerateHardware.Execute(new object());
                }

                if (LoadPluginsCommand.CanExecute(new object()))
                {
                    SplashScreenHelper.ShowText("Loading plugins...");
                    System.Threading.Thread.Sleep(250);
                    LoadPluginsCommand.Execute(new object());
                }

                if (ActiveAdapter != null)
                {
                    if (GetActivePluginsCommand.CanExecute(new object()))
                    {
                        SplashScreenHelper.ShowText("Selecting adapter plugins...");
                        GetActivePluginsCommand.Execute(ActiveAdapter);
                    }

                    if (ActiveDevice != null)
                    {
                        if (GetActivePluginsCommand.CanExecute(new object()))
                        {
                            SplashScreenHelper.ShowText("Selecting device plugins...");
                            GetActivePluginsCommand.Execute(ActiveDevice);
                        }

                        if (_isInternalMode)
                        {
                            
                            if (_pipeServer == null)
                            {
                                // Start pipe server
                                _serverThread = new System.Threading.Thread(PipeServerThread);
                                _serverThread.Start();
                            }
                        }
                    }
                }
            }
            finally
            {
                //SplashScreenHelper.Hide();

                if (callback != null)
                {
                    callback();
                }
            }
        }

        public void GetApplicationInfo()
        {
            string[] args = Environment.GetCommandLineArgs();
            string fileToFind = "Application.adz";
            string configFile = "AppConfig.xml";
            FileInfo[] files = FileUtilities.DeviceInfoDirectory.GetFiles();
            FileInfo adzfile = files.First(f => f.Name == fileToFind);

            XDocument appConfig = null;

            byte[] code = new byte[] { 0x69, 0x38, 0x6D, 0x53, 0x73, 0x76, 0x36, 0x41, 0x6A };
            string scode = Encoding.ASCII.GetString(code);

            // Verify the file exists
            if (adzfile != null)
            {
                try
                {
                    // Configuration settings
                    using (var ms = new MemoryStream())
                    {
                        using (ZipFile zip = ZipFile.Read(adzfile.FullName))
                        {
                            ZipEntry entry = zip[configFile];
                            entry.Extract(ms);
                            ms.Position = 0;
                            appConfig = XDocument.Load(ms);

                            var internalMode = (XElement)appConfig.XPathSelectElement("Application/Config/DevMode");
                            if (internalMode != null)
                            {
                                _isInternalMode = true;
                            }

                            var triggered = (XElement)appConfig.XPathSelectElement("Application/Config/TriggeredReadDelayMs");
                            if (triggered != null)
                            {
                                int result;
                                if (int.TryParse(triggered.Value, out result))
                                {
                                    _appManager.TriggeredReadDelayMs = result;
                                }
                            }
                        }
                    }

                    // Device Support
                    using (var ms = new MemoryStream())
                    {
                        using (ZipFile zip = ZipFile.Read(adzfile.FullName))
                        {
                            ZipEntry entry = zip[configFile];
                            entry.Extract(ms);
                            ms.Position = 0;
                            appConfig = XDocument.Load(ms);

                            IEnumerable<XElement> devices = appConfig.Descendants("Device");

                            foreach (var item in devices)
                            {
                                var deviceInfo = new DeviceAccess.DeviceManager.DeviceInfo();
                                deviceInfo.MFRID = item.Attribute("MfrId").Value;
                                deviceInfo.Model = item.Attribute("Model").Value;
                                deviceInfo.Revision = item.Attribute("Revision").Value;
                                deviceInfo.Code = item.Attribute("Code").Value;
                                deviceInfo.Key = int.Parse(item.Attribute("Key").Value);
                                deviceInfo.Plugin = item.Attribute("Plugin").Value;
                                deviceInfo.Protocol = item.Attribute("Protocol").Value;
                                _appManager.DeviceInfo.Add(deviceInfo);
                            }

                            IEnumerable<XElement> identityRegs = appConfig.Descendants("Setting");

                            if (identityRegs != null)
                            {
                                foreach (var item in identityRegs)
                                {
                                    var setting = new IdentityRegister()
                                    {
                                        RegCode = (item.Attribute("RegCode").Value != "") ? (byte?)Convert.ToByte(item.Attribute("RegCode").Value, 16) : null,
                                        RegId = Convert.ToByte(item.Attribute("RegId").Value, 16),
                                        Mask = Convert.ToByte(item.Attribute("Mask").Value, 16),
                                        Shift = int.Parse(item.Attribute("Shift").Value)
                                    };
                                    _appManager.IdentityRegisters.Add(setting);
                                }
                            }

                            IEnumerable<XElement> identityRegsTwoBytes = appConfig.Descendants("TwoBytesSetting");

                            if (identityRegsTwoBytes != null)
                            {
                                foreach (var item in identityRegsTwoBytes)
                                {
                                    var settingBytes = new IdentityRegisterTwoBytes()
                                    {
                                        RegCode = (item.Attribute("RegCode").Value != "") ? (ushort?)Convert.ToUInt16(item.Attribute("RegCode").Value, 16) : null,
                                        RegId = Convert.ToUInt16(item.Attribute("RegId").Value, 16),
                                        Mask = Convert.ToUInt16(item.Attribute("Mask").Value, 16),
                                        Shift = int.Parse(item.Attribute("Shift").Value)
                                    };
                                    _appManager.IdentityRegistersTwoBytes.Add(settingBytes);
                                }
                            }
                        }
                    }

                    foreach (var item in args)
                    {
                        // Forces the name of the device to load.
                        // We will only enable the protocol for this device

                        // Forces the i2c address of the device to load
                        if (item.Contains("/i2c="))
                        {
                            string[] address = item.Split('=');
                            if (address.Length == 2)
                            {
                                ForcedI2cAddress = byte.Parse(address[1].Trim(), System.Globalization.NumberStyles.AllowHexSpecifier);
                            }
                        }

                        switch (item.ToLower())
                        {
                            case "/demo":
                                _isDemoMode = true;
                                _tabVisible = Visibility.Collapsed;
                                break;
                            case "/devmode":
                                _isInternalMode = true;
                                _tabVisible = Visibility.Collapsed;
                                break;
                            case "/demowithdev":   //Combining demo mode with dev mode
                                _isDemoMode = true;
                                _isInternalMode = true;
                                _tabVisible = Visibility.Visible;
                                break;
                            case "/log":
                                _isLogMode = true;

                                if (_isLogMode)
                                {
                                    Log.Logger = new LoggerConfiguration()
                                     .MinimumLevel.Debug()
                                     .WriteTo.File(@"C:\Logs\MurataAppLogs.log", rollingInterval: RollingInterval.Year)
                                     .CreateLogger();
                                }
                                break;
                            case "/16bitaddressing":
                                var address = "38";
                                _isInternalMode = true;
                                _is16BitI2CMode = true;
                                ForcedI2cAddress = byte.Parse(address, System.Globalization.NumberStyles.AllowHexSpecifier);
                                if (!_isDemoMode)
                                {
                                    _tabVisible = Visibility.Collapsed;
                                }
                                break;
                            case "/mta":
                                _isMTAMode = true;
                                if (!_isDemoMode)
                                {
                                    _tabVisible = Visibility.Collapsed;
                                }
                                break;
                            case "/silent":
                                _isSilentMode = true;
                                _tabVisible = Visibility.Collapsed;
                                break;
                            case "/i2cauto":
                                _i2cAutoSlaveAddress = true;
                                _tabVisible = Visibility.Collapsed;
                                break;
                            case "/r":
                                _isDeviceIdHidden = true;
                                _tabVisible = Visibility.Collapsed;
                                break;
                        }

                        if (_isDemoMode & !_isInternalMode)
                        {
                            // Check command line switches

                            var devicenames = "/d=PE24103-R01,PE24103-R01,PE24101-102,PE23108-R01,PE24104-R01,PE26100,VADER";

                            if (devicenames.Contains("/d="))
                            {
                                string[] deviceName = devicenames.Split('=');
                                string[] devices = deviceName[1].Split(',').Select(sValue => sValue.Trim()).ToArray();

                                DeviceInfoNameList = new List<string>();

                                foreach (string device in devices)
                                {
                                    DeviceInfoNameList.Add(device);
                                }

                                if (deviceName.Length == 2)
                                {
                                    DeviceInfoName = deviceName[1].ToUpper().Trim();
                                    var device = _appManager.DeviceInfo.FirstOrDefault(d => d.Plugin.ToUpper() == DeviceInfoName);
                                    if (device != null)
                                    {
                                        if (device.Protocol.ToUpper() == "I2C")
                                        {
                                            this.IncludePmBus = false;
                                        }
                                        else if (device.Protocol.ToUpper() == "PMB")
                                        {
                                            this.IncludeI2c = false;
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            // Forces the name of the device to load.
                            // We will only enable the protocol for this device
                            if (item.Contains("/d="))
                            {
                                DeviceInfoNameList = new List<string>();

                                string[] deviceName = item.Split('=');
                                if (deviceName.Length == 2)
                                {
                                    DeviceInfoName = deviceName[1].ToUpper().Trim();

                                    DeviceInfoNameList.Add(DeviceInfoName);

                                    var device = _appManager.DeviceInfo.FirstOrDefault(d => d.Plugin.ToUpper() == DeviceInfoName);
                                    if (device != null)
                                    {
                                        if (device.Protocol.ToUpper() == "I2C")
                                        {
                                            this.IncludePmBus = false;
                                        }
                                        else if (device.Protocol.ToUpper() == "PMB")
                                        {
                                            this.IncludeI2c = false;
                                        }
                                    }
                                }
                            }
                        }
                    }
    
                }
                catch (Exception ex1)
                {
                    throw new Exception("Fatal Error: Cannot read the application configuration file.", ex1);
                    Log.Error("Fatal Error: Cannot read the application configuration file.", ex1);
                }
            }
        }

        public IPlugin GetActiveDeviceUserInterface()
        {
            IPlugin ip = null;

            if (ActiveDevice != null && ActivePlugins.Count > 0)
            {
                var baseControl = string.Format("{0}Control", ActiveDevice.BaseName);
                ip = ActivePlugins.FirstOrDefault(p => p.GetPluginInfo.AssemblyName.Contains(baseControl));
            }

            return ip;
        }

        public IPlugin GetRegisterControlUserInterface()
        {
            IPlugin ip = null;

            if (ActiveDevice != null && ActivePlugins.Count > 0)
            {
                var registerControl = string.Format("RegisterControl", ActiveDevice.BaseName);
                ip = ActivePlugins.FirstOrDefault(p => p.GetPluginInfo.AssemblyName.Contains(registerControl));
            }

            return ip;
        }

        public override void Cleanup()
        {
            _closing = true;

            if (_cancelSource != null)
            {
                _cancelSource.Cancel();
                using (NamedPipeClientStream npcs = new NamedPipeClientStream(PIPE_NAME))
                {
                    try
                    {
                        npcs.Connect(250);
                    }
                    finally
                    {
                        while (_serverThread.IsAlive) { System.Threading.Thread.Sleep(100); }
                    }
                }
            }

            if (_pollActive)
            {
                TogglePollingCommand.Execute(new object());
            }

            while (_pollActive)
            { }

            base.Cleanup();
        }

        #endregion

        #region Private Command Methods

        private void ExecuteChangeAdapterCommand(int selectedDeviceIndex)
        {
            if (AdaptersAttachedDevices.Count > 0)
            {
                ActiveAdapter = (IAdapter)AdaptersAttachedDevices.Keys.ElementAt(selectedDeviceIndex);
                Devices.Clear();
                AdaptersAttachedDevices.ElementAt(selectedDeviceIndex).Value.ForEach(d => Devices.Add(d));
                ActiveDevice = Devices[0];
                _registers.Clear();
                ActiveDevice.Registers.ForEach(r => _registers.Add(r));
            }
            else
            {
                // If there are no devices, only adapters then just load the first.
                ActiveAdapter = (IAdapter)_availableAdapters[selectedDeviceIndex];
            }
        }

        private void ExecuteChangeDeviceCommand(int[] selectedIndexes)
        {
            if (AdaptersAttachedDevices.Count > 0)
            {
                var adapter = AdaptersAttachedDevices.Keys.ElementAt(selectedIndexes[0]);
                ActiveDevice = Devices[selectedIndexes[1]];
                _registers.Clear();
                ActiveDevice.Registers.ForEach(r => _registers.Add(r));

                //set below variable to true based on the protocol - I2c PE24103
                //Is16BitI2CMode = true;
            }
        }

        private void ExecuteGetAvailablePluginsCommand(object host)
        {
            if (host is IDevice)
            {
                if (((IDevice)host).PluginCompatibility.Count > 0)
                {
                    foreach (IPlugin item in AllAvailablePlugins)
                    {
                        string[] assemblyInfo = item.GetPluginInfo.AssemblyName.Split(',');
                        if (((IDevice)host).PluginCompatibility.Contains(assemblyInfo[0]))
                        {
                            ActivePlugins.Add(item);
                        }
                    }
                }
            }
            else if (host is IAdapter)
            {
                if (((IAdapter)host).PluginCompatibility.Count > 0)
                {
                    foreach (IPlugin item in AllAvailablePlugins)
                    {
                        string[] assemblyInfo = item.GetPluginInfo.AssemblyName.Split(',');
                        if (((IAdapter)host).PluginCompatibility.Contains(assemblyInfo[0]))
                        {
                            ActivePlugins.Add(item);
                        }
                    }
                }
            }
        }

        private void ExecuteExitCommand(Window mainWindow)
        {
            if (mainWindow != null)
            {
                mainWindow.Close();
            }
        }

        private void ExecuteShowPluginCommand(IPlugin plugin)
        {

            var host = ServiceLocator.Current.GetInstance<IPluginHost>();
            if (IsAllRegistersRead)
            {
                host.PlacePlugin(plugin, this, true);
            }
            else
            {
                host.PlacePlugin(plugin, this);
            }
        }

        private void SetDeviceBase()
        {
            if (ActiveDevice == null)
            {
                _deviceBase = null;
                return;
            }

            var device = ActiveDevice as IDevice;
            // Get the name of the device without any containing dashes
            string deviceName = device.DeviceInfoName.Replace("-", "");

            // Get a reference to an available type with the same name.
            var customizer = Type.GetType("DeviceAccess." + deviceName + ",DeviceAccess");

            // If found, create an instance of the customizing class and perform its base actions
            if (customizer != null)
            {
                try
                {
                    _deviceBase = Activator.CreateInstance(customizer, device, _isDemoMode) as CustomizeBase;
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        #endregion

        #region Private Methods

        private void SetIsEnabled()
        {
            IsEnabled = (ActiveAdapter == null || ActiveDevice == null) ? false : true;
        }

        private async void ReadAllRegisters(bool firstRead = false)
        {
            SplashScreenHelper.ShowText("Reading Register Values");

            await Task.Run(() => ReadAll(firstRead));

            if (AllRegistersRead != null)
                AllRegistersRead(this, EventArgs.Empty);
        }

        private void UnlockDevice()
        {
            if (_deviceBase != null)
            {
                _deviceBase.Unlock();
            }
        }

        private void ReadAll(bool firstRead = false)
        {
            var regs = GetPolledRegisters(firstRead);
            foreach (Register reg in regs)
            {
                // If closing, return immediately
                if (_closing)
                {
                    _pollActive = false;
                    break;
                }

                try
                {
                    if (reg.DataType.ToUpper() == "C")
                    {
                        ActiveDevice.ReadRegister(reg);
                    }
                    else
                    {
                        ActiveDevice.ReadRegisterValue(reg);
                    }

                    if (reg.TriggeredRead)
                    {
                        System.Threading.Thread.Sleep(_appManager.TriggeredReadDelayMs);
                        if (reg.DataType.ToUpper() == "C")
                        {
                            ActiveDevice.ReadRegister(reg);
                        }
                        else
                        {
                            ActiveDevice.ReadRegisterValue(reg);
                        }
                    }

                    System.Threading.Thread.Sleep(10);
                    PollingState = PollState.Polling;
                }
                catch (Exception ex)
                {
                    PollingState = PollState.Error;
                    MessengerSend(ex);
                    ResetAdapter();
                    break;
                }
            }

            if (ActiveDevice != null)
            {
                if (ActiveDevice.SlaveDevices != null)
                {
                    // If the active deivce contains slaves then read them too
                    foreach (SlaveDevice device in ActiveDevice.SlaveDevices)
                    {
                        var slaveRegs = _statusOnlyRead ? device.Registers.Where(r => r.ReadOnly) : device.Registers;
                        foreach (Register reg in slaveRegs)
                        {
                            if (_closing)
                            {
                                _pollActive = false;
                                break;
                            }

                            try
                            {
                                device.ReadRegisterValue(reg);
                                if (reg.TriggeredRead)
                                {
                                    System.Threading.Thread.Sleep(_appManager.TriggeredReadDelayMs);
                                    device.ReadRegisterValue(reg);
                                }
                                System.Threading.Thread.Sleep(10);
                                PollingState = PollState.Polling;
                            }
                            catch (Exception ex)
                            {
                                PollingState = PollState.Error;
                                MessengerSend(ex);
                                break;
                            }
                        }
                    }
                }

                // Notify the active plugs to log data if applicable
                foreach (var item in ActivePlugins)
                {
                    item.Log();
                }
            }

            PollingState = (_pollActive) ? PollState.On : PollState.Off;

            try
            {
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

        private void SendMessageToAppLog(string type, string message)
        {
            HandleMessageNotification(
                new CommunicationMessage
                {
                    MessageType = type,
                    Sender = this,
                    TopLevel = new Exception(message)
                });
        }

        private void MessengerSend(Exception ex)
        {
            if (ex.InnerException != null)
                Debug.Print("Error: " + ex.InnerException.Message + " " + ex.Message);
            else
                Debug.Print("Error: " + ex.Message);
            Messenger.Default.Send(new CommunicationMessage(this, ex));
        }

        private void ResetAdapter()
        {
            if (ActiveDevice != null)
            {
                var dongle = ActiveDevice.Protocol;
                dongle.ResetAdapter();
            }
        }

        private ObservableCollection<Register> GetPolledRegisters(bool firstRead = false)
        {
            // First determine if the user wants to only poll read only registers
            var regs = _statusOnlyRead && _pollActive ? _registers.Where(r => r.ReadOnly) : _registers;

            // Then remove the special registers that only access through Send and Receive Byte commands
            regs = regs.Where(r => r.Access.ToLower() != "sendbyte" && r.Access.ToLower() != "receivebyte");

            if (!firstRead)
            {
                // Finally, remove block registers
                regs = regs.Where(r => r.Access.ToLower() != "rblock");
            }

            // Convert to observable collection and return
            ObservableCollection<Register> oRegs = new ObservableCollection<Register>(regs);
            return oRegs;
        }

        #endregion

        #region Event Handling

        private async void _pollTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _pollTimer.Stop();
            await Task.Run(() => ReadAll());

            if (_pollActive)
            {
                _pollTimer.Start();
            }
        }

        private void HandleNotification(NotificationMessage message)
        {
            if (message.Notification == Notifications.CleanupNotification)
            {
                Cleanup();
                Messenger.Default.Unregister(this);
            }

            if (message.Notification == Notifications.ReadAllNotification)
            {
                ReadAllRegisters();
            }
        }

        private void HandleMessageNotification(IMessageBase message)
        {
            Application.Current.Dispatcher.BeginInvoke(
              (Action)(() =>
              {
                  Messages.Insert(0, message);
                  if (message.MessageType == MessageType.Error || (message.SupressWarning == false && message.MessageType == MessageType.Warning))
                  {
                      Messenger.Default.Send(new NotificationMessage(Notifications.DisplayErrorsNotification));
                  }
              }));
        }

        #endregion

        #region PipeServer Commands

        private void PipeServerThread(object data)
        {
            _cancelSource = new System.Threading.CancellationTokenSource();
            _pipeBufferSize = 256 * AdaptersAttachedDevices.Count;
            Byte[] bytes = new Byte[_pipeBufferSize];
            int numBytes = 0;

            try
            {
                _pipeServer = new NamedPipeServerStream(PIPE_NAME, PipeDirection.InOut, 1, PipeTransmissionMode.Message);
                SendMessageToAppLog(MessageType.Ok, "IPC server running, waiting for client...");

                while (true)
                {
                    // Wait for a client to connect
                    _pipeServer.WaitForConnection();

                    // Check to see if the application is closing down
                    if (_cancelSource.IsCancellationRequested)
                    {
                        _pipeServer.Disconnect();
                        _pipeServer.Close();
                        return;
                    }

                    do
                    {
                        numBytes = _pipeServer.Read(bytes, 0, _pipeBufferSize);
                    } while (numBytes > 0 && !_pipeServer.IsMessageComplete);

                    if (numBytes > 0)
                    {
                        switch (bytes[0])
                        {
                            case 0x00:
                                GetAdapterCount();
                                break;
                            case 0x10:  // I2C Read Device
                                PipeServer_I2cReadDevice(bytes);
                                break;
                            case 0x11:  // I2C Read All Devices
                                PipeServer_I2cReadAllDevices(bytes);
                                break;
                            case 0x20:  // I2C Write
                                PipeServer_I2cWriteDevice(bytes);
                                break;
                            case 0x21:  // I2C Write All Devices
                                PipeServer_I2cWriteAllDevices(bytes);
                                break;
                            default:    // Error
                                SendMessageToAppLog(MessageType.Error, "IPC <Unknown Command> : " + bytes[0].ToString("X2"));
                                break;
                        }
                    }

                    _pipeServer.Disconnect();
                }
            }
            catch (Exception ex)
            {
                SendMessageToAppLog(MessageType.Error, ex.Message);
                return;
            }
        }

        private void GetAdapterCount()
        {
            _pipeServer.WriteByte((byte)AdaptersAttachedDevices.Count);
        }

        private void PipeServer_I2cReadDevice(byte[] bytes)
        {
            // Byte[0] = CMD (Not used here)
            // Byte[1] = Adapter Poistion
            // Byte[2] = Register of target device
            // Byte[3] = Number of bytes to read
            try
            {
                var adapter = AdaptersAttachedDevices.ElementAtOrDefault(bytes[1]);
                var device = adapter.Value.First();
                byte[] data = new byte[bytes[3]];

                var protocol = adapter.Key.GetAttachedProtocolObjects().First(p => p.GetInterfaceType() == typeof(II2c));
                var i2c = protocol as I2cProtocol;

                string message = string.Format("IPC <I2C-R> : {0:X2}-{1:X2}-{2:X2}-{3:X2}", bytes[1], i2c.TargetAddress, bytes[2], bytes[3]);
                SendMessageToAppLog(MessageType.Ok, message);

                device.ReadBlock(bytes[2], bytes[3], ref data);
                _pipeServer.Write(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                SendMessageToAppLog(MessageType.Error, ex.Message);
            }
        }

        private void PipeServer_I2cReadAllDevices(byte[] bytes)
        {
            // Byte[0] = CMD (Not used here)
            // Byte[1] = Register of target device
            // Byte[2] = Number of bytes to read
            try
            {
                byte[] allData = new byte[bytes[2] * AdaptersAttachedDevices.Count];
                int count = 0;
                foreach (DictionaryEntry adapter in AdaptersAttachedDevices)
                {
                    var device = ((List<Device>)adapter.Value).First();
                    byte[] data = new byte[bytes[2]];

                    var protocol = ((Adapter)adapter.Key).GetAttachedProtocolObjects().First(p => p.GetInterfaceType() == typeof(II2c));
                    var i2c = protocol as I2cProtocol;

                    string message = string.Format("IPC <I2C-R> : {0:X2}-{1:X2}-{2:X2}-{3:X2}", count, i2c.TargetAddress, bytes[1], bytes[2]);
                    SendMessageToAppLog(MessageType.Ok, message);

                    device.ReadBlock(bytes[1], bytes[2], ref data);
                    Array.Copy(data, 0, allData, count * data.Length, data.Length);
                    count++;
                }
                _pipeServer.Write(allData, 0, allData.Length);
            }
            catch (Exception ex)
            {
                SendMessageToAppLog(MessageType.Error, ex.Message);
            }
        }

        private void PipeServer_I2cWriteDevice(byte[] bytes)
        {
            // Byte[0] = CMD (Not used here)
            // Byte[1] = Adapter Position
            // Byte[2] = Register of target device
            // Byte[3] = Number of bytes to write
            // Byte[n] = Data
            try
            {
                var adapter = AdaptersAttachedDevices.ElementAtOrDefault(bytes[1]);
                var device = adapter.Value.First();
                byte[] data = new byte[bytes[3]];
                Array.Copy(bytes, 4, data, 0, bytes[3]);

                var protocol = adapter.Key.GetAttachedProtocolObjects().First(p => p.GetInterfaceType() == typeof(II2c));
                var i2c = protocol as I2cProtocol;

                string message = string.Format("IPC <I2C-W> : {0:X2}-{1:X2}-{2:X2}-{3:X2}-{4}", bytes[1], i2c.TargetAddress, bytes[2], bytes[3], BitConverter.ToString(data));
                SendMessageToAppLog(MessageType.Ok, message);

                device.WriteBlock(bytes[2], bytes[3], ref data);

                // Read back data to refresh all plugins
                if (device.Adapter.AdapterSerialNumber == ActiveDevice.Adapter.AdapterSerialNumber && !_pollActive)
                {
                    for (int i = 0; i < bytes[3]; i++)
                    {
                        var reg = Registers.First(r => r.Address == bytes[2] + i);
                        ActiveDevice.ReadRegisterValue(reg);
                    }
                }
            }
            catch (Exception ex)
            {
                SendMessageToAppLog(MessageType.Error, ex.Message);
            }
        }

        private void PipeServer_I2cWriteAllDevices(byte[] bytes)
        {
            // Byte[0] = CMD (Not used here)
            // Byte[1] = Register of target device
            // Byte[2] = Number of bytes to write
            // Byte[n] = Data
            try
            {
                int count = 0;
                foreach (DictionaryEntry adapter in AdaptersAttachedDevices)
                {
                    var device = ((List<Device>)adapter.Value).First();
                    byte[] data = new byte[bytes[2]];
                    Array.Copy(bytes, 3, data, 0, bytes[2]);

                    var protocol = ((Adapter)adapter.Key).GetAttachedProtocolObjects().First(p => p.GetInterfaceType() == typeof(II2c));
                    var i2c = protocol as I2cProtocol;

                    string message = string.Format("IPC <I2C-W> : {0:X2}-{1:X2}-{2:X2}-{3:X2}-{4}", count++, i2c.TargetAddress, bytes[1], bytes[2], BitConverter.ToString(data));
                    SendMessageToAppLog(MessageType.Ok, message);

                    device.WriteBlock(bytes[1], bytes[2], ref data);

                    // Read back data to refresh all plugins
                    if (device.Adapter.AdapterSerialNumber == ActiveDevice.Adapter.AdapterSerialNumber && !_pollActive)
                    {
                        for (int i = 0; i < bytes[2]; i++)
                        {
                            var reg = Registers.First(r => r.Address == bytes[1] + i);
                            ActiveDevice.ReadRegisterValue(reg);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SendMessageToAppLog(MessageType.Error, ex.Message);
            }
        }

        #endregion

        #region Functions were moved to device plugin

        private async void LoadRegisters()
        {
            ofd.Filter = "Register files (*.map)|*.map";
            if (ofd.ShowDialog() == true)
            {
                //RegisterMap map = DeserializeRegisterMap(ofd.FileName);
                RegisterMap map = XmlDeserializeRegisterMap(ofd.FileName);
                if (map == null)
                {
                    return;
                }

                if (map.DeviceName == _activeDevice.DisplayName)
                {
                    try
                    {
                        bool status = await Task<bool>.Run(() => _activeDevice.LoadRegisters(map));
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show("Loading register map failed.\r\nReason: " + e.Message);
                        Log.Error("Loading register map failed.\r\nReason: " + e.Message);
                    }
                }
            }
        }

        private async void SaveRegisters()
        {
            sfd.Filter = "Register files (*.map)|*.map";
            if (sfd.ShowDialog() == true)
            {
                RegisterMap regMap = (RegisterMap)
                    await Task<RegisterMap>.Run(() => _activeDevice.CreateRegisterMap());
                XmlSerializeRegisterMap(sfd.FileName, regMap);
                //SerializeRegisterMap(sfd.FileName, regMap);
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
                Log.Error("Failed to serialize register map.\r\nReason: " + e.Message);
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
                Log.Error("Failed to serialize register map.\r\nReason: " + e.Message);
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
                Log.Error("Failed to deserialize register map.\r\nReason: " + e.Message);
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
                Log.Error("Failed to deserialize register map.\r\nReason: " + e.Message);
            }
            finally
            {
                fs.Close();
            }

            return map;
        }

        #endregion
        #region INotifyPropertyChanged implementation

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)(() =>
            {
                PropertyChangedEventHandler handler = PropertyChanged;
                if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
            }));
        }

        #endregion

    }
}
