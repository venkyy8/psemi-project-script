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
using System.Runtime;
using System.ComponentModel;
using GalaSoft.MvvmLight.Command;
using System.Diagnostics;
using System.Timers;
using System.Collections;
using DeviceAccess;
using AdapterAccess.Protocols;
using System.IO;
using System.Xml.Serialization;
using Microsoft.Win32;
using System.Runtime.CompilerServices;
using System.Windows;
using Serilog;

namespace RegisterControl.ViewModel
{
    public class PluginViewModel : ViewModelBase, INotifyPropertyChanged
    {
        #region Private Members

        private Device _d;
        public IDevice _device;
        private IRegister _register;
        private bool _isInternalMode;
        private bool _isMasterTestRegisterON;
        private bool _isMasterTestRegisterOFF;
        private bool _isEditableMTP;

        private string _path;
        private string _pageInfo;
        private ObservableCollection<Register> _registers;

        private RelayCommand _copyAsCsvCommand;

        private ObservableCollection<MTPRegister> _pageRegisters;
        private ObservableCollection<string> _pages;
        private ObservableCollection<MTPRWRegister> _eraseRegisters;
        private ObservableCollection<MTPRegister> _pageRWRegisters;
        private ObservableCollection<MTPRWRegister> _programRegisters;
        private ObservableCollection<MTPRegister> _mtpRegisters;
        private ObservableCollection<MTPRegister> _pageMTPRegisters;
        private RelayCommand<Register.Bit> _toggleBitCommand;
        private RelayCommand<object> _writeRegisterCommand;
        public bool _loaded;
        private CustomizeBase _deviceBase;
        private string _adapterName;

        #endregion

        #region Properties

        public ObservableCollection<Register> Registers
        {
            get { return _registers; }
        }

        public bool IsMasterTestRegisterON
        {
            get
            {
                return _isMasterTestRegisterON;
            }
            set
            {
                if (value != _isMasterTestRegisterON)
                {
                    _isMasterTestRegisterON = value;
                    OnPropertyChanged("IsMasterTestRegisterON");
                }
            }
        }

        public bool IsMasterTestRegisterOff
        {
            get
            {
                return _isMasterTestRegisterOFF;
            }
            set
            {
                if (value != _isMasterTestRegisterOFF)
                {
                    _isMasterTestRegisterOFF = value;
                    OnPropertyChanged("IsMasterTestRegisterOff");
                }
            }
        }

        public bool IsEditableMTP
        {
            get
            {
                return _isEditableMTP;
            }
            set
            {
                if (value != _isEditableMTP)
                {
                    _isEditableMTP = value;
                    OnPropertyChanged("IsEditableMTP");
                }
            }
        }

        public ObservableCollection<MTPRegister> MTPRegisters
        {
            get { return _mtpRegisters; }
            private set
            {
                _mtpRegisters = value;
                OnPropertyChanged("MTPRegisters");
            }
        }

        public ObservableCollection<MTPRWRegister> EraseRegisters
        {
            get
            {
                return _eraseRegisters;
            }
            set
            {
                if (value != _eraseRegisters)
                {
                    _eraseRegisters = value;
                    OnPropertyChanged("EraseRegisters");
                }
            }
        }

        public ObservableCollection<MTPRWRegister> ProgramRegisters
        {
            get
            {
                return _programRegisters;
            }
            set
            {
                if (value != _programRegisters)
                {
                    _programRegisters = value;
                    OnPropertyChanged("ProgramRegisters");
                }
            }
        }

        public ObservableCollection<MTPRegister> PageRegisters
        {
            get
            {
                return _pageRegisters;
            }
            set
            {
                if (value != _pageRegisters)
                {
                    _pageRegisters = value;
                    OnPropertyChanged("PageRegisters");
                }
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

        public ObservableCollection<MTPRegister> PageRWRegisters
        {
            get
            {
                return _pageRWRegisters;
            }
            set
            {
                if (value != _pageRWRegisters)
                {
                    _pageRWRegisters = value;
                    OnPropertyChanged("PageRWRegisters");
                }
            }
        }

        public ObservableCollection<MTPRegister> PageMTPRegisters
        {
            get
            {
                return _pageMTPRegisters;
            }
            set
            {
                if (value != _pageMTPRegisters)
                {
                    _pageMTPRegisters = value;
                    OnPropertyChanged("PageMTPRegisters");
                }
            }
        }

        public string MTPPath
        {
            get
            {
                return _path;
            }
            set
            {
                if (value != _path)
                {
                    _path = value;
                    OnPropertyChanged("MTPPath");
                }
            }
        }

        public string PageInfo
        {
            get
            {
                return _pageInfo;
            }
            set
            {
                if (value != _pageInfo)
                {
                    _pageInfo = value;
                    OnPropertyChanged("PageInfo");
                }
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

        #region Constructors

        public PluginViewModel()
        {

        }

        public PluginViewModel(object device, bool isInternalMode, bool is16BitAddressing = false)
         {
            _d = device as Device;
            _device = device as IDevice;
            _adapterName = _device.GetAdapterName();

            _register = device as IRegister;
            _isInternalMode = isInternalMode;
            _registers = new ObservableCollection<Register>();

            _pageRegisters = new ObservableCollection<MTPRegister>();
            _mtpRegisters = new ObservableCollection<MTPRegister>();

            //NEED TO REMOVE LATER
            PageRWRegisters = new ObservableCollection<MTPRegister>();
            PageRWRegisters.Add(new MTPRegister { MTPName = "", MTPAddress = "3D8", MTPValue = "AF9C" });
            PageRWRegisters.Add(new MTPRegister { MTPName = "", MTPAddress = "3D9", MTPValue = "7F54" });
            PageRWRegisters.Add(new MTPRegister { MTPName = "", MTPAddress = "3DA", MTPValue = "AF9C" });
            PageRWRegisters.Add(new MTPRegister { MTPName = "", MTPAddress = "3DB", MTPValue = "0012" });
            PageRWRegisters.Add(new MTPRegister { MTPName = "", MTPAddress = "3DC", MTPValue = "7F54" });
            PageRWRegisters.Add(new MTPRegister { MTPName = "", MTPAddress = "3DD", MTPValue = "AF9C" });
            PageRWRegisters.Add(new MTPRegister { MTPName = "", MTPAddress = "3DE", MTPValue = "7F54" });
            PageRWRegisters.Add(new MTPRegister { MTPName = "", MTPAddress = "3DF", MTPValue = "00C3" });

            EraseRegisters = new ObservableCollection<MTPRWRegister>();
            ProgramRegisters = new ObservableCollection<MTPRWRegister>();
            RegistersList = new ObservableCollection<Register>();

            Pages = new ObservableCollection<string>();
            PageInfo = "0X0F0";
            EraseRegisters.Add(new MTPRWRegister { SLNo = 1, Address = "3D3", Data = "00F0" });
            EraseRegisters.Add(new MTPRWRegister { SLNo = 2, Address = "3D1", Data = "F022" });
            ProgramRegisters.Add(new MTPRWRegister { SLNo = 1, Address = "3D3", Data = "00F0" });
            ProgramRegisters.Add(new MTPRWRegister { SLNo = 2, Address = "3D1", Data = "C012" });

            if (_device.DeviceInfoName == "PE24104-R01")
            {
                RegisterValues(EraseRegisters);
                RegisterValues(ProgramRegisters);
            }
            if(_device.DeviceInfoName == "PE26100")
            {
                IsPE26100 = true;
            }

            if (is16BitAddressing)
            {
                CheckMasterTestStatus();
            }
            if (IsPE26100 && _isInternalMode)
            {
                int masterRegVal = (int)_register.Registers.FirstOrDefault(S => S.ID == "MASTERTESTLB").LastReadValue;

                if ((_isInternalMode && masterRegVal.ToString("X") == "1") || masterRegVal.ToString("X") == "FF" || 
                    (_adapterName.ToUpper() == "VIRTUAL ADAPTER" && masterRegVal.ToString("X") == "96"))
                {
                    _register.Registers.ForEach(
                        r =>
                        {
                            if ((r.Size == 1 || r.Size == 2) && !r.Access.ToLower().Contains("block"))
                            {
                                _registers.Add(r);
                            }
                        });
                }
            }
            else
            {
                if(_isInternalMode)
                {
                    _register.Registers.ForEach(
                        r =>
                        {
                            if ((r.Size == 1 || r.Size == 2) && !r.Access.ToLower().Contains("block"))
                            {
                                _registers.Add(r);
                            }
                        }
                        );
                }
                else
                {
                    _register.Registers.ForEach(
                          r =>
                          {
                              if ((r.Size == 1 || r.Size == 2) && !r.Access.ToLower().Contains("block") && !r.Private)
                              {
                                  _registers.Add(r);
                              }
                          });
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

            Messenger.Default.Register<NotificationMessage>(this, HandleNotification);
        }

        #endregion

        #region MTPProgramming

        public bool BrowsePath()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "CSV files (*.csv)|*.csv";
            openFileDialog.InitialDirectory = @"C:\";
            if (openFileDialog.ShowDialog() == true)
            {
                MTPPath = openFileDialog.FileName;
                MTPRegisters.Clear();
                PageRegisters.Clear();
                _isEditableMTP = true;
                return true;
            }
            else
                return false;
        }

        public ObservableCollection<MTPRegister> LoadMTPRegisters()
        {
            try
            {
                ObservableCollection<MTPRegister> mtpRegisterList = new ObservableCollection<MTPRegister>();

                if (!String.IsNullOrEmpty(MTPPath))
                {
                    using (StreamReader sr = File.OpenText(MTPPath))
                    {
                        while (!sr.EndOfStream)
                        {
                            string[] inLine = sr.ReadLine().Split(',');

                            if ((inLine[0].StartsWith("confg")) || (inLine[0].StartsWith("config")) || (inLine[0] == "MFR_SPECIFIC_MATRIX"))
                            {
                                if (inLine[0] == "MFR_SPECIFIC_MATRIX")
                                {
                                    mtpRegisterList?.Add(new MTPRegister { MTPName = "MFR_SPECIFIC_MATRIX", MTPAddress = "0x0C0", MTPValue = "0x0FE4", IsEditable = true });
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

                                    mtpRegisterList?.Add(new MTPRegister { MTPName = inLine[0], MTPAddress = inLine[1], MTPValue = inLine[2], IsEditable = isEdit });
                                }

                            }
                            catch (Exception ex)
                            {
                                Log.Error("RegisterControl PluginViewModel : LoadMTPRegisters 1- " + ex);
                                EventLog.GetEventLogs(inLine[0]);
                            }
                        }

                        MTPRegisters = mtpRegisterList;

                        int count = 0;

                        foreach (var a in MTPRegisters)
                        {
                            if (count == 0 || count % 8 == 0)
                            {
                                Pages.Add(a.MTPAddress);
                            }
                            count++;
                        }
                    }

                    return MTPRegisters;
                }
            }
            catch (Exception ex)
            {
                Log.Error("RegisterControl PluginViewModel : LoadMTPRegisters 2 - " + ex);
                EventLog.GetEventLogs(ex.Message);
            }

            return null;
        }

        public void ReadPageValuesFromFile(string pageName, bool isReset = false, bool isGridUpdated = false, bool isBrowsedFile = false)
        {
            int count = 0;

            if (PageRegisters == null || PageRegisters.Count == 0 || isReset)
            {
                PageRegisters = new ObservableCollection<MTPRegister>();

                string rwRegister = (_device.DeviceInfoName == "PE24104-R01") ? "3C8" : "3D8";

                int intRWRegister = 0;

                foreach (MTPRegister register in MTPRegisters)
                {
                    if (register == null)
                    {
                        return;
                    }
                    if (String.IsNullOrEmpty(register.MTPValue))
                    {
                        register.MTPValue = string.Empty;
                    }

                    string mtpDefaultValue = (register.MTPValue.ToUpper().Contains("0X")) ? register.MTPValue.Substring(2) : register.MTPValue;

                    if (register.MTPAddress.ToLower() == pageName.ToLower())
                    {
                        count++;

                        if (count < 7)
                        {
                            PageRegisters.Add(new MTPRegister
                            {
                                MTPName = register.MTPName,
                                MTPAddress = register.MTPAddress,
                                MTPDefaultValue = mtpDefaultValue,
                                RWRegister = rwRegister,
                                IsEditable = register.IsEditable
                            });
                            continue;
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        if (count != 0)
                        {
                            count++;
                            intRWRegister = int.Parse(rwRegister, System.Globalization.NumberStyles.HexNumber) + 1;

                            string hexRWRegisterVal = intRWRegister.ToString("X");
                            rwRegister = hexRWRegisterVal;

                            PageRegisters.Add(new MTPRegister
                            {
                                MTPName = register.MTPName,
                                MTPAddress = register.MTPAddress,
                                MTPDefaultValue = mtpDefaultValue,
                                RWRegister = hexRWRegisterVal,
                                IsEditable = register.IsEditable
                            });

                            if (count > 7)
                            {
                                break;
                            }
                        }
                    }
                }
            }

            List<MTPRegister> nonMatchingMTPReg = new List<MTPRegister>();

            // update the present values of reg
            if (PageRegisters.Any())
            {
                foreach (var pageReg in PageRegisters)
                {
                    Register reg = Registers?.FirstOrDefault(a => a.DisplayName == pageReg.MTPName);

                    if(reg == null)
                    {
                        nonMatchingMTPReg.Add(pageReg);
                    }

                    pageReg.MTPValue = reg?.LastReadString;

                    if (reg?.LastReadString.ToString().Length % 2 != 0 || reg?.LastReadString.ToString().Length == 2)
                    {
                        pageReg.MTPValue = reg?.LastReadString.ToString().PadLeft(4, '0');
                    }
                    else
                    {
                        pageReg.MTPValue = reg?.LastReadString;
                    }
                }
            }

            if (nonMatchingMTPReg.Any() && isBrowsedFile)
            {
                MessageBox.Show("Address mismatch found in page registers and i2c registers. Please verify the input file.");
            }

            if (MTPRegisters.Any() && isGridUpdated)
            {
                bool isRegUpdated = false;

                foreach (var mTPRegister in MTPRegisters)
                {
                    var reg = Registers?.FirstOrDefault(a => a.DisplayName == mTPRegister.MTPName);
                    string newValue = string.Concat("0x", reg?.LastReadString.ToString().PadLeft(4, '0'));

                    if (reg?.LastReadString.ToString().Length % 2 != 0 || reg?.LastReadString.ToString().Length == 2)
                    {
                        if (!String.IsNullOrEmpty(reg?.LastReadString.ToString()))
                        {
                            if (newValue != mTPRegister.MTPValue)
                            {
                                mTPRegister.MTPValue = newValue;
                                isRegUpdated = true;
                            }
                        }

                    }
                    else
                    {
                        if (!String.IsNullOrEmpty(reg?.LastReadString))
                            mTPRegister.MTPValue = string.Concat("0x", reg?.LastReadString);
                    }
                }
                if (isRegUpdated)
                {
                    MessageBox.Show("MTP Registers are updated");
                }
            }
        }

        public void TurnOnMasterTestRegister()
        {
            //reg - 3FD

            string regId = "MASTERTEST";
            string hexValue = "A596";

            WriteToRegister(regId, hexValue);
        }

        public void TurnOffMasterTestRegister()
        {
            string regId = "MASTERTEST";
            string hexValue = "0000";

            WriteToRegister(regId, hexValue);
        }

        public void CheckMasterTestStatus()
        {
            string regId = "MASTERTEST";
            Register reg = _register?.GetRegister(regId);
            
            if (reg != null)
            {
                _register.ReadRegisterValue(reg);

                if (_adapterName == "Virtual Adapter" && reg.LastReadString == "A596")
                {
                    IsMasterTestRegisterON = true;
                    IsMasterTestRegisterOff = false;
                }
                else if (reg.LastReadString == "001")
                {
                    IsMasterTestRegisterON = true;
                    IsMasterTestRegisterOff = false;
                }
                else
                {
                    IsMasterTestRegisterOff = true;
                    IsMasterTestRegisterON = false;
                }
            }
        }

        public void SendClockSignal()
        {
            //reg - 29F

            string regId = "TRIMCLKDLYR";
            string hexValue = "55A0";

            WriteToRegister(regId, hexValue);
        }

        private void WriteToRegister(string regId, string hexValue)
        {
            if (String.IsNullOrEmpty(hexValue))
            {
                return;
            }

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
                Log.Error("RegisterControl PluginViewModel : WriteToRegister - Register : " + reg.DisplayName + "Data Sent : " + decValue + "Received Reg Data :" + reg.LastReadString + "Exception :" + e);

                throw new Exception("Reading Value from Register (" + reg.DisplayName + ")", e);
            }
        }

        public bool SendRegisterValues(ObservableCollection<MTPRWRegister> registers)
        {
            if (registers.Count > 0)
            {
                foreach (MTPRWRegister reg in registers)
                {
                    if (!String.IsNullOrEmpty(reg?.Address) && (!String.IsNullOrEmpty(reg?.Data)))
                    {
                        int decValue = int.Parse(reg.Address, System.Globalization.NumberStyles.HexNumber);
                        Register register = Registers?.FirstOrDefault(a => a.Address == decValue);
                        WriteToRegister(register?.ID, reg?.Data);
                    }
                    else
                    {
                        return false;
                    }
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        public void WriteMTPRegisterValues()
        {
            foreach (MTPRegister pageRegister in PageRegisters)
            {
                string address = (pageRegister.MTPAddress.ToUpper().Contains("0X")) ? pageRegister?.MTPAddress.Substring(2) : pageRegister?.MTPAddress;
                int decValue = int.Parse(address, System.Globalization.NumberStyles.HexNumber);

                Register register = Registers?.FirstOrDefault(a => a.Address == decValue);

                WriteToRegister(register?.ID, pageRegister?.MTPDefaultValue);
            }
        }

        public void WriteRWRegisterValues()
        {
            int i = 0;

            foreach (MTPRegister pageRWRegister in PageRWRegisters)
            {
                string address = (pageRWRegister.MTPAddress.ToUpper().Contains("0X")) ? pageRWRegister?.MTPAddress.Substring(2) : pageRWRegister?.MTPAddress;
                int decValue = int.Parse(address, System.Globalization.NumberStyles.HexNumber);

                Register register = Registers?.FirstOrDefault(a => a.Address == decValue);

                if (pageRWRegister?.MTPValue.Length % 2 != 0)
                {
                    pageRWRegister.MTPValue = pageRWRegister?.MTPValue.PadLeft(4, '0');
                }

                pageRWRegister.MTPDefaultValue = PageRegisters[i].MTPDefaultValue;
                i++;
                WriteToRegister(register?.ID, pageRWRegister?.MTPDefaultValue);
            }
        }

        public bool WriteRWRegistersFromMTP()
        {
            // Actual Device scenario - Writing to RW registers from where MTP Page registers will be overriden
            foreach (MTPRegister pageRWRegister in PageRegisters)
            {
                if (pageRWRegister?.RWRegister == null)
                {
                    return false;
                }

                int decValue = int.Parse(pageRWRegister.RWRegister, System.Globalization.NumberStyles.HexNumber);

                Register register = Registers?.FirstOrDefault(a => a.Address == decValue);
                WriteToRegister(register?.ID, pageRWRegister?.MTPDefaultValue);
            }

            // Demo Device scenario - Writing to MTP Page registers directly as no device exists. Need only for virtual device case

            if (_adapterName.ToUpper() == "VIRTUAL ADAPTER")
            {
                foreach (MTPRegister pageRWRegister in PageRegisters)
                {
                    var d = pageRWRegister.MTPAddress.Substring(2);

                    int decValue = int.Parse(pageRWRegister.MTPAddress.Substring(2), System.Globalization.NumberStyles.HexNumber);

                    Register register = Registers?.FirstOrDefault(a => a.Address == decValue);

                    int defaultValue = int.Parse(pageRWRegister.MTPDefaultValue, System.Globalization.NumberStyles.HexNumber);

                    _register.WriteRegister(register, defaultValue);
                    _register.ReadRegisterValue(register);
                }
            }

            return true;

        }

        #endregion

        #region MTP Programming PE24104

        public void RegisterValues(ObservableCollection<MTPRWRegister> Registers)
        {
            int count = 0;
            foreach (MTPRWRegister registers in Registers)
            {
                string address = ((registers.Address.ToUpper().Contains("0X")) ? registers?.Address.Substring(2) : registers?.Address).Replace('D', 'C');
                Registers[count].Address = address;
                count++;
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

        public RelayCommand<object> WriteRegisterCommand
        {
            get
            {
                return _writeRegisterCommand
                    ?? (_writeRegisterCommand = new RelayCommand<object>(ExecuteWriteRegisterCommand));
            }
        }

        public ObservableCollection<Register> RegistersList { get; set; }
        public bool IsPE26100 { get;  set; }

        #endregion

        #region Public Methods
        public void UpdateEraseAndProgamRegisters(string pageValue)
        {
            string hex = (pageValue.ToUpper().Contains("0X")) ? pageValue.Substring(2) : pageValue;
            EraseRegisters[0].Data = hex;
            ProgramRegisters[0].Data = hex;
        }
        public override void Cleanup()
        {
            base.Cleanup();
        }

        #endregion

        #region Private Methods

        private void ExecuteWriteRegisterCommand(object value)
        {
            ArrayList info = value as ArrayList;
            string id = (string)info[0];
            int val = (int)info[1];

            Register reg = _register.GetRegister(id);

            if (_loaded)
            {
                try
                {
                    if (reg.LastReadValueWithoutFormula != (double)val)
                    {
                        reg.LastReadValueWithoutFormula = val; // Small hack to change the binding so if the write fails the drop down selection will change back.
                        _register.WriteRegister(reg, val);
                        _register.ReadRegisterValue(reg);

                        if (_deviceBase != null)
                        {
                            _deviceBase.CheckDevice(null);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("RegisterControl PluginViewModel : ExecuteWriteRegisterCommand - Register : " + reg.DisplayName
                        + "Received Reg Data :" + reg.LastReadString + "Exception :" + ex);

                    MessengerSend(ex);
                }
            }
        }

        private void ExecuteToggleBitCommand(Register.Bit bit)
        {
            Register reg = _register.GetRegister(bit.RegisterID);

            int x = (((int)reg.LastReadValueWithoutFormula & (int)bit.Mask) == (int)bit.Mask) ? 0 : 1;

            Log.Information("RegisterControl : ExecuteToggleBitCommand . Bit : " + bit.DisplayName + " Corresponding Register : " + reg.DisplayName
                            + " OldValue : " + reg.LastReadString + " Old Value without formula : " + reg.LastReadValueWithoutFormula
                            + " Formula : " + reg.LoadFormula + " (High(1) / Low(0)) :" + x.ToString() + " ");

            try
            {
                _register.WriteRegisterBit(bit, (uint)x);
                
                if (bit.Name.ToUpper() == "RESET")
                {
                    // Give the device some time to reset
                    System.Threading.Thread.Sleep(100);

                    // Call the devices customized reset method
                    if (_deviceBase != null)
                    {
                        _deviceBase.ResetDevice(null);
                    }

                    // Notify the main GUI to read all of the registers
                    Messenger.Default.Send(new NotificationMessage(Notifications.ReadAllNotification));
                }
                else
                {
                    Log.Debug("RegisterControl : ExecuteToggleBitCommand  - Before reading register . Register value : " + reg.LastReadString
                        + " Register value without formula : " + reg.LastReadValueWithoutFormula);

                    _register.ReadRegisterValue(reg);

                    Log.Debug("RegisterControl : ExecuteToggleBitCommand  - After reading register . Register value : " + reg.LastReadString
                        + " Register value without formula : " + reg.LastReadValueWithoutFormula);

                    // Check the device for errors
                    if (_deviceBase != null)
                    {
                        _deviceBase.CheckDevice(null);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("RegisterControl PluginViewModel : ExecuteWriteRegisterCommand - Register : " + reg.DisplayName
                      + "Received Reg Data :" + reg.LastReadString + "Exception :" + ex);

                MessengerSend(ex);
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

        #endregion

        #region Event Handling

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

    public class MTPRegister : INotifyPropertyChanged
    {
        private string _address = "0x000";
        private string _name = "";
        private bool isEditable = false;
        private string _lastReadValue;
        private string _mtpDefaultValue;
        private string _rwRegister;

        public string MTPName
        {
            get { return _name; }

            internal set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged("MTPName");
                }
            }
        }

        public string MTPAddress
        {
            get { return _address; }

            internal set
            {
                if (_address != value)
                {
                    _address = value;
                    OnPropertyChanged("MTPAddress");
                }
            }
        }

        public string MTPValue
        {
            get { return _lastReadValue; }
            set
            {
                if (_lastReadValue != value)
                {
                    _lastReadValue = value;
                    OnPropertyChanged("MTPValue");
                }
            }
        }

        public string MTPDefaultValue
        {
            get { return _mtpDefaultValue; }
            set
            {
                if (_mtpDefaultValue != value)
                {
                    _mtpDefaultValue = value;
                    OnPropertyChanged("MTPDefaultValue");
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

    public class MTPRWRegister : INotifyPropertyChanged
    {
        private int _slno;
        private string _address = "0x000";
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
