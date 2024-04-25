using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using AdapterAccess.Protocols;
using System.Xml.Linq;
using HardwareInterfaces;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Text.RegularExpressions;

namespace AdapterAccess.Adapters
{
	public class PMBobAdapter : Adapter, IAdapter, IPMBus
	{
		public delegate void OnErrorHandler(string message);
		public event OnErrorHandler OnError;

		#region Private Members
        private SerialPort _serialPort;
        private PMBobDeviceInfo _adapterInfo;
		private PMBusCapabilities _pmbusCapabilities;
        private PMBobConfiguration _config;
        private string _status;
		private List<string> _pluginCompatibility = new List<string>();
		private bool _isScanAllAddresses;
		private byte _defaultSlaveAddress;
        public const decimal _minFw = 3.1m;

		/// <summary>
		/// Used to synchronize the low level driver "scan" functionality.
		/// </summary>
		static object _getAllAdaptersLock = new object();

		#endregion

		#region Constructors

        internal PMBobAdapter(PMBobDeviceInfo adapterInfo)
		{
			_supportedProtocols.Add(new ProtocolInfo(typeof(PMBusProtocol), "PMbus", true, true));
            this._adapterInfo = adapterInfo;
            this._serialPort = new SerialPort();

            // Register for the events
            this._serialPort.DataReceived += _serialPort_DataReceived;
            this._serialPort.ErrorReceived += _serialPort_ErrorReceived;

			ConfigureDefaultAdapterSettings();
		}

        void _serialPort_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
        }

        void _serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
        }

		#endregion

		#region Overrides

		public string AdapterName
		{
            get { return _adapterInfo.Description; }
		}

		public string AdapterVersion
		{
			get { return _adapterInfo.FWVersion; }
		}

		public string AdapterSerialNumber
		{
			get { return _adapterInfo.SerialNumber; }
		}

		public List<string> PluginCompatibility
		{
			get { return _pluginCompatibility; }

			internal set
			{
				_pluginCompatibility = value;
			}
		}

		public override bool IsInterfaceSupported(Type interfaceType)
		{
			if (interfaceType == typeof(IPMBus))
			{
				return true;
			}
			return false;
		}

		public override string ToString()
		{
			return _adapterInfo.Description + " - S/N: " + _adapterInfo.SerialNumber;
		}

        public List<byte> SlaveAddresses
        {
            get { return _adapterInfo.SlaveAddresses; }
        }

		#endregion

		#region Open and Close

		protected override bool OpenDriverInterface()
		{
            try
            {
                if (!this._serialPort.IsOpen)
                {
                    this._serialPort.Open();
                    this._isOpened = true;
                }

                return true;
            }
            catch (Exception)
            {                
                throw;
            }
		}

		protected override void CloseDriverInterface()
		{
            try
            {
                if (this._serialPort.IsOpen)
                {
                    this._serialPort.DataReceived -= _serialPort_DataReceived;
                    this._serialPort.ErrorReceived -= _serialPort_ErrorReceived;
                    this._serialPort.Close();
                    this._serialPort.Dispose();
                }
            }
            catch (Exception)
            {                
                throw;
            }
		}

        public bool ReconfigureInterface(PMBobConfiguration config)
		{
			_config = config;
			return ReconfigureInterface();
		}

		protected override bool ReconfigureInterface()
		{
			_status = Initialize();

			if (_status != "no faults")
			{
				if (OnError != null)
				{
					OnError(_status);
				}
				return false;
			}
			return true;
		}

        private string Initialize()
        {
            int pec = _config.IsPEC ? 0 : 1;
            _serialPort.WriteLine(string.Format("nopec[{0}]", pec));
            _serialPort.WriteLine(string.Format("scl[{0}]", _config.GetBitRate()));
            return GetStatus();
        }

        private string GetStatus()
        {
            _serialPort.DiscardInBuffer();
            _serialPort.WriteLine("status");
            return GetString(_serialPort);
        }

		#endregion

		#region PMBus Interface

		public void I2cSetCapabilities(PMBusCapabilities config)
		{
			_pmbusCapabilities = config;
		}

		public PMBusCapabilities I2cGetCapabilities()
		{
			return _pmbusCapabilities;
		}

		public PMBusConfiguration I2cGetConfiguration()
		{
			return new PMBusConfiguration
			{
				BitRate = _config != null ? _config.BitRate : 0
			};
		}

		public bool I2cSetBitRate(int bitRate)
		{
			_config.BitRate = bitRate;
			return ReconfigureInterface();
		}

        public bool I2cSetPec(bool pec)
        {
            _config.IsPEC = pec;
            return ReconfigureInterface();
        }

		public bool I2cSetDriveByZero(int drive)
		{			
			return ReconfigureInterface();
		}

		public void I2cSetConfiguration(PMBusConfiguration config)
		{
			_isScanAllAddresses = config.ScanAllAddresses;
			_defaultSlaveAddress = config.SlaveAddress;
		}

		public void I2cEnable()
		{
		}

		public void I2cDisable()
		{
		}

		public void I2cSendByte(byte targetAddress, byte data)
		{
			byte[] bdata = new byte[] { data };
			I2cWriteBlock(targetAddress, null, 1, ref bdata);
		}

		public void I2cWriteByte(byte targetAddress, byte address, byte data)
		{
			byte[] bdata = new byte[] { data };
			I2cWriteBlock(targetAddress, address, 1, ref bdata);
		}

		public void I2cWriteBlock(byte targetAddress, byte? address, int length, ref byte[] data)
		{
			if (CheckAdapterPresents())
			{
				// Determine if the write is a BYTE, WORD or BLOCK
				byte[] writeData = null;
				uint writeLength = (uint)length;
				if (length > 2)
				{
					writeLength = (uint)length + 1;
					writeData = new byte[length + 1];
					Array.Copy(data, 0, writeData, 1, length);
					writeData[0] = (byte)writeLength;
				}
				else
				{
					writeData = new byte[length];
					Array.Copy(data, writeData, length);
				}

                string write = string.Empty;
                if (address.HasValue)
                {
                    write = string.Format("awr{{{0}}}[0]{{{2}}}", targetAddress.ToString("X2"), length, address.Value.ToString("X2"));
                    foreach (var b in writeData)
                    {
                        write += string.Format("{{{0}}}", b.ToString("X2"));
                    }
                }
                else
                {

                }

                _serialPort.DiscardOutBuffer();
                _serialPort.WriteLine(write);
                _status = GetString(_serialPort);
				data = writeData;

				if (_status != "no faults")
				{
					if (OnError != null)
					{
						OnError(_status);
					}

					throw new Exception(_status);
				}
			}
		}

		public byte I2cReadByte(byte targetAddress, byte address)
		{
			byte[] data = new byte[1];
			I2cReadBlock(targetAddress, address, 1, ref data);
			return data[0];
		}

		public void I2cReadBlock(byte targetAddress, byte address, int length, ref byte[] data)
		{
			if (CheckAdapterPresents())
			{
                try
                {
                    _serialPort.DiscardInBuffer();
                    _serialPort.WriteLine(string.Format("awr{{{0}}}[{1}]{{{2}}}", targetAddress.ToString("X2"), length, address.ToString("X2")));
                    data = GetBytes(_serialPort);
                }
                catch (Exception ex)
                {
					if (OnError != null)
					{
						OnError(ex.ToString());
					}

					throw new Exception(ex.ToString());
                }
			}
			else
			{
				throw new Exception("Adapter Closed!");
			}
		}

		public void I2cSendStop()
		{
			return;
		}

		public bool IsScanAllAddresses 
		{
			get { return _isScanAllAddresses; } 
		}

		public byte DefaultSlaveAddress 
		{
			get { return _defaultSlaveAddress; }
		}

		public void SetGpio(byte value)
		{
		}

        public bool DoesFwMeetRequirement()
        {
            try
            {
                int indexOfV = _adapterInfo.FWVersion.IndexOf('V');
                int indexOfR = _adapterInfo.FWVersion.IndexOf('R');
                var fw = int.Parse(_adapterInfo.FWVersion.Substring(indexOfV + 1, 2));
                var rev = int.Parse(_adapterInfo.FWVersion.Substring(indexOfR + 1, 2));
                var val = string.Format("{0}.{1}", fw, rev);
                decimal fwversion = decimal.Parse(val);
                return fwversion >= _minFw;
            }
            catch (Exception)
            {
                throw new Exception("Cannot parse PMBob FW Version.");
            }
        }

		#endregion

		#region Enumeration

        internal static List<PMBobDeviceInfo> GetAllHardwareAdapters()
		{
            var adapters = new List<PMBobDeviceInfo>();

			lock (_getAllAdaptersLock)
			{
                string[] ports = SerialPort.GetPortNames();

                foreach (var port in ports)
                {
                    try
                    {
                        SerialPort sp = new SerialPort(port, 19200, Parity.None, 8, StopBits.One);
                        if (!sp.IsOpen)
                        {
                            sp.ReadTimeout = 500;
                            sp.Open();
                            sp.WriteLine("*idn?");
                            Thread.Sleep(100);
                            var status = sp.ReadExisting();

                            if (status.Contains("PMBob"))
                            {
                                sp.DiscardInBuffer();
                                sp.WriteLine("list");
                                byte[] addresses = GetBytes(sp);

                                string[] result = status.Split(',');
                                var mfg = result[0].Trim().Remove(0, 1);
                                var desc = result[1].Trim();
                                var sn = result[2].Trim();

                                string[] versions = result[3].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                var hw = versions[0].Trim();
                                var fw = versions[1].Trim();
                                var bl = versions[2].Trim().Split('\n')[0];

                                adapters.Add(new PMBobDeviceInfo
                                    {
                                        MFG = mfg,
                                        Description = desc,
                                        SerialNumber = sn,
                                        HWVersion = hw,
                                        FWVersion = fw,
                                        BLVersion = bl,
                                        ComPort = port,
                                        SlaveAddresses = addresses.ToList()
                                    });
                            }

                            sp.Close();
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
			}

			return adapters;
		}

        internal PMBobDeviceInfo GetAdapterInfo()
		{
			return this._adapterInfo;
		}

        internal bool Resync(PMBobDeviceInfo pmBobInfo)
		{
			return true;
		}

		#endregion

		#region Private Methods

        private static byte[] GetBytes(SerialPort sp)
        {
            Thread.Sleep(25);
            string pmbobData = sp.ReadExisting();

            Regex r = new Regex(@"{(.+?)}");
            MatchCollection mc = r.Matches(pmbobData);

            byte[] data = new byte[mc.Count];

            for (int i = 0; i < mc.Count; i++)
            {
                data[i] = Convert.ToByte(mc[i].Groups[1].Value, 16);
            }

            return data;
        }

        private static string GetString(SerialPort sp)
        {
            Thread.Sleep(20);
            string pmbobData = sp.ReadExisting();

            Regex r = new Regex(@"\0(.+?)\n");
            MatchCollection mc = r.Matches(pmbobData);

            if (mc.Count == 0)
                throw new Exception(pmbobData);

            return mc[0].Groups[1].Value;
        }

		private bool CheckAdapterPresents()
		{
            return _serialPort.IsOpen;
		}

		private void ConfigureDefaultAdapterSettings()
		{
            _config = new PMBobConfiguration
            {
                BitRate = 0,
                IsPEC = false
            };
            _serialPort.PortName = this._adapterInfo.ComPort;
            _serialPort.BaudRate = 19200;
            _serialPort.Parity = Parity.None;
            _serialPort.DataBits = 8;
            _serialPort.StopBits = StopBits.One;
            _serialPort.ReadTimeout = 500;
            _serialPort.WriteTimeout = 500;
		}

		#endregion
	}

    public class PMBobDeviceInfo
    {
        public string ComPort { get; set; }

        public string SerialNumber { get; set; }

        public string FWVersion { get; set; }

        public string HWVersion { get; set; }

        public string BLVersion { get; set; }

        public string Description { get; set; }

        public string MFG { get; set; }

        public List<byte> SlaveAddresses { get; set; }

        public bool Claimed { get; set; }
    }

    public class PMBobConfiguration
    {
        public int BitRate { get; set; }

        public int GetBitRate()
        {
            switch (BitRate)
            {
                case 0: return 100;
                case 1: return 400;
                default: return 100;
            }
        }
        public bool IsPEC { get; set; }
    }

    public enum PMStatus
    {
        NoFault = 0,
        I2CMasterTimeout = 1,
        I2CMaster_SlaveNACKedAddress = 2,
        I2CMaster_SlaveNACKedData = 3,
        I2CMaster_PECErrorReceived  = 4
    }
}
