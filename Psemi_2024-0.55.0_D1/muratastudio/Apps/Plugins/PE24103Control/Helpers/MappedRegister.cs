using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using HardwareInterfaces;
using DeviceAccess;
using System.Threading;
using System.Diagnostics;

namespace PE24103Control.Helpers
{
    public class MappedRegister : INotifyPropertyChanged
    {
        #region Config

        private int _config;
        public int Config
        {
            get
            {
                return _config;
            }

            set
            {
                if (_config == value)
                {
                    return;
                }

                _config = value;
                OnPropertyChanged("Config");
            }
        }

        #endregion

        #region Page

        private int _page;
        public int Page
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
                OnPropertyChanged("Page");
            }
        }

        #endregion

        #region DeviceSource

        private Device _device;
        public Device DeviceSource
        {
            get
            {
                return _device;
            }

            set
            {
                if (_device == value)
                {
                    return;
                }

                _device = value;
                OnPropertyChanged("DeviceSource");
            }
        }

        #endregion

        #region Register

        private Register _register;
        public Register RegisterSource
        {
            get
            {
                return _register;
            }

            set
            {
                if (_register == value)
                {
                    return;
                }

                _register = value;
                OnPropertyChanged("RegisterSource");
            }
        }

        #endregion

        #region Bit

        private Register.Bit _bit;
        public Register.Bit Bit
        {
            get
            {
                return _bit;
            }

            set
            {
                if (_bit == value)
                {
                    return;
                }

                _bit = value;
                OnPropertyChanged("Bit");
            }
        }

        #endregion

        #region Value

        private double _value;
        public double Value
        {
            get
            {
                return _value;
            }

            set
            {
                if (_value == value)
                {
                    return;
                }

                _value = value;
                OnPropertyChanged("Value");
            }
        }

        #endregion

        #region RawValue

        private string _rawValue = "0";
        public string RawValue
        {
            get
            {
                return _rawValue;
            }

            set
            {
                if (_rawValue == value)
                {
                    return;
                }

                _rawValue = value;
                OnPropertyChanged("RawValue");
            }
        }

        #endregion

        #region BitValue

        private bool _bitValue;
        public bool BitValue
        {
            get
            {
                return _bitValue;
            }

            set
            {
                if (_bitValue == value)
                {
                    return;
                }

                _bitValue = value;
                OnPropertyChanged("BitValue");
            }
        }

        #endregion

        #region Mask

        private ushort _mask;
        public ushort Mask
        {
            get
            {
                return _mask;
            }

            set
            {
                if (_mask == value)
                {
                    return;
                }

                _mask = value;
                OnPropertyChanged("Mask");
            }
        }

        #endregion

        #region Units

        private string _units;
        public string Units
        {
            get
            {
                return _units;
            }

            set
            {
                if (_units == value)
                {
                    return;
                }

                _units = value;
                OnPropertyChanged("Units");
            }
        }

        #endregion

        #region IsError

        private bool _isError;
        public bool IsError
        {
            get
            {
                return _isError;
            }

            set
            {
                if (_isError == value)
                {
                    return;
                }

                _isError = value;
                OnPropertyChanged("IsError");
            }
        }

        #endregion

        #region IsReadOnly

        private bool _isReadOnly;
        public bool IsReadOnly
        {
            get
            {
                return _isReadOnly;
            }

            set
            {
                if (_isReadOnly == value)
                {
                    return;
                }

                _isReadOnly = value;
                OnPropertyChanged("IsReadOnly");
            }
        }

        #endregion

        #region PageDelay

        private int _pageDelay = 400;
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

                _pageDelay = value;
                OnPropertyChanged("PageDelay");
            }
        }

        #endregion

        public void Read(bool setPage = false)
        {
            try
            {
                IsError = false;

                if (setPage)
                {
                    DeviceSource.WriteByte(0, (byte)Page);
                    System.Diagnostics.Debug.Print(string.Format("PMBus W: {0}-{1}", "PAGE", Page));
                    Thread.Sleep(_pageDelay);
                }

                double value = 0;
                DeviceSource.ReadRegisterValue(RegisterSource, ref value);
                System.Diagnostics.Debug.Print(string.Format("PMBus R: {0}-{1}", RegisterSource.DisplayName, value));
                RawValue = ((int)value).ToString();
                BitValue = Bit != null ? Bit.LastReadValue : false;
                Value = value;
            }
            catch (Exception)
            {
                IsError = true;
                throw;
            }
        }

        public void Write(ref byte page, string value)
        {
            try
            {
                IsError = false;

                if (page != Page)
                {
                    page = (byte)Page;
                    DeviceSource.WriteByte(0, page);
                    System.Diagnostics.Debug.Print(string.Format("PMBus W: {0}-{1}", "PAGE", page));
                    Thread.Sleep(_pageDelay);
                }

                if (Mask == 0)
                {
                    DeviceSource.WriteRegisterValue(RegisterSource, double.Parse(value));
                    System.Diagnostics.Debug.Print(string.Format("PMBus W: {0}-{1}", RegisterSource.DisplayName, value));
                }
                else
                {
                    int val = int.Parse(value);
                    val = GetMaskedValue((int)Mask, val, (int)(RegisterSource.Size * 8));
                    int orig = int.Parse(RawValue);

                    if (val == orig)
                        return;

                    int x = (orig & ~Mask) | (val & Mask);
                    DeviceSource.WriteRegisterValue(RegisterSource, (double)x);
                    System.Diagnostics.Debug.Print(string.Format("PMBus W: {0}-{1}", RegisterSource.DisplayName, x));
                }

                Thread.Sleep(1);
                double ret = 0;
                DeviceSource.ReadRegisterValue(RegisterSource, ref ret);
                System.Diagnostics.Debug.Print(string.Format("PMBus R: {0}-{1}", RegisterSource.DisplayName, ret));
                RawValue = ((int)ret).ToString();
                Value = ret;
            }
            catch (Exception)
            {
                IsError = true;
                throw;
            }
        }

        private int GetMaskedValue(int mask, int value, int size)
        {
            int i = 0;
            for (i = 0; i < size; i++)
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

        #region INotifyPropertyChanged implementation

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            if (PropertyChanged != null)
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
