using HardwareInterfaces;
using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace muRata
{
	public class AppManager
	{
		private int _triggeredReadDelayMs;
		private List<DeviceAccess.DeviceManager.DeviceInfo> _deviceInfo;
		private List<DeviceAccess.IdentityRegister> _identityRegisters;
		private List<DeviceAccess.IdentityRegisterTwoBytes> _identityRegistersTwoBytes;

		public List<DeviceAccess.DeviceManager.DeviceInfo> DeviceInfo
		{
			get { return _deviceInfo; }
			set { _deviceInfo = value; }
		}

		public List<DeviceAccess.IdentityRegister> IdentityRegisters
		{
			get { return _identityRegisters; }
			set { _identityRegisters = value; }
		}

		public List<DeviceAccess.IdentityRegisterTwoBytes> IdentityRegistersTwoBytes
		{
			get { return _identityRegistersTwoBytes; }
			set { _identityRegistersTwoBytes = value; }
		}

		public int TriggeredReadDelayMs
		{
			get { return _triggeredReadDelayMs; }
			set { _triggeredReadDelayMs = value; }
		}

		public AppManager()
		{
			this._deviceInfo = new List<DeviceAccess.DeviceManager.DeviceInfo>();
			this._identityRegisters = new List<DeviceAccess.IdentityRegister>();
			this._identityRegistersTwoBytes = new List<DeviceAccess.IdentityRegisterTwoBytes>();
		}
	}
}
