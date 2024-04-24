using AdapterAccess;
using AdapterAccess.Adapters;
using AdapterAccess.Protocols;
using HardwareInterfaces;
using Ionic.Zip;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Xml.XPath;

namespace DeviceAccess
{
	public class DeviceManager
	{
		#region Private Member Variables

		private AdapterManager _adapterManager = null;
		private List<byte> _deviceSlaveAddresses;
		private List<DeviceInfo> _deviceInfo;
		private List<IdentityRegister> _identityRegisters;
		private List<IdentityRegisterTwoBytes> _identityRegistersTwoBytes;

        private bool _is16BitI2C;
		private bool _isMTADevice;   
		
		private const string DEFAULT_DEVICE = "MPQ8645P-R01";
		private List<byte> _targetAddresses_6100;

		#endregion

		#region Constructor

		public DeviceManager(AdapterManager adapterManager, bool isAuthenticated, List<DeviceInfo> deviceInfo, List<IdentityRegister> identityRegisters, List<IdentityRegisterTwoBytes> identityRegisterTwoBytes)
		{
			IsAuthenticated = isAuthenticated;
			// Save the adapter manager
			_adapterManager = adapterManager;

			_deviceInfo = deviceInfo;

			_identityRegisters = identityRegisters;

			_identityRegistersTwoBytes = identityRegisterTwoBytes;
		}

		#endregion

		#region Public Readonly Fields

		public readonly bool IsAuthenticated = false;
		//public bool Is16BitI2CMode = false;

		#endregion

		#region Public Properties

		public bool Is16BitAddressing { get; set; }

		public bool IsAnMTADevice 
		{
			get { return _isMTADevice; }
			private set { _isMTADevice = value; }
		}

		public bool IsPE24103PMBusDevice { get; internal set; }

		public List<byte> TargettAddressesList
		{
			get { return _targetAddresses_6100; }
			private set { _targetAddresses_6100 = value; }
		}

		public List<IdentityRegister> IdentityRegisters
		{
			get { return _identityRegisters; }
			private set { _identityRegisters = value; }
		}

		public List<IdentityRegisterTwoBytes> IdentityRegistersTwoBytes
		{
			get { return _identityRegistersTwoBytes; }
			private set { _identityRegistersTwoBytes = value; }
		}

		public List<DeviceInfo> DeviceInformation
		{
			get { return _deviceInfo; }
			private set { _deviceInfo = value; }
		}

		#endregion

		#region Create and Enumerate Devices

		/// <summary>
		/// Returns a list of all of the devices attached to a specific adapter.
		/// </summary>
		/// <param name="adapter">Adapter object.</param>
		/// <returns>List of devices.</returns>
		public List<Device> GetAllDevices(Adapter adapter,
            byte? forcedI2cAddress,
            string optionalDeviceInfoName = "",
            bool isSilent = false,
            bool isDeviceHidden = false,
            bool includeI2c = true,
            bool includePmbus = true, bool isMTADevice = false)
		{
			List<Device> devices = new List<Device>();
			bool is16BitAddressing = false;
			IsAnMTADevice = isMTADevice;


			if (adapter != null)
			{
				#region PMBus

				// Loop through all interfaces on this adapter
				if (adapter is IPMBus && includePmbus)
				{
					IPMBus pmbusAdapter = adapter as IPMBus;
					_deviceSlaveAddresses = new List<byte>();

					// If the user is forcing an I2c address
					if (forcedI2cAddress.HasValue)
					{
						_deviceSlaveAddresses.Add((byte)forcedI2cAddress);
					}
					else
					{

                        if (pmbusAdapter is PMBobAdapter)
                        {
                            var pmbob = pmbusAdapter as PMBobAdapter;
                            _deviceSlaveAddresses = pmbob.SlaveAddresses;
                        }
                        else
                        {
                            // Otherwise add range to scan else use the default address
                            if (pmbusAdapter.IsScanAllAddresses && !isSilent)
                            {
                                // PMBus address range for muRata devices
                                for (int i = 0x30; i < 0x7F; i++)
                                {
                                    _deviceSlaveAddresses.Add((byte)i);
                                }
                            }
                            else
                            {
                                _deviceSlaveAddresses.Add(pmbusAdapter.DefaultSlaveAddress);
                            }
                        }
					}

                    // Must be at least one address
                    PMBusProtocol pmbus = null;
                    if (_deviceSlaveAddresses.Count > 0)
                    {
                        // Create I2C Protocol object pointing to the first address to scan
                        pmbus = new PMBusProtocol(_deviceSlaveAddresses[0], pmbusAdapter.I2cGetConfiguration().BitRate, adapter, AdapterMode.Shared);

                        // Turn on all gpio lines
                        pmbus.SetGpio(0xF0);

                        foreach (byte address in _deviceSlaveAddresses)
                        {
                            pmbus.TargetAddress = address;

                            // Look for all PMBUS device types
                            Device newDevice = new Device(pmbus, this, optionalDeviceInfoName);

                            if (newDevice.IsPresent( out is16BitAddressing, isSilent, isDeviceHidden))
                            {
                                // Try to parameterize the device
                                newDevice = ParameterizeDevice(newDevice);

								Log.Debug("New Device : ", newDevice.DeviceInfoName, " is16bitAddressing :", is16BitAddressing, " TargetAddress :" , pmbus.TargetAddress);

								// Check to see if this device has any special modifications
								// to its default register map and/or UiElement XML structure based on settings
								// within the private register space.
								newDevice = CustomizeDevice(newDevice);

                                devices.Add(newDevice);

                                // DEMO mode
                                if (adapter is VirtualAdapter)
                                {
                                    var va = adapter as VirtualAdapter;
                                    va.Registers = newDevice.Registers;
                                    break;
                                }

                                // Get the next i2c protocol
                                pmbus = new PMBusProtocol(address, pmbusAdapter.I2cGetConfiguration().BitRate, adapter, AdapterMode.Shared);
                            }
                        }
                    }

					// Set the last i2c protocol objects target address (which is not connected to a device) to the first devices address.
					// This is so the orphened protocol can communicate as soon as the plugin loads so that it appears to be valid.
					// The Protocol plugin will always load the last orphened protocol so that changing its address does not break the
					// link between the master and its slave device.
					if (devices.Count > 0)
					{
						var pmbus_ = devices[0].Protocol as PMBusProtocol;
						pmbus.TargetAddress = pmbus_.TargetAddress;
					}
					else
					{
                        if (pmbus != null)
                        {
                            pmbus.AttachedAdapter.RemoveProtocol(pmbus);
                            pmbus.Dispose();
                        }
					}
				}

				#endregion

				#region I2C

				// Loop through all interfaces on this adapter
				if (adapter is II2c && includeI2c)
				{
					II2c i2cAdapter = adapter as II2c;
					_deviceSlaveAddresses = new List<byte>();

					// If the user is forcing an I2c address
					if (forcedI2cAddress.HasValue)
					{
						_deviceSlaveAddresses.Add((byte)forcedI2cAddress);
					}
					else
					{
						// Otherwise add range to scan else use the default address
						if (i2cAdapter.IsScanAllAddresses && !isSilent)
						{
							if(IsAnMTADevice)
							{
								for (int i = 0x00; i < 0xFF; i++)
								{
									_deviceSlaveAddresses.Add((byte)i);
								}
							}
                            else
                            {
								for (int i = 0x30; i < 0x7F; i++)
								{
									_deviceSlaveAddresses.Add((byte)i);
								}
							}
						}
						else
						{
							_deviceSlaveAddresses.Add(i2cAdapter.DefaultSlaveAddress);
						}
					}

					// Create I2C Protocol object pointing to the first address to scan
					I2cProtocol i2c = new I2cProtocol(_deviceSlaveAddresses[0], i2cAdapter.I2cGetConfiguration().BitRate, adapter, AdapterMode.Shared);

					// Turn on all gpio lines
					i2c.SetGpio(0xF0);

					foreach (byte address in _deviceSlaveAddresses)
					{
						i2c.TargetAddress = address;

						// Look for all I2C device types
						Device newDevice = new Device(i2c, this, optionalDeviceInfoName);

						if (newDevice.IsPresent(out is16BitAddressing, isSilent, isDeviceHidden))
						{
                            #region checking i2c 16bit in silent mode for address confirmation
                            //if (isSilent && forcedI2cAddress == 56)
                            //                     {
                            //                         if (optionalDeviceInfoName.ToString() == "PE24103-R01" && includeI2c)
                            //                             is16BitAddressing = true;
                            //                     }
                            //                     else
                            //                     {
                            //                         is16BitAddressing = false;
                            //                     }
                            #endregion

                            newDevice.Is16BitAddressing = is16BitAddressing;
							
							// Try to parameterize the device
							newDevice = ParameterizeDevice(newDevice);

							Log.Debug("New Device : ", newDevice.DeviceInfoName, " is16bitAddressing :", is16BitAddressing, " TargetAddress :", i2c.TargetAddress);

							// Check to see if this device has any special modifications
							// to its default register map and/or UiElement XML structure based on settings
							// within the private register space.
							newDevice = CustomizeDevice(newDevice);

							if(newDevice.DeviceInfoName == "PE23108-R01")
                            {
								//PE23108 device need to support only the device addresses  48=72,4A=74, 4D=77 for 7 bit type - Request by Jason

								byte[] PE23108DeviceAddresses = new byte[] { 72, 74, 77 };

                                foreach(var targetAddress in PE23108DeviceAddresses)
                                {
									if(targetAddress == address)
                                    {
										devices.Add(newDevice);
									}
									else
                                    {
										continue;
                                    }
                                }
                            }
							else
                            {
								devices.Add(newDevice);
							}


							// DEMO mode
							if (adapter is VirtualAdapter)
							{
								var va = adapter as VirtualAdapter;
								va.Registers = newDevice.Registers;
								break;
							}

							// Get the next i2c protocol
							i2c = new I2cProtocol(address, i2cAdapter.I2cGetConfiguration().BitRate, adapter, AdapterMode.Shared);
                           
						}
					}

					// Set the last i2c protocol objects target address (which is not connected to a device) to the first devices address.
					// This is so the orphened protocol can communicate as soon as the plugin loads so that it appears to be valid.
					// The Protocol plugin will always load the last orphened protocol so that changing its address does not break the
					// link between the master and its slave device.
					if (devices.Count > 0)
					{
						var i2c_ = devices[0].Protocol as I2cProtocol;
						i2c.TargetAddress = i2c_.TargetAddress;
					}
					else
					{
						i2c.AttachedAdapter.RemoveProtocol(i2c);
						i2c.Dispose();
					}
				}

				#endregion
			}

			return devices;
		}

		public List<Device> GetAllDevicesAutoAddress(Adapter adapter, string optionalDeviceInfoName = "", bool isSilent = false, bool isDeviceHidden = false)
		{
			List<Device> devices = new List<Device>();
			bool is16BitAddressing = false;
			if (adapter != null)
			{
				// Loop through all interfaces on this adapter
				if (adapter is II2c)
				{
					// AD7 - AD0 xxxx 0111
					// AD7 - GPIOL3 - Blue
					// AD6 - GPIOL2 - White
					// AD5 - GPIOL1 - Purple
					// AD4 - GPIOL0 - Grey
					byte[] adbus = new byte[] { 0x80, 0xC0, 0xE0, 0xF0 };

					// List of available i2c addresses for distribution
					List<int> addressPresets = new List<int>();
					addressPresets.AddRange(Enumerable.Range(0x31, adbus.Length));
					addressPresets.Reverse();

					int deviceCount = 0;
					II2c i2cAdapter = adapter as II2c;

					// Create I2C Protocol object pointing to the first address to scan
					I2cProtocol i2c = new I2cProtocol(0x30, i2cAdapter.I2cGetConfiguration().BitRate, adapter, AdapterMode.Shared);

					// Turn off all devices
					i2c.SetGpio(0);

					// Enable one device at a time
					foreach (var gpio in adbus)
					{
						i2cAdapter.SetGpio(gpio);

						// Look for all I2C device types
						Device newDevice = new Device(i2c, this, optionalDeviceInfoName);

						if (newDevice.IsPresent(out is16BitAddressing, isSilent, isDeviceHidden))
						{
							// Try to parameterize the device
							newDevice = ParameterizeDevice(newDevice);

							// Check to see if this device has any special modifications
							// to its default register map and/or UiElement XML structure based on settings
							// within the private register space.
							newDevice = CustomizeDevice(newDevice);

							// Set the I2C address and remove the address from the list
							SetI2cAccess(newDevice, true);
							i2c.TargetAddress = SetAddress(newDevice, (byte)addressPresets[deviceCount++]);
							SetI2cAccess(newDevice, false);
							devices.Add(newDevice);

							i2c = new I2cProtocol(0x30, i2cAdapter.I2cGetConfiguration().BitRate, adapter, AdapterMode.Shared);
                            /*if (newDevice.IsPE26100)
                            {
								i2c.TargetAddress = 50;
                            }*/
						}
					}
                    
					// Set the last i2c protocol objects target address (which is not connected to a device) to the first devices address.
					// This is so the orphened protocol can communicate as soon as the plugin loads so that it appears to be valid.
					// The Protocol plugin will always load the last orphened protocol so that changing its address does not break the
					// link between the master and its slave device.
					if (devices.Count > 0)
					{
						var i2c_ = devices[0].Protocol as I2cProtocol;
						i2c.TargetAddress = i2c_.TargetAddress;
					}
					else
					{
						// If there are no devices then change the invalid address 0x38 to the first valid address
						i2c.TargetAddress = 0x30;
					}
				}
			}

			return devices;//.OrderBy(d => d.DeviceNameAddress).ToList();
		}

		public List<Device> GetVirtualDevices(Adapter adapter, List<string> deviceInfoNameList, bool is16BitI2CMode=false, bool isMTADeviceMode = false)
		{
			List<Device> devices = new List<Device>();
			this._is16BitI2C = is16BitI2CMode;
			
			// Create I2C Protocol object pointing to the first address to scan
			foreach (string device in deviceInfoNameList)
			{
				VirtualProtocol virtualProtocol = new VirtualProtocol(0x30, 100000, _is16BitI2C, adapter, AdapterMode.Exclusive, device);
				Device newDevice = new Device(virtualProtocol, this, device, "n/a");

				if (is16BitI2CMode)
				{
					newDevice.Is16BitAddressing = true;
					virtualProtocol.TargetAddress = 0x38;
				}

				newDevice.IsMTADevice = isMTADeviceMode;

				newDevice = ParameterizeDevice(newDevice);
				devices.Add(newDevice);
			}
			return devices;
		}

		#endregion

		#region Private Methods

		private Device ParameterizeDevice(Device device)
		{
			// If we ever get here with no assigned device info name
			// then just force the device info to the default.
			string fileName = string.Empty;
			fileName = (string.IsNullOrEmpty(device.DeviceInfoName))
				? DEFAULT_DEVICE
				: device.DeviceInfoName;

			string fileToFind = fileName + ".adz";
			string appFile = "Application.xml";
			string configFile = string.Format("Config/{0}", fileToFind.Replace(".adz", ".xml"));
			string DocumentFile = string.Format("Documents/{0}", fileToFind.Replace(".adz", "DS.pdf"));
			string HelpFile = string.Format("Documents/{0}", fileToFind.Replace(".adz", "HS.pdf"));

			byte[] code = new byte[] { 0x69, 0x38, 0x6D, 0x53, 0x73, 0x76, 0x36, 0x41, 0x6A };
			string scode = Encoding.ASCII.GetString(code);

			FileInfo[] files = FileUtilities.DeviceInfoDirectory.GetFiles();
			FileInfo adzfile = files.FirstOrDefault(f => f.Name == fileToFind);

			XDocument appConfig = null;
			XDocument deviceConfig = null;

			// Verify the file exists
			if (adzfile != null)
			{
				try
				{
					// Get the register map
					using (var ms = new MemoryStream())
					{
						using (ZipFile zip = ZipFile.Read(adzfile.FullName))
                        {
                            ZipEntry entry = zip[configFile];
                            entry.Encryption = EncryptionAlgorithm.WinZipAes256;
                            entry.ExtractWithPassword(ms, scode);
                            ms.Position = 0;
                            deviceConfig = XDocument.Load(ms);
                            device.BaseName = deviceConfig.XPathSelectElement("Device/DeviceId/BaseName").Value;
                            if (device.Is16BitAddressing)
                            {
                                device.I2CBaseName = deviceConfig.XPathSelectElement("Device/DeviceId/I2CBaseName").Value;

                                if (device.I2CBaseName != null)
                                {
                                    device.BaseName = device.I2CBaseName;

                                }
                            }
                            device.DisplayName = (deviceConfig.XPathSelectElement("Device/DeviceId/DisplayName") != null) ? deviceConfig.XPathSelectElement("Device/DeviceId/DisplayName").Value : deviceConfig.XPathSelectElement("Device/DeviceId/Name").Value;
							device.AltDisplayName = (deviceConfig.XPathSelectElement("Device/DeviceId/AlternateName") != null) ? deviceConfig.XPathSelectElement("Device/DeviceId/AlternateName").Value : deviceConfig.XPathSelectElement("Device/DeviceId/Name").Value;
							device.DeviceName = deviceConfig.XPathSelectElement("Device/DeviceId/Name").Value;
                            device.DeviceVersion = deviceConfig.XPathSelectElement("Device/DeviceId/Version").Value;

                            if (device.Is16BitAddressing)
                            {
                                device.ParseRegisterData(deviceConfig.XPathSelectElements("Device/I2CRegisters/Register"), device.Is16BitAddressing);
                            }
                            else
                            {
                                device.ParseRegisterData(deviceConfig.XPathSelectElements("Device/Registers/Register"));

								//if( deviceConfig.XPathSelectElement("Device/I2CRegisters/Register") != null)
								//{
								//	device.ParseRegisterData(deviceConfig.XPathSelectElements("Device/I2CRegisters/Register"), true);
								//}
							}

							CreateDummyDevice(deviceConfig, device.DeviceInfoName);

                            device.LoadSlaveRegisterMapConfig(deviceConfig.XPathSelectElements("Device/ChildRegisters/Register"));
                            device.UiElements = (XElement)deviceConfig.XPathSelectElement("Device/UI");
                            device.IsParameterized = true;
                        }
                    }

					// Get the plugin info file
					using (var ms = new MemoryStream())
					{
						using (ZipFile zip = ZipFile.Read(adzfile.FullName))
						{
							ZipEntry entry = zip[appFile];
							entry.Extract(ms);
							ms.Position = 0;
							appConfig = XDocument.Load(ms);
							IEnumerable<XElement> plugins = appConfig.Descendants("Plugin");

							foreach (string item in plugins)
							{
								if(item == "PE24103Control" && (device.Is16BitAddressing))
								{
									continue;
                                }
								else if(item == "PE24103i2cControl" && (!device.Is16BitAddressing))
								{
									continue;
								}
								device.PluginCompatibility.Add(item);
							}
						}
					}

					// Get the datasheet - don't error if missing
					using (var ms = new MemoryStream())
					{
						using (ZipFile zip = ZipFile.Read(adzfile.FullName))
						{
							ZipEntry entry = zip[DocumentFile];
							if (entry != null)
							{
								entry.Extract(ms);
								ms.Position = 0;
								device.DataSheet = ms.ToArray();
							}
						}
					}

					// Get the helpsheet - don't error if missing
					using (var ms = new MemoryStream())
					{
						using (ZipFile zip = ZipFile.Read(adzfile.FullName))
						{
							ZipEntry entry = zip[HelpFile];
							if (entry != null)
							{
								entry.Extract(ms);
								ms.Position = 0;
								device.HelpSheet = ms.ToArray();
							}
						}
					}
				}
				catch (Exception ex)
				{
					Log.Error("ParameterizeDevice ", ex.Message);
					throw;
				}
			}

			return device;
		}

        private static void CreateDummyDevice(XDocument deviceConfig, string deviceName)
        {
            string folderName = @"c:\Devices";

            System.IO.Directory.CreateDirectory(folderName);

            string path = System.IO.Path.Combine(folderName, deviceName + ".xml");


            List<XElement> regData = deviceConfig.Descendants("Registers").ToList();

            regData.AddRange(deviceConfig.Descendants("ChildRegisters").ToList());

            XmlSerializeRegisterMap(path, regData);
			regData.AddRange(deviceConfig.Descendants("I2CRegisters").ToList());

			XmlSerializeRegisterMap(path, regData);
			string value = "00";

            XmlDocument xDoc = new XmlDocument();
            xDoc.Load(path);

            XmlElement root = xDoc.DocumentElement;

			//Update registers
            XmlNodeList regNodes = root.SelectNodes("//Register");
			if(regNodes.Count != 0)
            UpdateRegisterXML(value, xDoc, regNodes);

			//Update child registers
			XmlNodeList childRegNodes = root.SelectNodes("//ChildRegisters//Register");
			if (childRegNodes.Count != 0)
			UpdateRegisterXML(value, xDoc, childRegNodes);

			//Update I2C Registers
			XmlNodeList I2CRegNodes = root.SelectNodes("//I2CRegisters//Register");
			if (I2CRegNodes.Count != 0)
			UpdateRegisterXML(value, xDoc, I2CRegNodes);

			xDoc.Save(path);
        }

        private static void UpdateRegisterXML(string value, XmlDocument xDoc, XmlNodeList nodes)
        {
            foreach (XmlNode node in nodes)
            {
                //Removing Attributes from nodes 
                node.Attributes.RemoveNamedItem("DataType");
                //node.Attributes.RemoveNamedItem("Size");
                node.Attributes.RemoveNamedItem("Format");
                node.Attributes.RemoveNamedItem("LockBitMask");
                node.Attributes.RemoveNamedItem("TriggeredRead");
                node.Attributes.RemoveNamedItem("Unit");
                node.Attributes.RemoveNamedItem("ReadOnly");
                node.Attributes.RemoveNamedItem("Private");
                node.Attributes.RemoveNamedItem("Description");
                node.Attributes.RemoveNamedItem("Access");
                node.Attributes.RemoveNamedItem("LoadFormula");
                node.Attributes.RemoveNamedItem("StoreFormula");
                node.Attributes.RemoveNamedItem("SignedBit");

				//append a new Attribute LastReadValue which will be assigned as 00 initially and

				XmlAttribute xKey = xDoc.CreateAttribute("LastReadValue");
                xKey.Value = value;
                node.Attributes.Append(xKey);
            }
        }

        private static void XmlSerializeRegisterMap(string path, List<XElement> reg)
		{
			FileStream fs = new FileStream(path, FileMode.Create);
			XmlSerializer formatter = new XmlSerializer(typeof(List<XElement>));
			try
			{
				formatter.Serialize(fs, reg);
				Log.Information("XmlSerializeRegisterMap - serialize register map");
			}
			catch (InvalidOperationException e)
			{
				Console.WriteLine("Failed to serialize register map.\r\nReason: " + e.Message);
				Log.Error("Failed to serialize register map.\r\nReason: " + e.Message);

				Console.ReadLine();
			}
			finally
			{
				fs.Close();
			}
		}


		private Device CustomizeDevice(Device device)
		{
			// Get the name of the device without any containing dashes
			string deviceName = device.DeviceInfoName.Replace("-", "");

			// Get a reference to an available type with the same name.
			var customizer = Type.GetType("DeviceAccess." + deviceName + ",DeviceAccess");

			// If found, create an instance of the customizing class and perform its base actions
			if (customizer != null)
			{
				try
				{
					var obj = Activator.CreateInstance(customizer, device) as CustomizeBase;

					// Modify the device information if necessary
					obj.ResetDevice(null);
					obj.CustomizeDevice(IsAuthenticated);
				}
				catch (Exception)
				{
					throw;
				}
			}

			return device;
		}

		private byte SetAddress(Device device, byte address)
		{
			// Get the name of the device without any containing dashes
			string deviceName = device.DeviceInfoName.Replace("-", "");

			// Get a reference to an available type with the same name.
			var customizer = Type.GetType("DeviceAccess." + deviceName + ",DeviceAccess");

			// If found, create an instance of the customizing class and perform its base actions
			if (customizer != null)
			{
				try
				{
					var obj = Activator.CreateInstance(customizer, device) as CustomizeBase;

					// Set I2C address
					address = obj.SetAddress(address);
				}
				catch (Exception)
				{
					throw;
				}
			}

			return address;
		}

		private void SetI2cAccess(Device device, bool unlock)
		{
			// Get the name of the device without any containing dashes
			string deviceName = device.DeviceInfoName.Replace("-", "");

			// Get a reference to an available type with the same name.
			var customizer = Type.GetType("DeviceAccess." + deviceName + ",DeviceAccess");

			// If found, create an instance of the customizing class and perform its base actions
			if (customizer != null)
			{
				try
				{
					var obj = Activator.CreateInstance(customizer, device) as CustomizeBase;

					// Set I2C access
					if (unlock)
					{
						obj.SetI2cAccess(CustomizeBase.I2cAccess.Unlock);
					}
					else
					{
						obj.SetI2cAccess(CustomizeBase.I2cAccess.Lock);
					}
				}
				catch (Exception)
				{
					throw;
				}
			}
		}

		#endregion

		public class DeviceInfo
		{
			public string MFRID { get; set; }
			public string Model { get; set; }
			public string Revision { get; set; }
			public string Plugin { get; set; }
			public string Code { get; set; }
			public int Key { get; set; }
            public string Protocol { get; set; }
		}
	}

	[Serializable]
	public class RegisterData
	{
		[XmlAttribute]
		public string Name { get; set; }
		[XmlAttribute]
		public string Address { get; set; }
		[XmlAttribute]
		public string DataType { get; set; }
		[XmlAttribute]
		public string Size { get; set; }
		[XmlAttribute]
		public string Format { get; set; }
		[XmlAttribute]
		public string Unit { get; set; }
		[XmlAttribute]
		public string ReadOnly { get; set; }
		[XmlAttribute]
		public string Private { get; set; }
		[XmlAttribute]
		public string Description { get; set; }

		[XmlElement("Bit")]
		public List<BitData> Bit { get; set; }
	}

	[Serializable]
	public class BitData
	{
		[XmlAttribute]
		public string Mask { get; set; }
		[XmlAttribute]
		public string Name { get; set; }
		[XmlAttribute]
		public string Description { get; set; }
	}

	public class IdentityRegister
	{
		public byte? RegCode { get; set; }

		public byte RegId { get; set; }

		public byte Mask { get; set; }

		public int Shift { get; set; }
	}

	public class IdentityRegisterTwoBytes
	{
		public ushort? RegCode { get; set; }

		public ushort RegId { get; set; }

		public ushort Mask { get; set; }

		public int Shift { get; set; }
	}
}
