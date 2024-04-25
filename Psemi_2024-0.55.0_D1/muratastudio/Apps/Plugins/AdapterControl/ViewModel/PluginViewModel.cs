using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using HardwareInterfaces;
using PluginFramework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Runtime;
using AdapterAccess;
using AdapterAccess.Adapters;
using AdapterAccess.Protocols;
using System.Windows.Media;
using Microsoft.Win32;
using System.Reflection;
using System.IO;
using DeviceAccess;
using System.Threading;
using System.Runtime.CompilerServices;
using Serilog;

namespace AdapterControl.ViewModel
{
    public class PluginViewModel : DependencyObject, ICleanup, INotifyPropertyChanged
    {
        #region Private Members

        private IDevice _device;
        public Device _activeDev;
        private IRegister _register;

        private RelayCommand _writeCommand;
        private RelayCommand _readCommand;
        private RelayCommand<string> _executeScriptCommand;
        private RelayCommand<int> _selectAdapterCommand;
        private RelayCommand _loadScriptCommand;
        private RelayCommand _clearLogCommand;
        private RelayCommand _saveLogCommand;
        private RelayCommand _cancelScriptCommand;
        private RelayCommand<int> _changeBitRate;

        private RelayCommand<int> _changeDriveByZero;

        private SaveFileDialog sfd = new SaveFileDialog();
        private ObservableCollection<Adapter> _adapters;
        internal Adapter _activeAdapter;
        private dynamic _activeProtocol;
        private List<Protocol> _protocols;
        private ScriptManager _scriptMananger;
        private CancellationTokenSource _cancelSource;
        private bool _isInternalMode;
        private string _i2cAddress = "02";
        private int _addressingType = 2;
        private int _bitRate = 0;           // 100Khz
        private int _driveByZero = 2;       // SDA
        private bool _supportsDrive = true;

        private SolidColorBrush _writeColor = new SolidColorBrush(Color.FromRgb(75, 95, 110));
        private SolidColorBrush _readColor = new SolidColorBrush(Color.FromRgb(0, 0, 0));
        private SolidColorBrush _errorColor = new SolidColorBrush(Color.FromRgb(102, 0, 0));

        public enum ActiveProtocol
        {
            None,
            I2C,
            PMBus,
            Virtual
        }

        private ActiveProtocol _activeProtocolType = ActiveProtocol.None;

        #endregion

        #region Public Properties

        public ObservableCollection<Adapter> Adapters
        {
            get { return _adapters; }
        }

        public ObservableCollection<LogEntry> LogEntries { get; set; }

        public ObservableCollection<string> Scripts { get; set; }

        public ObservableCollection<string> BitRateDescriptions { get; set; }

        public ActiveProtocol ActiveProtocolType
        {
            get { return _activeProtocolType; }
            set
            {
                if (value != _activeProtocolType)
                {
                    _activeProtocolType = value;
                    OnPropertyChanged("ActiveProtocolType");
                }
            }
        }

        private Visibility _isVader;
        public Visibility IsVader
        {
            get { return _isVader; }

            set
            {
                if (_isVader == value)
                {
                    return;
                }

                _isVader = value;
                OnPropertyChanged("IsVader");
            }
        }
        public int BitRate
        {
            get { return _bitRate; }
            set
            {
                if (value != _bitRate)
                {
                    _bitRate = value;
                    OnPropertyChanged("BitRate");
                }
            }
        }

        public int DriveByZero
        {
            get { return _driveByZero; }
            set
            {
                if (value != _driveByZero)
                {
                    _driveByZero = value;
                    OnPropertyChanged("DriveByZero");
                }
            }
        }

        public bool SupportsDriveByZero
        {
            get { return _supportsDrive; }
            set
            {
                if (value != _supportsDrive)
                {
                    _supportsDrive = value;
                    OnPropertyChanged("SupportsDriveByZero");
                }
            }
        }

        #endregion

        #region Constructor

        public PluginViewModel(object device, object activeDevice, bool isInternalMode, bool is16BitI2CMode = false)
        {

            LogEntries = new ObservableCollection<LogEntry>();
            Scripts = new ObservableCollection<string>();
            BitRateDescriptions = new ObservableCollection<string>();

            _device = activeDevice as IDevice;
            _activeDev = activeDevice as Device;
            _register = activeDevice as IRegister;

            if (is16BitI2CMode)
            {
                AddressingType = 3;
                int _readCount = 2;
                ReadCount = _readCount.ToString();
            }
            else if (_device.DeviceName == "PE26100")
            {
                AddressingType = 2;

                int _readCount = 1;
                ReadCount = _readCount.ToString();
                PE26100Mode = true;
            }

            _adapters = new ObservableCollection<Adapter>(((ObservableCollection<Adapter>)device).Where(a => a.IsOpened).ToList());

            // Automatically select the first protocol
            SelectAdapter(0);
            _isInternalMode = isInternalMode;

            Messenger.Default.Register<NotificationMessage>(this, HandleNotification);
        }

        #endregion

        #region Dependency Properties

        public string I2cAddress
        {
            get { return _i2cAddress; }

            private set
            {
                if (_i2cAddress != value)
                {
                    _i2cAddress = value;
                    OnPropertyChanged("I2cAddress");
                }
            }
        }


        public int AddressingType
        {
            get { return _addressingType; }

            private set
            {
                if (_addressingType != value)
                {
                    _addressingType = value;
                    OnPropertyChanged("AddressingType");
                }
            }
        }
        public string RegisterAddress
        {
            get { return (string)GetValue(RegisterAddressProperty); }
            set { SetValue(RegisterAddressProperty, value); }
        }

        public string Data
        {
            get { return (string)GetValue(DataProperty); }
            set
            {
                SetValue(DataProperty, value);
                OnPropertyChanged("Data");
            }
        }

        public string ReadCount
        {
            get { return (string)GetValue(ReadCountProperty); }

            set
            {
                SetValue(ReadCountProperty, value);
                OnPropertyChanged("ReadCount");
            }
        }

        public bool PE26100Mode { get; set; }

        public static DependencyProperty RegisterAddressProperty =
            DependencyProperty.Register("RegisterAddress", typeof(string), typeof(AdapterControl), new FrameworkPropertyMetadata("01"));

        public static DependencyProperty DataProperty =
            DependencyProperty.Register("Data", typeof(string), typeof(AdapterControl), new FrameworkPropertyMetadata("00"));

        public static DependencyProperty ReadCountProperty =
            DependencyProperty.Register("ReadCount", typeof(string), typeof(AdapterControl), new FrameworkPropertyMetadata("1"));

        #endregion

        #region Commands

        /// <summary>
        /// Gets the MyCommand.
        /// </summary>
        /// Read Write Commands
        public RelayCommand WriteCommand
        {
            get
            {
                return _writeCommand
                    ?? (_writeCommand = new RelayCommand(
                    () =>
                    {
                        Thread.Sleep(100); // Force a delay to prevent enter key from repeated button clicks

                        string dt = DateTime.Now.ToString("hh:mm:ss:fff");
                        byte targetAddress = Convert.ToByte(I2cAddress, 16);


                        string data = Data.Replace(" ", "");
                        string regAddress = string.Empty;

                        if (AddressingType == 3)
                        {
                            ushort address = Convert.ToUInt16(RegisterAddress, 16);

                            if (ReadCount == "2")
                            {
                                regAddress = address.ToString("X3");
                                if (data.Length % 4 != 0)
                                {
                                    data = String.Concat(data.Substring(0, (data.Length / 4) * 4) +
                                                            (data.Substring((data.Length / 4) * 4).PadLeft(4, '0')));
                                }
                            }

                        }
                        else
                        {

                            byte address = Convert.ToByte(RegisterAddress, 16);
                            regAddress = address.ToString("X2");

                            if (data.Length % 2 != 0)
                            {
                                data = data.PadLeft(data.Length + 1, '0');
                            }
                        }

                        byte[] writeData = new byte[data.Length / 2];

                        for (int i = 0; i < data.Length / 2; i++)
                        {
                            writeData[i] = Convert.ToByte(data.Substring(i * 2, 2), 16);
                        }

                        int decRegAddress = int.Parse(RegisterAddress, System.Globalization.NumberStyles.HexNumber);
                        Register reg = _activeDev?.Registers?.FirstOrDefault(r => r.Address.ToString() == decRegAddress.ToString());
                        int startAddress = decRegAddress;
                        int byteCount = 0;
                        string element = string.Empty;
                        int regSize = 0;
                        string hexaData = string.Empty;
                        byte[] writeData1 = new byte[1];

                        string dataResponse = string.Empty;

                        if (reg != null)
                        {
                            string protocolType = ((DeviceAccess.Device)_device).Protocol.Name.ToLower();

                            if ((_activeDev.DeviceInfoName == "PE24103-R01" || _activeDev.DeviceInfoName == "PE24104-R01")
                               && ((DeviceAccess.Device)_device).Protocol.Name == "PMBus")
                            {
                                int startRegSize = (int)reg.Size;
                                byte[] startRegData = new byte[startRegSize];

                                for (int i = 0; i < startRegSize; i++)
                                {
                                    startRegData[i] = writeData[i];
                                }

                                _register.WriteRegister(reg, protocolType, startRegSize.ToString(), startRegData);

                                int currentByteCount = startRegData.Length;
                                int expectedByteCount = writeData.Length;

                                if (currentByteCount < expectedByteCount)
                                {
                                    int count = 0;

                                    for (int i = currentByteCount; i < writeData.Length; i++)
                                    {
                                        startAddress = startAddress + 1;

                                        Register register = _activeDev?.Registers?.FirstOrDefault(r => r.Address.ToString() == startAddress.ToString());

                                        byte[] currentRegData = new byte[1];

                                        if (register != null && register.Size != 0)
                                        {
                                            currentRegData = new byte[register.Size];
                                        }

                                        if (currentByteCount < writeData.Length)
                                        {
                                            for (int j = 0; j < currentRegData.Length; j++)
                                            {
                                                currentRegData[j] = writeData[currentByteCount];
                                                currentByteCount++;
                                            }

                                            _register.WriteRegister(register, protocolType, currentRegData.Length.ToString(), currentRegData);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                _register.WriteRegister(reg, protocolType, ReadCount, writeData);
                            }

                            _register.ReadRegisterValue(reg);

                            //if (AddressingType == 2 && ((DeviceAccess.Device)_device).Protocol.Name.ToLower() == "virtual")
                            //{
                            //    dataResponse = BitConverter.ToString(writeData.Reverse().ToArray());
                            //}
                            //else
                            //{
                            dataResponse = BitConverter.ToString(writeData);
                            //}
                            // Refreshing list of updated registers.
                            if (reg.Size != writeData.Length)
                            {
                                decRegAddress = decRegAddress + 1;

                                Register regToRead = reg;
                                int currentByteCount = Convert.ToUInt16(reg.Size);
                                int expectedByteCount = writeData.Length;

                                for (int i = currentByteCount; i < expectedByteCount; i++)
                                {
                                    if (currentByteCount < expectedByteCount)
                                    {
                                        regToRead = _activeDev?.Registers?.FirstOrDefault(r => r.Address.ToString() == (decRegAddress).ToString());

                                        if (regToRead != null)
                                        {
                                            if (AddressingType == 3 && protocolType == "i2c")
                                            {
                                                byte[] i2cData = new byte[regToRead.Size];
                                                int count = 0;

                                                for (int j = 0; j < i2cData.Length; j++)
                                                {
                                                    i2cData[count] = writeData[currentByteCount + j];
                                                    count++;
                                                }

                                                _register.WriteRegister(regToRead, protocolType, ReadCount, i2cData);
                                                currentByteCount += i2cData.Length;
                                            }

                                            _register.ReadRegisterValue(regToRead);
                                        }

                                        decRegAddress = decRegAddress + 1;
                                    }
                                }
                            }
                            //}
                            //else
                            //{

                            //}
                        }

                        Dispatcher.BeginInvoke(
                          (Action)(() =>
                          {
                              LogEntries.Insert(0, new LogEntry()
                              {
                                  DateTime = dt,
                                  ProtocolName = _activeProtocolType.ToString(),
                                  Direction = "W",
                                  Address = targetAddress.ToString("X2"),
                                  Register = regAddress,
                                  Length = ReadCount.PadLeft(2, '0'),
                                  ForeColor = _writeColor,
                                  Data = dataResponse
                              });
                          }));
                    }));
            }
        }

        /// <summary>
        /// Gets the MyCommand.
        /// </summary>
        public RelayCommand ReadCommand
        {
            get
            {
                return _readCommand
                    ?? (_readCommand = new RelayCommand(
                    () =>
                    {
                        Thread.Sleep(100); // Force a delay to prevent enter key from repeated button clicks
                        string dt = DateTime.Now.ToString("hh:mm:ss:fff");

                        int readCount = int.Parse(ReadCount);

                        byte targetAddress = Convert.ToByte(I2cAddress, 16);

                        if (AddressingType == 2)
                        {

                            byte address = Convert.ToByte(RegisterAddress, 16);
                            int decValue = int.Parse(RegisterAddress, System.Globalization.NumberStyles.HexNumber);
                            Register register = _register.Registers.FirstOrDefault(a => a.Address == decValue);
                            ReadBlock(targetAddress, address, readCount, (int)register.Size);

                        }
                        else if (AddressingType == 3)
                        {
                            ushort regAddress = Convert.ToUInt16(RegisterAddress, 16);
                            Read16BitBlock(targetAddress, regAddress, readCount);
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
        /// Selects an adapter.
        /// </summary>
        public RelayCommand<int> SelectAdapterCommand
        {
            get
            {
                return _selectAdapterCommand
                    ?? (_selectAdapterCommand = new RelayCommand<int>(SelectAdapter));
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
        /// Gets the ClearLog.
        /// </summary>
        public RelayCommand ClearLogCommand
        {
            get
            {
                return _clearLogCommand
                    ?? (_clearLogCommand = new RelayCommand(
                    () =>
                    {
                        LogEntries.Clear();
                    }));
            }
        }

        /// <summary>
        /// Saves the log data.
        /// </summary>
        public RelayCommand SaveLogCommand
        {
            get
            {
                return _saveLogCommand
                    ?? (_saveLogCommand = new RelayCommand(
                    () =>
                    {
                        SaveLog();
                    }));
            }
        }

        /// <summary>
        /// Gets the ChangeBitRateCommand.
        /// </summary>
        public RelayCommand<int> ChangeBitRateCommand
        {
            get
            {
                return _changeBitRate
                    ?? (_changeBitRate = new RelayCommand<int>(ExecuteChangeBitRateCommand));
            }
        }

        public RelayCommand<int> ChangeDriveByZeroCommand
        {
            get
            {
                return _changeDriveByZero
                    ?? (_changeDriveByZero = new RelayCommand<int>(ExecuteChangeDriveByZeroCommand));
            }
        }

        #endregion

        #region Public Methods

        public void Cleanup()
        {
            CancelScriptCommand.Execute(new object());
        }

        #endregion

        #region Private Methods

        private async void SaveLog()
        {
            sfd.Filter = "CSV files (*.csv)|*.csv";
            if (sfd.ShowDialog() == true)
            {
                await Task.Run(() =>
                {
                    try
                    {
                        using (StreamWriter sw = File.CreateText(sfd.FileName))
                        {
                            foreach (var item in LogEntries)
                            {
                                sw.WriteLine("{0},{1},{2},{3},{4},{5},{6}",
                                    item.DateTime,
                                    item.ProtocolName,
                                    item.Direction,
                                    item.Address,
                                    item.Register,
                                    item.Length,
                                    item.Data);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Messenger.Default.Send(new CommunicationMessage(this, ex));
                    }
                });
            }
        }

        private void ExecuteChangeBitRateCommand(int bitRate)
        {
            if (bitRate == -1)
                return;
            _activeProtocol.BitRate = bitRate;
            BitRate = bitRate;
        }

        private void ExecuteChangeDriveByZeroCommand(int drive)
        {
            _activeProtocol.DriveByZero = drive;
            DriveByZero = _activeProtocol.DriveByZero;
        }

        private void SelectAdapter(int adapterIndex)
        {
            if (_adapters.Count() >= 1)
            {
                _activeAdapter = _adapters[adapterIndex];
                _protocols = _activeAdapter.GetAttachedProtocolObjects();

                if (_protocols.Count > 0)
                {
                    if (_protocols[0] is I2cProtocol)
                    {
                        ActiveProtocolType = ActiveProtocol.I2C;
                        _activeProtocol = _protocols[0] as I2cProtocol;
                    }
                    else if (_protocols[0] is PMBusProtocol)
                    {
                        ActiveProtocolType = ActiveProtocol.PMBus;
                        _activeProtocol = _protocols[0] as PMBusProtocol;
                    }
                    else if (_protocols[0] is VirtualProtocol)
                    {
                        ActiveProtocolType = ActiveProtocol.Virtual;
                        _activeProtocol = _protocols[0] as VirtualProtocol;
                    }
                    else
                    {
                        ActiveProtocolType = ActiveProtocol.None;
                        _activeProtocol = null;
                    }

                    // Activate the protocol
                    if (_activeProtocolType == ActiveProtocol.I2C)
                    {
                        var capabilities = ((II2c)_activeAdapter).I2cGetCapabilities();
                        BitRateDescriptions.Clear();
                        foreach (var item in capabilities.SupportedBitRatesLabels)
                        {
                            BitRateDescriptions.Add(item);
                        }
                        SupportsDriveByZero = capabilities.SupportsDriveByZero;

                        _protocols = _adapters[adapterIndex].GetAttachedProtocolObjects();
                        //_activeProtocol = _protocols.Last() as I2cProtocol;
                        BitRate = _activeProtocol.BitRate;
                    }
                    else if (_activeProtocolType == ActiveProtocol.PMBus)
                    {
                        var capabilities = ((IPMBus)_activeAdapter).I2cGetCapabilities();
                        BitRateDescriptions.Clear();
                        foreach (var item in capabilities.SupportedBitRatesLabels)
                        {
                            BitRateDescriptions.Add(item);
                        }
                        SupportsDriveByZero = capabilities.SupportsDriveByZero;

                        _protocols = _adapters[adapterIndex].GetAttachedProtocolObjects();
                        //_activeProtocol = _protocols.Last() as PMBusProtocol;
                        BitRate = _activeProtocol.BitRate;
                    }
                }
            }

            // TODO - If we include other protocols then we need to change this to a generic
            // protocol object and determine which protocol UI elements to populate.
            if (_activeProtocol != null)
            {
                // Set the I2cAddress in the window to match the current protocol address
                // I2cAddress = _activeProtocol.TargetAddress.ToString("X2");

                //code to set the Target address based on the active device selection change

                if (((DeviceAccess.Device)_device).Protocol.Name.ToUpper() == "I2C")
                {
                    I2cAddress = ((AdapterAccess.Protocols.I2cProtocol)((DeviceAccess.Device)_device).Protocol).TargetAddress.ToString("X2");
                }
                else if (((DeviceAccess.Device)_device).Adapter.AdapterName.ToUpper() == "VIRTUAL ADAPTER")
                {
                    if (((DeviceAccess.Device)_device).DeviceName == "PE26100")
                    {
                        ((AdapterAccess.Protocols.VirtualProtocol)((DeviceAccess.Device)_device).Protocol).TargetAddress = 50;
                    }

                    I2cAddress = ((AdapterAccess.Protocols.VirtualProtocol)((DeviceAccess.Device)_device).Protocol).TargetAddress.ToString("X2");
                }
                else
                {
                    I2cAddress = _activeProtocol.TargetAddress.ToString("X2");
                }

            }
        }

        private async void ExecuteExecuteScript(string parameter)
        {
            await Task.Run(() => RunScript(parameter));
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

                        // Pre-set the target address to the active adapter
                        byte addr = _activeProtocol.TargetAddress;

                        // Reassign the target address if it is specified in the script
                        if (script.SlaveAddress.HasValue)
                        {
                            addr = (byte)script.SlaveAddress;
                        }

                        for (int i = 0; i < transaction.RepeatCount; i++)
                        {
                            if (item.Operation == OperationType.Write)
                            {
                                WriteBlock(addr, transaction.Command, transaction.Data);
                            }
                            else
                            {
                                ReadBlock(addr, transaction.Command, transaction.Length);
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

        private void WriteBlock(byte slaveAddress, byte registerAddress, byte[] data)
        {
            string dt = DateTime.Now.ToString("hh:mm:ss:fff");

            try
            {
                if (_activeProtocol != null)
                {
                    _activeProtocol.TargetAddress = slaveAddress;

                    Log.Debug("AdapterControl - WriteBlock : " + "Data " + BitConverter.ToString(data));

                    _activeProtocol.WriteBlock(slaveAddress, registerAddress, data.Length, ref data);

                    Log.Debug("AdapterControl - WriteBlock : ActiveProtocol - " + _activeProtocol + " Device Address " + slaveAddress
                        + " Register Address " + registerAddress + " Data " + BitConverter.ToString(data));

                    byte[] reversedData = data.Reverse().ToArray();

                    string dataValue = BitConverter.ToString(data);

                    if (((DeviceAccess.Device)_device).Protocol.Name == "PMBus")
                    {
                        if (data.Length > 1)
                        {
                            dataValue = BitConverter.ToString(reversedData);
                        }
                    }
                    else if (((DeviceAccess.Device)_device).Protocol.Name == "I2C")
                    {
                        dataValue = BitConverter.ToString(data);
                    }
                    Dispatcher.BeginInvoke(
                      (Action)(() =>
                      {
                          LogEntries.Insert(0, new LogEntry()
                          {
                              DateTime = dt,
                              ProtocolName = _activeProtocolType.ToString(),
                              Direction = "W",
                              Address = slaveAddress.ToString("X2"),
                              Register = registerAddress.ToString("X2"),
                              Length = data.Length.ToString().PadLeft(2, '0'),
                              ForeColor = _writeColor,
                              Data = dataValue
                          });
                      }));
                }
            }
            catch (Exception ex)
            {

                Dispatcher.BeginInvoke(
                  (Action)(() =>
                  {
                      LogEntries.Insert(0, new LogEntry()
                      {
                          DateTime = dt,
                          ProtocolName = _activeProtocolType.ToString(),
                          Direction = "W",
                          Address = slaveAddress.ToString("X2"),
                          Register = registerAddress.ToString("X2"),
                          Length = "00",
                          ForeColor = _errorColor,
                          Data = ex.Message
                      });
                  }));
            }

            return;
        }

        private void Write16BitBlock(byte slaveAddress, ushort registerAddress, byte[] data)
        {
            string dt = DateTime.Now.ToString("hh:mm:ss:fff");

            try
            {
                if (_activeProtocol != null)
                {
                    _activeProtocol.TargetAddress = slaveAddress;

                    Log.Debug("AdapterControl - Write16BitBlock : " + "Data " + BitConverter.ToString(data));

                    if (_activeProtocol.Name.ToLower() == "virtual")
                    {
                        if (data != null)
                        {
                            int length = data.Length;
                            _activeProtocol.WriteBlock(registerAddress, length, ref data);
                        }
                    }
                    else
                    {
                        _activeProtocol.WriteBlock(slaveAddress, registerAddress, data.Length, ref data);
                    }

                    Log.Debug("AdapterControl - Write16BitBlock : ActiveProtocol - " + _activeProtocol + " Device Address " + slaveAddress
                            + " Register Address " + registerAddress + " Data " + BitConverter.ToString(data));

                    Dispatcher.BeginInvoke(
                      (Action)(() =>
                      {
                          LogEntries.Insert(0, new LogEntry()
                          {
                              DateTime = dt,
                              ProtocolName = _activeProtocolType.ToString(),
                              Direction = "W",
                              Address = slaveAddress.ToString("X2"),
                              Register = registerAddress.ToString("X3"),
                              Length = data.Length.ToString().PadLeft(2, '0'),
                              ForeColor = _writeColor,
                              Data = BitConverter.ToString(data)
                          });
                      }));
                }
            }
            catch (Exception ex)
            {

                Dispatcher.BeginInvoke(
                  (Action)(() =>
                  {
                      LogEntries.Insert(0, new LogEntry()
                      {
                          DateTime = dt,
                          ProtocolName = _activeProtocolType.ToString(),
                          Direction = "W",
                          Address = slaveAddress.ToString("X2"),
                          Register = registerAddress.ToString("X3"),
                          Length = "00",
                          ForeColor = _errorColor,
                          Data = ex.Message
                      });
                  }));
            }

            return;
        }
        private void ReadBlock(byte slaveAddress, ushort registerAddress, int length, int size = 0)
        {
            //string dt = DateTime.Now.ToString("yyyy-dd-MM hh:mm:ss:fff");
            string dt = DateTime.Now.ToString("hh:mm:ss:fff");

            try
            {
                if (_activeProtocol != null)
                {

                    byte[] data = new byte[length];
                    if (_activeProtocol.Name == "virtual")
                        _activeProtocol.deviceFileName = _activeDev.DeviceInfoName; //only for demo mode => RW when device selection
                    _activeProtocol.TargetAddress = slaveAddress;

                    if ((_activeDev.DeviceInfoName == "PE24103-R01" || _activeDev.DeviceInfoName == "PE24104-R01")
                       && ((DeviceAccess.Device)_device).Protocol.Name == "PMBus")
                    {
                        data = new byte[size];
                        _activeProtocol.ReadBlock(registerAddress, size, ref data, true);
                    }
                    else
                    {
                        _activeProtocol.ReadBlock(registerAddress, length, ref data, true);
                    }

                    byte[] reversedData = data.Reverse().ToArray();
                    string dataValue = BitConverter.ToString(reversedData);

                    if ((_activeDev.DeviceInfoName == "PE24103-R01" || _activeDev.DeviceInfoName == "PE24104-R01")
                        && ((DeviceAccess.Device)_device).Protocol.Name == "PMBus")
                    {
                        int expectedByteCount = length;
                        int currentbyteCount = reversedData.Length;

                        byte[] readData = new byte[1];
                        string regStartAddress = RegisterAddress;
                        ushort startAddress = registerAddress;
                        uint raddress = uint.Parse(startAddress.ToString());
                        string hyphen = "-";

                        for (int i = data.Length; i < length; i++)
                        {
                            if (currentbyteCount < expectedByteCount)
                            {
                                raddress = raddress + 1;
                                string currentRegAddress = raddress.ToString("X2");

                                int decValue = int.Parse(currentRegAddress, System.Globalization.NumberStyles.HexNumber);
                                byte address = Convert.ToByte(currentRegAddress, 16);
                                Register register = _register.Registers.FirstOrDefault(a => a.Address == raddress);

                                if (register != null)
                                {
                                    if (register != null)
                                    {
                                        int regSize = 0;

                                        bool isSuccess = int.TryParse(register.Size.ToString(), out regSize);

                                        if (regSize != 0)
                                        {
                                            readData = new byte[register.Size];
                                        }
                                        else
                                        {
                                            regSize = 1;
                                            readData = new byte[1];
                                        }

                                        _activeProtocol.ReadBlock((byte)register.Address, regSize, ref readData, true);
                                    }
                                }
                                else
                                {
                                    readData = new byte[1];
                                    readData[0] = 00;
                                }

                                byte[] reverseReadData = readData.Reverse().ToArray();
                                string dataToWrite = BitConverter.ToString(reverseReadData);

                                dataValue += string.Concat(hyphen, dataToWrite);
                                currentbyteCount += readData.Length;
                            }
                        }

                        dataValue = dataValue.Substring(0, ((length * 2) + (length - 1)));

                    }
                    else
                    {
                        Log.Debug("AdapterControl - ReadBlock : DataValue - " + dataValue + "Length - " + length);

                        if (((DeviceAccess.Device)_device).Protocol.Name == "PMBus")
                        {
                            if (length > 1)
                            {
                                dataValue = BitConverter.ToString(reversedData);
                            }
                        }
                        else if (((DeviceAccess.Device)_device).Protocol.Name == "I2C" ||
                            ((DeviceAccess.Device)_device).Protocol.Name.ToLower() == "virtual")
                        {
                            dataValue = BitConverter.ToString(data);
                        }

                    }
                    Log.Debug("AdapterControl - ReadBlock : ActiveProtocol - " + _activeProtocol + " Device Address " + slaveAddress
                            + " Register Address " + registerAddress + " Data " + dataValue);

                    Dispatcher.BeginInvoke(
                      (Action)(() =>
                      {
                          LogEntries.Insert(0, new LogEntry()
                          {
                              DateTime = dt,
                              ProtocolName = _activeProtocolType.ToString(),
                              Direction = "R",
                              Address = slaveAddress.ToString("X2"),
                              Register = registerAddress.ToString("X2"),
                              Length = length.ToString().PadLeft(2, '0'),
                              ForeColor = _readColor,
                              Data = dataValue
                          });
                      }));
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error in AdapterControl - ReadBlock : ", ex.StackTrace);
                Dispatcher.BeginInvoke(
                  (Action)(() =>
                  {
                      LogEntries.Insert(0, new LogEntry()
                      {
                          DateTime = dt,
                          ProtocolName = _activeProtocolType.ToString(),
                          Direction = "R",
                          Address = slaveAddress.ToString("X2"),
                          Register = registerAddress.ToString("X2"),
                          Length = "00",
                          ForeColor = _errorColor,
                          Data = ex.Message
                      });
                  }));
            }

        }
        private void ReadBlock(byte slaveAddress, byte registerAddress, int length, int size = 0)
        {
            //string dt = DateTime.Now.ToString("yyyy-dd-MM hh:mm:ss:fff");
            string dt = DateTime.Now.ToString("hh:mm:ss:fff");

            try
            {
                if (_activeProtocol != null)
                {

                    byte[] data = new byte[length];
                    if (_activeProtocol.Name == "virtual")
                        _activeProtocol.deviceFileName = _activeDev.DeviceInfoName;//only for demo mode => RW when device selection
                    _activeProtocol.TargetAddress = slaveAddress;

                    if ((_activeDev.DeviceInfoName == "PE24103-R01" || _activeDev.DeviceInfoName == "PE24104-R01")
                       && ((DeviceAccess.Device)_device).Protocol.Name == "PMBus")
                    {
                        data = new byte[size];
                        _activeProtocol.ReadBlock(registerAddress, size, ref data, true);
                    }
                    else
                    {
                        _activeProtocol.ReadBlock(registerAddress, length, ref data, true);
                    }

                    byte[] reversedData = data.Reverse().ToArray();
                    string dataValue = BitConverter.ToString(reversedData);

                    if ((_activeDev.DeviceInfoName == "PE24103-R01" || _activeDev.DeviceInfoName == "PE24104-R01")
                        && ((DeviceAccess.Device)_device).Protocol.Name == "PMBus")
                    {
                        int expectedByteCount = length;
                        int currentbyteCount = reversedData.Length;

                        byte[] readData = new byte[1];
                        string regStartAddress = RegisterAddress;
                        byte startAddress = registerAddress;
                        uint raddress = uint.Parse(startAddress.ToString());
                        string hyphen = "-";

                        for (int i = data.Length; i < length; i++)
                        {
                            if (currentbyteCount < expectedByteCount)
                            {
                                raddress = raddress + 1;
                                string currentRegAddress = raddress.ToString("X2");

                                int decValue = int.Parse(currentRegAddress, System.Globalization.NumberStyles.HexNumber);
                                byte address = Convert.ToByte(currentRegAddress, 16);
                                Register register = _register.Registers.FirstOrDefault(a => a.Address == raddress);

                                if (register != null)
                                {
                                    if (register != null)
                                    {
                                        int regSize = 0;

                                        bool isSuccess = int.TryParse(register.Size.ToString(), out regSize);

                                        if (regSize != 0)
                                        {
                                            readData = new byte[register.Size];
                                        }
                                        else
                                        {
                                            regSize = 1;
                                            readData = new byte[1];
                                        }

                                        _activeProtocol.ReadBlock((byte)register.Address, regSize, ref readData, true);
                                    }
                                }
                                else
                                {
                                    readData = new byte[1];
                                    readData[0] = 00;
                                }

                                byte[] reverseReadData = readData.Reverse().ToArray();
                                string dataToWrite = BitConverter.ToString(reverseReadData);

                                dataValue += string.Concat(hyphen, dataToWrite);
                                currentbyteCount += readData.Length;
                            }
                        }

                        dataValue = dataValue.Substring(0, ((length * 2) + (length - 1)));

                    }
                    else
                    {
                        Log.Debug("AdapterControl - ReadBlock : DataValue - " + dataValue + "Length - " + length);

                        if (((DeviceAccess.Device)_device).Protocol.Name == "PMBus")
                        {
                            if (length > 1)
                            {
                                dataValue = BitConverter.ToString(reversedData);
                            }
                        }
                        else if (((DeviceAccess.Device)_device).Protocol.Name == "I2C" ||
                            ((DeviceAccess.Device)_device).Protocol.Name.ToLower() == "virtual")
                        {
                            dataValue = BitConverter.ToString(data);
                        }

                    }
                    Log.Debug("AdapterControl - ReadBlock : ActiveProtocol - " + _activeProtocol + " Device Address " + slaveAddress
                            + " Register Address " + registerAddress + " Data " + dataValue);

                    Dispatcher.BeginInvoke(
                      (Action)(() =>
                      {
                          LogEntries.Insert(0, new LogEntry()
                          {
                              DateTime = dt,
                              ProtocolName = _activeProtocolType.ToString(),
                              Direction = "R",
                              Address = slaveAddress.ToString("X2"),
                              Register = registerAddress.ToString("X2"),
                              Length = length.ToString().PadLeft(2, '0'),
                              ForeColor = _readColor,
                              Data = dataValue
                          });
                      }));
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error in AdapterControl - ReadBlock : ", ex.StackTrace);
                Dispatcher.BeginInvoke(
                  (Action)(() =>
                  {
                      LogEntries.Insert(0, new LogEntry()
                      {
                          DateTime = dt,
                          ProtocolName = _activeProtocolType.ToString(),
                          Direction = "R",
                          Address = slaveAddress.ToString("X2"),
                          Register = registerAddress.ToString("X2"),
                          Length = "00",
                          ForeColor = _errorColor,
                          Data = ex.Message
                      });
                  }));
            }

        }

        private void Read16BitBlock(byte slaveAddress, ushort registerAddress, int length)
        {
            //string dt = DateTime.Now.ToString("yyyy-dd-MM hh:mm:ss:fff");
            string dt = DateTime.Now.ToString("hh:mm:ss:fff");

            if (_activeProtocol != null)
            {
                try
                {
                    byte[] data = new byte[2];

                    _activeProtocol.TargetAddress = slaveAddress;

                    _activeProtocol.ReadBlock(registerAddress, length, ref data);

                    if (data.Length > 1)
                    {
                        Log.Debug("AdapterControl - Read16BitBlock : ActiveProtocol - " + _activeProtocol + " Device Address " + slaveAddress
                            + " Register Address " + registerAddress + " Data[0] " + data[0] + " Data[1] " + data[1] + " Data : " + BitConverter.ToString(data));
                    }
                    else
                    {
                        Log.Debug("AdapterControl - Read16BitBlock : ActiveProtocol - " + _activeProtocol + " Device Address " + slaveAddress
                            + " Register Address " + registerAddress + " Data[0] " + data[0] + " Data : " + BitConverter.ToString(data));
                    }

                    Dispatcher.BeginInvoke(
                      (Action)(() =>
                      {
                          LogEntries.Insert(0, new LogEntry()
                          {
                              DateTime = dt,
                              ProtocolName = _activeProtocolType.ToString(),
                              Direction = "R",
                              Address = slaveAddress.ToString("X2"),
                              Register = registerAddress.ToString("X3"),
                              Length = length.ToString().PadLeft(2, '0'),
                              ForeColor = _readColor,
                              Data = BitConverter.ToString(data)
                          });
                      }));
                }
                catch (Exception ex)
                {
                    Log.Error("Error in AdapterControl - Read16BitBlock : ", ex.StackTrace);

                    Dispatcher.BeginInvoke(
                      (Action)(() =>
                      {
                          LogEntries.Insert(0, new LogEntry()
                          {
                              DateTime = dt,
                              ProtocolName = _activeProtocolType.ToString(),
                              Direction = "R",
                              Address = slaveAddress.ToString("X2"),
                              Register = registerAddress.ToString("X3"),
                              Length = "000",
                              ForeColor = _errorColor,
                              Data = ex.Message
                          });
                      }));
                }
            }
        }

        private void HandleNotification(NotificationMessage message)
        {
            if (message.Notification == Notifications.CleanupNotification)
            {
                Messenger.Default.Unregister(this);
            }
        }

        #endregion

        #region INotificationPropertyChanged Interface

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion
    }
}
