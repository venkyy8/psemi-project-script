using AdapterAccess.Adapters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml.Linq;
using System.Linq;
using Ionic.Zip;
using HardwareInterfaces;
using FTD2XX_NET;

namespace AdapterAccess
{
	public class AdapterManager
	{
		#region Private Members

		/// <summary>
		/// Object used to "lock" the scan function's critical section.
		/// </summary>
		private object _scanLock = new object();

		/// <summary>
		/// List of all adapters present in the system.
		/// </summary>
		private List<Adapter> _adapters = new List<Adapter>();

		/// <summary>
		/// List of FTDI Adapters.
		/// </summary>
		private List<FtdiI2cAdapter> _ftdiI2cAdapters = new List<FtdiI2cAdapter>();
		private List<FtdiPMBusAdapter> _ftdiPmbusAdapters = new List<FtdiPMBusAdapter>();

        /// <summary>
        /// List of PMBob Adapters.
        /// </summary>
        private List<PMBobAdapter> _pmbobAdapters = new List<PMBobAdapter>();

		/// <summary>
		/// List of Aardvark Adapters.
		/// </summary>
		private List<AardvarkAdapter> _aardvarkAdapters = new List<AardvarkAdapter>();

		/// <summary>
		/// Counter used to assign indexes to new adapters.
		/// </summary>
		private int _nextAdapterIndex = 0;

		#endregion

		#region Public Properties

		/// <summary>
		/// Total number of adapters in the system.
		/// </summary>
		public int AdapterCount
		{
			get { return _adapters.Count; }
		}

		#endregion
		
		#region Constructors

		public AdapterManager()
		{
		} 

		#endregion

		#region Get Adapters

		/// <summary>
		/// Scans the system for all supported hardware adapters.
		/// </summary>
		/// <returns>The number of adapters found.</returns>
		public int Scan(bool includeI2c, bool includePmbus)
		{
			// Only allow one thread to scan at a time.
			lock (_scanLock)
			{
				// Scan Aardvark adapters
				//ScanAardvarkAdapters();

				// Scan for FTDI PMBus adapters
				if (includePmbus)
                {
                    ScanFtdiPmbusAdapters();
                    ScanPMBobAdapters();
                }

                // Scan for FTDI I2C adapters
                if (includeI2c)
                    ScanFtdiI2cAdapters();

				// Rebuild the "master" list of adapters
				_adapters.Clear();

                foreach (var adapter in _ftdiPmbusAdapters)
                {
                    _adapters.Add(adapter);
                }

				foreach (var adapter in _ftdiI2cAdapters)
				{
					_adapters.Add(adapter);
				}

                foreach (PMBobAdapter adapter in _pmbobAdapters)
                {
                    _adapters.Add(adapter);
                }

				foreach (AardvarkAdapter adapter in _aardvarkAdapters)
				{
					_adapters.Add(adapter);
				}

				// Add more adapters here when they become necessary.
			}

			return _adapters.Count;
		}

		/// <summary>
		/// Provides a fixed array of all of the adapters in the system.
		/// </summary>
		/// <param name="forceRescan">Forces the adapter manager to scan for adapters first.</param>
		/// <returns>List of Adapter objects.</returns>
		public List<Adapter> GetAllAdapters(bool forceRescan = true, bool includeI2c = true, bool includePmbus = true)
		{
			if (forceRescan)
                Scan(includeI2c, includePmbus);

			return new List<Adapter>(_adapters);
		}

		/// <summary>
		/// Returns a list of all adapters that are "available" or "open shared"
		/// in the system.
		/// </summary>
		/// <param name="forceRescan">Forces the adapter manager to scan for adapters first.</param>
		/// <returns>List of Adapter objects.</returns>
        public List<Adapter> GetAllAvailableAdapters(bool forceRescan = true, bool includeI2c = true, bool includePmbus = true)
		{
			if (forceRescan)
                Scan(includeI2c, includePmbus);

			List<Adapter> adapters = new List<Adapter>();

			foreach (Adapter adapter in _adapters)
			{
				if (adapter.State == AdapterState.Available)
				{
					adapters.Add(adapter);
				}
			}

			return adapters;
		}

		public List<Adapter> GetAllAvailableVirtualAdapters()
		{
			var adapter = new VirtualAdapter();
			adapter.State = AdapterState.Available;
			_adapters.Add(adapter);
			return _adapters;
		}

		#endregion

		#region Public Methods

		public void GetPluginInfo(int? biteRate = null)
		{
			string fileToFind = "Application.adz";
			string adaptersFile = "Adapters.xml";
			FileInfo[] files = FileUtilities.DeviceInfoDirectory.GetFiles();
			FileInfo adzfile = files.First(f => f.Name == fileToFind);

			XDocument appConfig = null;

			// Verify the file exists
			if (adzfile != null)
			{
				try
				{
					using (var ms = new MemoryStream())
					{
						using (ZipFile zip = ZipFile.Read(adzfile.FullName))
						{
							ZipEntry entry = zip[adaptersFile];
							entry.Extract(ms);
							ms.Position = 0;
							appConfig = XDocument.Load(ms);

							IEnumerable<XElement> adapters = appConfig.Descendants("Adapter");

							foreach (Adapter adapter in _adapters)
							{
								IPMBus pmbusAdapter = null;
								II2c i2cAdapter = null;
								IAdapter iAdapter = adapter as IAdapter;

								if (adapter.IsInterfaceSupported(typeof(II2c)))
								{
									// Get II2c Reference
									i2cAdapter = adapter as II2c;
								}
								else if (adapter.IsInterfaceSupported(typeof(IPMBus)))
								{
									pmbusAdapter = adapter as IPMBus;
								}


								XElement thisAdapter = adapters.FirstOrDefault(a => a.Attribute("Name").Value == iAdapter.AdapterName);
								if (thisAdapter != null)
								{
									if (i2cAdapter != null)
									{
										#region I2C

										// If this fails then the user corrupted the file during editing
										try
										{
											var settings = thisAdapter.Element("I2cSettings");

											// Settings
											i2cAdapter.I2cSetConfiguration(new I2cConfiguration
												{
													BitRate = Convert.ToInt32(settings.Element("DefaultBitRate").Value),
													SlaveAddress = Convert.ToByte(settings.Element("SlaveAddress").Value, 16),
													ScanAllAddresses = Convert.ToBoolean(settings.Element("ScanAllAddresses").Value),
													PullupsEnabled = Convert.ToBoolean(settings.Element("PullupsEnabled").Value),
													BusLockTimeoutMs = Convert.ToInt32(settings.Element("DefaultBusLockTimeout").Value)
												});

											// BitRate Capabilites
											var bitRateCapabilities = settings.Elements("BitRates");
											var b = bitRateCapabilities.Elements("BitRate");
											int[] supportedBitRates = new int[b.Count()];
											string[] supportedBitRateLabels = new string[b.Count()];

											for (int i = 0; i < b.Count(); i++)
											{
												supportedBitRates[i] = int.Parse(b.ElementAt(i).Value);
												supportedBitRateLabels[i] = b.ElementAt(i).Attribute("Label").Value;
											}

											// BusTimeout Capabilities
											var busCapabilities = settings.Elements("BusLockTimeouts");
											var bus = busCapabilities.Elements("BusLockTimeout");
											int[] supportedBusTimeouts = new int[bus.Count()];
											string[] supportedBusTimeoutLabels = new string[bus.Count()];

											for (int i = 0; i < bus.Count(); i++)
											{
												supportedBusTimeouts[i] = int.Parse(bus.ElementAt(i).Value);
												supportedBusTimeoutLabels[i] = bus.ElementAt(i).Attribute("Label").Value;
											}

											// Set capabilities
											i2cAdapter.I2cSetCapabilities(new I2cCapabilities
												{
													SoftwareControlledPullups = Convert.ToBoolean(settings.Element("SoftwareControlledPullups").Value),
													StopCommand = Convert.ToBoolean(settings.Element("StopCommand").Value),
													SupportsDriveByZero = Convert.ToBoolean(settings.Element("SupportsDriveByZero").Value),
													SupportedBitRates = supportedBitRates,
													SupportedBitRatesLabels = supportedBitRateLabels,
													SupportedBusLockTimeouts = supportedBusTimeouts
												});

										}
										catch (Exception e)
										{
											throw new Exception("Error reading the adapters <I2cSettings> node in the configuration file.", e);
										}

										#endregion
									}

									if (pmbusAdapter != null)
									{
										#region PMBus

										// If this fails then the user corrupted the file during editing
										try
										{
											var settings = thisAdapter.Element("PMBusSettings");

											// Settings
											pmbusAdapter.I2cSetConfiguration(new PMBusConfiguration
											{
												BitRate = Convert.ToInt32(settings.Element("DefaultBitRate").Value),
												SlaveAddress = Convert.ToByte(settings.Element("SlaveAddress").Value, 16),
												ScanAllAddresses = Convert.ToBoolean(settings.Element("ScanAllAddresses").Value),
												PullupsEnabled = Convert.ToBoolean(settings.Element("PullupsEnabled").Value),
												BusLockTimeoutMs = Convert.ToInt32(settings.Element("DefaultBusLockTimeout").Value)
											});

											// BitRate Capabilites
											var bitRateCapabilities = settings.Elements("BitRates");
											var b = bitRateCapabilities.Elements("BitRate");
											int[] supportedBitRates = new int[b.Count()];
											string[] supportedBitRateLabels = new string[b.Count()];

											for (int i = 0; i < b.Count(); i++)
											{
												supportedBitRates[i] = int.Parse(b.ElementAt(i).Value);
												supportedBitRateLabels[i] = b.ElementAt(i).Attribute("Label").Value;
											}

											// BusTimeout Capabilities
											var busCapabilities = settings.Elements("BusLockTimeouts");
											var bus = busCapabilities.Elements("BusLockTimeout");
											int[] supportedBusTimeouts = new int[bus.Count()];
											string[] supportedBusTimeoutLabels = new string[bus.Count()];

											for (int i = 0; i < bus.Count(); i++)
											{
												supportedBusTimeouts[i] = int.Parse(bus.ElementAt(i).Value);
												supportedBusTimeoutLabels[i] = bus.ElementAt(i).Attribute("Label").Value;
											}

											// Set capabilities
											pmbusAdapter.I2cSetCapabilities(new PMBusCapabilities
											{
												SoftwareControlledPullups = Convert.ToBoolean(settings.Element("SoftwareControlledPullups").Value),
												StopCommand = Convert.ToBoolean(settings.Element("StopCommand").Value),
												SupportsDriveByZero = Convert.ToBoolean(settings.Element("SupportsDriveByZero").Value),
												SupportedBitRates = supportedBitRates,
												SupportedBitRatesLabels = supportedBitRateLabels,
												SupportedBusLockTimeouts = supportedBusTimeouts,
												Pec = Convert.ToBoolean(settings.Element("Pec").Value)
											});

										}
										catch (Exception e)
										{
											throw new Exception("Error reading the adapters <PMBusSettings> node in the configuration file.", e);
										}

										#endregion
									}

									try
									{
										// Plugins
										IEnumerable<XElement> plugins = thisAdapter.Descendants("Plugin");

										foreach (string plugin in plugins)
										{
											iAdapter.PluginCompatibility.Add(plugin);
										}
									}
									catch (Exception e)
									{
										throw new Exception("Error reading the adapters <Plugin> node in the configuration file.", e);
									}
								}
							}
						}
					}
				}
				catch (Exception ex1)
				{
					throw new Exception("Error reading the adapters configuration file.", ex1);
				}
			}
		}

		#endregion

		#region Private Methods

		private void ScanFtdiI2cAdapters()
		{
			var ftdi_wrapper = new FTDI();
			var enumeratedAdapters = FtdiI2cAdapter.GetAllHardwareAdapters(ftdi_wrapper);

			foreach (var adapter in _ftdiI2cAdapters)
			{
				bool stillPresent = false;

				for (int f = 0; f < enumeratedAdapters.Count; f++)
				{
					var ftdiEnum = enumeratedAdapters[f];

					if (ftdiEnum.ftHandle == adapter.GetAdapterInfo().ftHandle)
					{
						stillPresent = true;
						ftdiEnum.Claimed = true;
					}

					switch (adapter.State)
					{
						case AdapterState.NotPresent:
							if (ftdiEnum.Free)
								adapter.State = AdapterState.Available;
							else
								adapter.State = AdapterState.NotAvailable;
							break;
						case AdapterState.NotAvailable:
							if (ftdiEnum.Free)
								adapter.State = AdapterState.Available;
							break;
						case AdapterState.Available:
							if (!ftdiEnum.Free)
								adapter.State = AdapterState.NotAvailable;
							break;
						case AdapterState.OpenShared:
						case AdapterState.OpenReserved:
							// Should be marked as "not free"
							// Flag an error if otherwise
							if (ftdiEnum.Free)
								throw new Exception("HW State: showing 'free' when it should be 'not free'.");
							break;
						case AdapterState.Removed:
							if (ftdiEnum.Free)
							{
								adapter.State = AdapterState.Reattached;
								if (adapter.Resync(ftdiEnum))
								{
									if (adapter.AccessMode == AdapterMode.Shared)
										adapter.State = AdapterState.OpenShared;
									else
										adapter.State = AdapterState.OpenReserved;
								}
								else
								{
									throw new Exception("Error trying to 'resync' the hardware adapter.");
								}
							}
							else
								adapter.State = AdapterState.ReattachedNotAvailable;
							break;
						case AdapterState.ReattachedNotAvailable:
							if (ftdiEnum.Free)
								adapter.State = AdapterState.Reattached;
							break;
						case AdapterState.Reattached:
							if (!ftdiEnum.Free)
								adapter.State = AdapterState.ReattachedNotAvailable;
							else
							{
								if (adapter.Resync(ftdiEnum))
								{
									if (adapter.AccessMode == AdapterMode.Shared)
										adapter.State = AdapterState.OpenShared;
									else
										adapter.State = AdapterState.OpenReserved;
								}
								else
								{
									throw new AdapterException("Error trying to 'resync' the hardware adapter.");
								}
							}
							break;
					}

					// Is the hardware still present?
					if (!stillPresent)
					{
						switch (adapter.State)
						{
							case AdapterState.NotPresent:
								break;
							case AdapterState.NotAvailable:
								adapter.State = AdapterState.NotPresent;
								break;
							case AdapterState.Available:
								adapter.State = AdapterState.NotPresent;
								break;
							case AdapterState.OpenShared:
							case AdapterState.OpenReserved:
								adapter.State = AdapterState.Removed;
								break;
							case AdapterState.Removed:
								break;
							case AdapterState.ReattachedNotAvailable:
								adapter.State = AdapterState.Removed;
								break;
							case AdapterState.Reattached:
								adapter.State = AdapterState.Removed;
								break;
						}
					}
				}
			}

			foreach (var ftdiEnum in enumeratedAdapters)
			{
				if (ftdiEnum.ftHandle == IntPtr.Zero && ftdiEnum.Type == FTDI.FT_DEVICE.FT_DEVICE_232H)
				{
					FtdiI2cAdapter newAdapter = new FtdiI2cAdapter(ftdiEnum);
					newAdapter.State = AdapterState.Available;
					_ftdiI2cAdapters.Add(newAdapter);
				}
			}
		}

		private void ScanFtdiPmbusAdapters()
		{
			var ftdi_wrapper = new FTD2XX_NET_PMBus.FTDI();
			var enumeratedAdapters = FtdiPMBusAdapter.GetAllHardwareAdapters(ftdi_wrapper);

			foreach (var adapter in _ftdiPmbusAdapters)
			{
				bool stillPresent = false;

				for (int f = 0; f < enumeratedAdapters.Count; f++)
				{
					var ftdiEnum = enumeratedAdapters[f];

					if (ftdiEnum.ftHandle == adapter.GetAdapterInfo().ftHandle)
					{
						stillPresent = true;
						ftdiEnum.Claimed = true;
					}

					switch (adapter.State)
					{
						case AdapterState.NotPresent:
							if (ftdiEnum.Free)
								adapter.State = AdapterState.Available;
							else
								adapter.State = AdapterState.NotAvailable;
							break;
						case AdapterState.NotAvailable:
							if (ftdiEnum.Free)
								adapter.State = AdapterState.Available;
							break;
						case AdapterState.Available:
							if (!ftdiEnum.Free)
								adapter.State = AdapterState.NotAvailable;
							break;
						case AdapterState.OpenShared:
						case AdapterState.OpenReserved:
							// Should be marked as "not free"
							// Flag an error if otherwise
							if (ftdiEnum.Free)
								throw new Exception("HW State: showing 'free' when it should be 'not free'.");
							break;
						case AdapterState.Removed:
							if (ftdiEnum.Free)
							{
								adapter.State = AdapterState.Reattached;
								if (adapter.Resync(ftdiEnum))
								{
									if (adapter.AccessMode == AdapterMode.Shared)
										adapter.State = AdapterState.OpenShared;
									else
										adapter.State = AdapterState.OpenReserved;
								}
								else
								{
									throw new Exception("Error trying to 'resync' the hardware adapter.");
								}
							}
							else
								adapter.State = AdapterState.ReattachedNotAvailable;
							break;
						case AdapterState.ReattachedNotAvailable:
							if (ftdiEnum.Free)
								adapter.State = AdapterState.Reattached;
							break;
						case AdapterState.Reattached:
							if (!ftdiEnum.Free)
								adapter.State = AdapterState.ReattachedNotAvailable;
							else
							{
								if (adapter.Resync(ftdiEnum))
								{
									if (adapter.AccessMode == AdapterMode.Shared)
										adapter.State = AdapterState.OpenShared;
									else
										adapter.State = AdapterState.OpenReserved;
								}
								else
								{
									throw new AdapterException("Error trying to 'resync' the hardware adapter.");
								}
							}
							break;
					}

					// Is the hardware still present?
					if (!stillPresent)
					{
						switch (adapter.State)
						{
							case AdapterState.NotPresent:
								break;
							case AdapterState.NotAvailable:
								adapter.State = AdapterState.NotPresent;
								break;
							case AdapterState.Available:
								adapter.State = AdapterState.NotPresent;
								break;
							case AdapterState.OpenShared:
							case AdapterState.OpenReserved:
								adapter.State = AdapterState.Removed;
								break;
							case AdapterState.Removed:
								break;
							case AdapterState.ReattachedNotAvailable:
								adapter.State = AdapterState.Removed;
								break;
							case AdapterState.Reattached:
								adapter.State = AdapterState.Removed;
								break;
						}
					}
				}
			}

			foreach (var ftdiEnum in enumeratedAdapters)
			{
				if (ftdiEnum.ftHandle == IntPtr.Zero && ftdiEnum.Type == FTD2XX_NET_PMBus.FTDI.FT_DEVICE.FT_DEVICE_232H)
				{
					FtdiPMBusAdapter newAdapter = new FtdiPMBusAdapter(ftdiEnum);
					newAdapter.State = AdapterState.Available;
					_ftdiPmbusAdapters.Add(newAdapter);
				}
			}
		}

        private void ScanPMBobAdapters()
        {
            List<PMBobDeviceInfo> enumberatedPMBobInstances = PMBobAdapter.GetAllHardwareAdapters();
            foreach (PMBobDeviceInfo hwEnum in enumberatedPMBobInstances)
            {
                if (!hwEnum.Claimed)
                {
                    PMBobAdapter newAdapter = new PMBobAdapter(hwEnum);
                    newAdapter.State = AdapterState.Available;
                    _pmbobAdapters.Add(newAdapter);
                }
            }
        }

		/// <summary>
		/// Scans the system for all Aardvark adapters. It updates the list that
		/// tracks Aardvark adapter objects.
		/// </summary>
		private void ScanAardvarkAdapters()
		{
			// Get info about current state of Aardvark hardware adapters
			AardvarkHardwareEnumeration[] enumeratedAardvarkInstances = AardvarkAdapter.GetAllHardwareAdapters();

			// Update the state for each existing adapter object
			foreach (AardvarkAdapter adapter in _aardvarkAdapters)
			{
				// Flag tracking if the adapter has been removed
				bool stillPresent = false;

				// Find the current enumeration info for this adapter if it is still attached
				foreach (AardvarkHardwareEnumeration hwEnum in enumeratedAardvarkInstances)
				{
					// Each Aardvark adapter has a unique ID
					if (hwEnum.UniqueId == adapter.UniqueId)
					{
						// Indicate this device has been accounted for
						hwEnum.Claimed = true;
						stillPresent = true;

						// Update the port number in case the adapter was initially created
						// as "NOT_FREE"
						adapter.UpdatePortNumber(hwEnum.PortNumber);

						// Update adapter state
						switch (adapter.State)
						{
							case AdapterState.NotPresent:
								if (hwEnum.Free)
									adapter.State = AdapterState.Available;
								else
									adapter.State = AdapterState.NotAvailable;
								break;
							case AdapterState.NotAvailable:
								if (hwEnum.Free)
									adapter.State = AdapterState.Available;
								break;
							case AdapterState.Available:
								if (!hwEnum.Free)
									adapter.State = AdapterState.NotAvailable;
								break;
							case AdapterState.OpenShared:
							case AdapterState.OpenReserved:
								// Should be marked as "not free"
								// Flag an error if otherwise
								if (hwEnum.Free)
									throw new Exception("HW State: showing 'free' when it should be 'not free'.");
								break;
							case AdapterState.Removed:
								if (hwEnum.Free)
								{
									adapter.State = AdapterState.Reattached;
									if (adapter.Resync(hwEnum))
									{
										if (adapter.AccessMode == AdapterMode.Shared)
											adapter.State = AdapterState.OpenShared;
										else
											adapter.State = AdapterState.OpenReserved;
									}
									else
									{
										throw new Exception("Error trying to 'resync' the hardware adapter.");
									}
								}
								else
									adapter.State = AdapterState.ReattachedNotAvailable;
								break;
							case AdapterState.ReattachedNotAvailable:
								if (hwEnum.Free)
									adapter.State = AdapterState.Reattached;
								break;
							case AdapterState.Reattached:
								if (!hwEnum.Free)
									adapter.State = AdapterState.ReattachedNotAvailable;
								else
								{
									if (adapter.Resync(hwEnum))
									{
										if (adapter.AccessMode == AdapterMode.Shared)
											adapter.State = AdapterState.OpenShared;
										else
											adapter.State = AdapterState.OpenReserved;
									}
									else
									{
										throw new Exception("Error trying to 'resync' the hardware adapter.");
									}
								}
								break;
						}

						// Don't need to look through the rest of the enumerated adapters
						break;
					}
				}

				// Is the hardware still present?
				if (!stillPresent)
				{
					switch (adapter.State)
					{
						case AdapterState.NotPresent:
							break;
						case AdapterState.NotAvailable:
							adapter.State = AdapterState.NotPresent;
							break;
						case AdapterState.Available:
							adapter.State = AdapterState.NotPresent;
							break;
						case AdapterState.OpenShared:
						case AdapterState.OpenReserved:
							adapter.State = AdapterState.Removed;
							break;
						case AdapterState.Removed:
							break;
						case AdapterState.ReattachedNotAvailable:
							adapter.State = AdapterState.Removed;
							break;
						case AdapterState.Reattached:
							adapter.State = AdapterState.Removed;
							break;
					}
				}
			}

			// Create new adapters for each hw enumeration not claimed
			foreach (AardvarkHardwareEnumeration hwEnum in enumeratedAardvarkInstances)
			{
				if (!hwEnum.Claimed)
				{
					AardvarkAdapter newAdapter = new AardvarkAdapter(hwEnum);

					// Assign an index to the adapter
					newAdapter.Index = GetNextAdapterIndex();

					_aardvarkAdapters.Add(newAdapter);
				}
			}
		}

		/// <summary>
		/// Manages the "global" next adapter index value. The next index is returned
		/// and the counter is incremented for the next time.
		/// </summary>
		/// <returns>Index to be used for the newly created adapter.</returns>
		private int GetNextAdapterIndex()
		{
			return _nextAdapterIndex++;
		}
		#endregion
	}
}
