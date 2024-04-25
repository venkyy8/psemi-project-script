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
using System.Xml;
using Serilog;

namespace AdapterAccess.Adapters
{
    public class VirtualAdapter : Adapter, IAdapter, IVirtual
    {
        private List<string> _pluginCompatibility = new List<string>();
        private VirtualCapabilities _VirtualCapabilities;

        public List<Register> Registers { get; set; }

        #region Overrides

        public string AdapterName
        {
            get { return "Virtual Adapter"; }
        }

        public string AdapterVersion
        {
            get { return "1.0"; }
        }

        public string AdapterSerialNumber
        {
            get { return "0001"; }
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
            if (interfaceType == typeof(IVirtual))
            {
                return true;
            }
            return false;
        }

        public override string ToString()
        {
            return AdapterName + " - S/N: " + AdapterSerialNumber;
        }

        #endregion

        #region Virtual Interface

        public void VirtualSetCapabilities(VirtualCapabilities config)
        {
            _VirtualCapabilities = config;
        }

        public VirtualCapabilities VirtualGetCapabilities()
        {
            return _VirtualCapabilities;
        }

        public VirtualConfiguration VirtualGetConfiguration()
        {
            return new VirtualConfiguration
            {
                BitRate = 100000,
                BusLockTimeoutMs = 0,
                PullupsEnabled = false
            };
        }

        public bool VirtualSetBitRate(int bitRate)
        {
            return true;
        }

        public bool VirtualSetDriveByZero(int drive)
        {
            return true;
        }

        public void VirtualSetConfiguration(VirtualConfiguration config)
        {
        }

        public void VirtualSendByte(byte targetAddress, byte data)
        {
        }

        public void VirtualEnable()
        {
        }

        public void VirtualDisable()
        {
        }

        public byte VirtualReadByte(byte targetAddress, byte address, string deviceFileName, bool isAdapterControl = false)
        {
            byte[] data = new byte[1];
            VirtualReadBlock(targetAddress, address, 1, ref data, deviceFileName, isAdapterControl);
            return data[0];
        }

        public void VirtualWriteByte(byte targetAddress, byte address, byte data, string deviceFileName)
        {
            //WriteData(address, data);

            byte[] bdata = new byte[] { data };
            VirtualWriteBlock(targetAddress, address, 1, ref bdata, deviceFileName);
        }

        public void VirtualWriteBlock(byte targetAddress, byte? address, int length, ref byte[] data, string deviceFileName)
        {
            // write code to send value to Register.xml in c drive , device folder
            WriteData(address, data, length, deviceFileName);
        }

        public void VirtualReadBlock(byte targetAddress, byte address, int length, ref byte[] data, string deviceFileName, bool isAdapterControl = false)
        {
            ReadData(address, ref data, length, deviceFileName, isAdapterControl);
        }

        public void VirtualReadBlock(byte targetAddress, ushort address, int length, ref byte[] data, string deviceFileName, bool isAdapterControl = false)
        {
            ReadData(address, ref data, length, deviceFileName, isAdapterControl);
        }

        public void VirtualRead16Bit(byte targetAddress, ushort address, int length, ref byte[] data, string deviceFileName)
        {
            Read16Data(address, ref data, length, deviceFileName);// Read code to send value to Register.xml in c drive , device folder
        }

        public void VirtualWrite16Bit(byte targetAddress, ushort? address, int length, ref byte[] data, string deviceFileName)
        {
            Write16Data(address, data, length, deviceFileName);// write code to send value to Register.xml in c drive , device folder
        }

        public void VirtualSendStop()
        {
        }

        public bool IsScanAllAddresses
        {
            get { return true; }
        }

        public byte DefaultSlaveAddress
        {
            get { return 0x30; }
        }

        public void SetGpio(byte value)
        {
        }

        #endregion

        #region Data

        private void WriteData(uint? address, byte[] data, int length, string deviceFileName = "")
        {
            int raddress = int.Parse(address.ToString());
            string dataToWrite = BitConverter.ToString(data).Replace("-", "");

            Int64 intValue = Int64.Parse(dataToWrite, System.Globalization.NumberStyles.HexNumber);

            string registerStartAddress = null;
            registerStartAddress = "0X" + int.Parse(address.ToString()).ToString("X2");

            string currentRegAddress = registerStartAddress;

            try
            {
                string result = BitConverter.ToString(data.ToArray()).Replace("-", string.Empty);

                string path = @"C:\Devices\" + deviceFileName + ".xml";

                Log.Debug("VirtualAdapter WriteData " + " Register Address :" + registerStartAddress.ToString() +
                        " deviceFileName : " + deviceFileName + " Data[0]: " + result + " Length : " + length + "Path : " + path);


                XmlDocument doc = new XmlDocument();

                doc.Load(path);

                XmlNodeList nodes = doc.DocumentElement.SelectNodes("//Registers//Register");
                XDocument xDoc = XDocument.Load(path);
                IEnumerable<XElement> regNodes = xDoc.Descendants("Register");

                int byteCount = 0;
                int decValue = int.Parse(int.Parse(raddress.ToString()).ToString("X2"), System.Globalization.NumberStyles.HexNumber);

                foreach (XmlNode node in nodes)
                {
                    string regNodeAddress = node.Attributes["Address"].Value;

                    if (currentRegAddress.ToLower() == regNodeAddress.ToLower())
                    {
                        int regSize = 1;
                        bool isSuccess = int.TryParse(node.Attributes["Size"].Value, out regSize);
                        if (regSize == 0)
                        {
                            regSize = 1;
                        }

                        if (regSize >= data.Length)
                        {
                            if (regSize == 1)
                            {
                                node.Attributes["LastReadValue"].Value = data[0].ToString();
                            }
                            else if (regSize == 2)
                            {
                                node.Attributes["LastReadValue"].Value = intValue.ToString();
                            }

                            break;
                        }
                        else if (regSize < data.Length)
                        {
                            for (int i = byteCount; i < data.Length; i++)
                            {
                                string value = data[i].ToString();
                                node.Attributes["LastReadValue"].Value = value;
                                decValue = int.Parse(int.Parse(raddress.ToString()).ToString("X2"), System.Globalization.NumberStyles.HexNumber);
                                raddress = decValue + 1;
                                currentRegAddress = "0X" + raddress.ToString("X2");
                                
                                currentRegAddress = CheckRegisterInNodes(regNodes, currentRegAddress, ref decValue, ref raddress, ref byteCount);

                                byteCount += regSize;
                                break;
                            }
                        }


                    }
                }

                doc.Save(path);
            }
            catch (Exception ex)
            {
                if (String.IsNullOrEmpty(deviceFileName))
                {
                    Log.Error(" Device file name is empty: (" + registerStartAddress + ")", ex);
                    throw new Exception(" Device file name is empty: (" + registerStartAddress + ")", ex);
                }

                Log.Error(" Virtual Adapter failed to write data to the register address: (" + registerStartAddress + ")", ex);

                throw new Exception(" Virtual Adapter failed to write data to the register address: (" + registerStartAddress + ")", ex);
            }
        }


        private void WriteData(byte? address, byte[] data, int length, string deviceFileName = "")
        {
            int raddress = int.Parse(address.ToString());
            string dataToWrite = BitConverter.ToString(data).Replace("-", "");

            Int64 intValue = Int64.Parse(dataToWrite, System.Globalization.NumberStyles.HexNumber);

            string registerStartAddress = null;
            registerStartAddress = "0X" + int.Parse(address.ToString()).ToString("X2");
            string currentRegAddress = registerStartAddress;

            try
            {
                string result = BitConverter.ToString(data.ToArray()).Replace("-", string.Empty);

                string path = @"C:\Devices\" + deviceFileName + ".xml";

                Log.Debug("VirtualAdapter WriteData " + " Register Address :" + registerStartAddress.ToString() +
                        " deviceFileName : " + deviceFileName + " Data[0]: " + result + " Length : " + length + "Path : " + path);


                XmlDocument doc = new XmlDocument();

                doc.Load(path);

                XmlNodeList nodes = doc.DocumentElement.SelectNodes("//Registers//Register");
                XDocument xDoc = XDocument.Load(path);
                IEnumerable<XElement> regNodes = xDoc.Descendants("Register");

                int byteCount = 0;
                int decValue = int.Parse(int.Parse(raddress.ToString()).ToString("X2"), System.Globalization.NumberStyles.HexNumber);

                foreach (XmlNode node in nodes)
                {
                    string regNodeAddress = node.Attributes["Address"].Value;

                    if (currentRegAddress.ToLower() == regNodeAddress.ToLower())
                    {
                        int regSize = 1;
                        bool isSuccess = int.TryParse(node.Attributes["Size"].Value, out regSize);
                        if (regSize == 0)
                        {
                           regSize = 1;
                        }

                        if (regSize >= data.Length)
                        {
                            if (regSize == 1)
                            {
                                node.Attributes["LastReadValue"].Value = data[0].ToString();
                            }
                            else if (regSize == 2)
                            {
                                node.Attributes["LastReadValue"].Value = intValue.ToString();
                            }

                            break;
                        }
                        else if (regSize < data.Length)
                        {
                            for (int i = byteCount; i < data.Length; i++)
                            {
                                string value = data[i].ToString();
                                node.Attributes["LastReadValue"].Value = value;
                                decValue = int.Parse(int.Parse(raddress.ToString()).ToString("X2"), System.Globalization.NumberStyles.HexNumber);
                                raddress = decValue + 1;
                                currentRegAddress = "0X" + raddress.ToString("X2");
                                
                                currentRegAddress = CheckRegisterInNodes(regNodes, currentRegAddress, ref decValue, ref raddress, ref byteCount);

                                byteCount += regSize;
                                break;
                            }
                        }


                    }
                }

                doc.Save(path);
            }
            catch (Exception ex)
            {
                if (String.IsNullOrEmpty(deviceFileName))
                {
                    Log.Error(" Device file name is empty: (" + registerStartAddress + ")", ex);
                    throw new Exception(" Device file name is empty: (" + registerStartAddress + ")", ex);
                }

                Log.Error(" Virtual Adapter failed to write data to the register address: (" + registerStartAddress + ")", ex);

                throw new Exception(" Virtual Adapter failed to write data to the register address: (" + registerStartAddress + ")", ex);
            }
        }

        public string CheckRegisterInNodes(IEnumerable<XElement> regNodes, string currentRegAddress, ref int decValue, ref int raddress, ref int byteCount)
        {
            IEnumerable<XElement> regAddressExists = regNodes.Where(p => p.Attribute("Address").Value.ToLower() == currentRegAddress.ToLower());
            string regAdd = string.Empty;

            if (!regAddressExists.Any())
            {
                decValue = int.Parse(int.Parse(raddress.ToString()).ToString("X2"), System.Globalization.NumberStyles.HexNumber);
                raddress = decValue + 1;
                byteCount += 1;
                currentRegAddress = "0X" + raddress.ToString("X2");
                regAdd = CheckRegisterInNodes(regNodes, currentRegAddress, ref decValue, ref raddress, ref byteCount);
            }
            else
            {
                regAdd = currentRegAddress;
            }

            return regAdd;
        }

        public string CheckRegisterInReadingNodes(IEnumerable<XElement> regNodes, string currentRegAddress, ref int decValue, ref int raddress, ref string hexData, int byteCount)
        {
            IEnumerable<XElement> regAddressExists = regNodes.Where(p => p.Attribute("Address").Value.ToLower() == currentRegAddress.ToLower());
            string regAdd = string.Empty;

            if (!regAddressExists.Any())
            {
                decValue = int.Parse(int.Parse(raddress.ToString()).ToString("X2"), System.Globalization.NumberStyles.HexNumber);
                raddress = decValue + 1;
                currentRegAddress = "0X" + raddress.ToString("X2");

                int number = 0;
                hexData += number.ToString("X2");

                if (hexData.Length / 2 >= byteCount)
                {
                    return regAdd;
                }
                else
                {
                    regAdd = CheckRegisterInReadingNodes(regNodes, currentRegAddress, ref decValue, ref raddress, ref hexData, byteCount);
                }
            }
            else
            {
                regAdd = currentRegAddress;
            }

            return regAdd;
        }

        //private void ReadData(byte? address, ref byte[] data, int length, string deviceFileName = "")
        //{
        //	string registerAddress = "0X" + int.Parse(address.ToString()).ToString("X2");

        //	try
        //	{

        //		string path = @"C:\Devices\" + deviceFileName + ".xml";

        //		XmlDocument doc = new XmlDocument();

        //		doc.Load(path);

        //		XmlNodeList nodes = doc.DocumentElement.SelectNodes("//Registers//Register");

        //		foreach (XmlNode node in nodes)
        //		{
        //			string regAddress = node.Attributes["Address"].Value;

        //			if (registerAddress.ToLower() == regAddress.ToLower())
        //			{
        //				byte[] writeValue = new byte[length];

        //				var hexValue = node.Attributes["LastReadValue"].Value;
        //				int decValue = int.Parse(hexValue, System.Globalization.NumberStyles.HexNumber);

        //				//uint rawValue2 = Convert.ToUInt32(decValue);
        //				//var ushortVal = (ushort)(rawValue2 & 0xFFFF);
        //				//writeValue = BitConverter.GetBytes(ushortVal);

        //				//if(length > 2)
        //    //                  {
        //				//	var value = (uint)(rawValue2 & 0xFFFF);
        //				//	writeValue = BitConverter.GetBytes(value);
        //				//}

        //                      for (int i = 0; i < hexValue.ToString().Length / 2; i++)
        //                      {
        //                          writeValue[i] = Convert.ToByte(hexValue.ToString().Substring(i * 2, 2), 16);
        //                      }

        //                      data = writeValue;

        //				break;
        //			}
        //		}
        //	}
        //	catch (Exception ex)
        //	{
        //		Log.Error(" Virtual Adapter failed to read data from the register address: (" + registerAddress + ")", ex);
        //		throw new Exception(" Virtual Adapter failed to read data from the register address: (" + registerAddress + ")", ex);
        //	}

        //}


        private void ReadData(byte? address, ref byte[] data, int length, string deviceFileName = "", bool isAdapterControl = false)
        {
            int raddress = int.Parse(address.ToString());
            string registerStartAddress = null;
            registerStartAddress = "0X" + int.Parse(address.ToString()).ToString("X2");
            
            string currentRegAddress = registerStartAddress;

            try
            {

                string path = @"C:\Devices\" + deviceFileName + ".xml";

                XmlDocument doc = new XmlDocument();

                doc.Load(path);
                List<XmlNode> regAddressList = new List<XmlNode>();
                byte[] writeValue = new byte[2];

                string hexValuesString = string.Empty;
                int decValue = int.Parse(int.Parse(raddress.ToString()).ToString("X2"), System.Globalization.NumberStyles.HexNumber);
                int byteCount = length;

                XmlNodeList nodes = doc.DocumentElement.SelectNodes("//Registers//Register");
                XDocument xDoc = XDocument.Load(path);
                IEnumerable<XElement> regNodes = xDoc.Descendants("Register");

                foreach (XmlNode node in nodes)
                {
                    string regAddress = node.Attributes["Address"].Value;

                    if (currentRegAddress.ToLower() == regAddress.ToLower())
                    {
                        string deciValue = node.Attributes["LastReadValue"].Value;
                        string hexData = string.Empty;
                        int number = int.Parse(deciValue);

                        int regSize = 1;
                        bool isSuccess = int.TryParse(node.Attributes["Size"].Value, out regSize);

                        if (isSuccess)
                        {
                            if ((Convert.ToInt32(node.Attributes["Size"].Value)) == 2)
                            {
                                hexData = number.ToString("X4");

                                if (isAdapterControl)
                                {
                                    for (int i = 0; i < hexData.Length / 2; i++)
                                    {
                                        writeValue[i] = Convert.ToByte(hexData.Substring(i * 2, 2), 16);
                                    }

                                    hexData = BitConverter.ToString(writeValue.Reverse().ToArray()).Replace("-", "");
                                }
                            }
                            else
                            {
                                hexData = number.ToString("X2");
                            }
                        }
                        else
                        {
                            hexData = number.ToString("X2");
                        }

                        hexValuesString += hexData;

                        if (hexValuesString.Length / 2 >= length)
                        {
                            break;
                        }

                        decValue = int.Parse(int.Parse(raddress.ToString()).ToString("X2"), System.Globalization.NumberStyles.HexNumber);
                        raddress = decValue + 1;
                        currentRegAddress = "0X" + raddress.ToString("X2");
                        
                        currentRegAddress = CheckRegisterInReadingNodes(regNodes, currentRegAddress, ref decValue, ref raddress, ref hexValuesString, length);

                        if (hexValuesString.Length / 2 >= length)
                        {
                            break;
                        }
                        else
                        {
                            continue;
                        }

                        //if ((Convert.ToInt32(node.Attributes["Size"].Value)) == 2)
                        //{
                        //    data = writeValue.Reverse().ToArray();
                        //}
                    }
                }

                data = new byte[length];

                for (int i = 0; i < hexValuesString.Length / 2; i++)
                {
                    //if (i <= length)
                    if (i < length)
                    {
                        data[i] = Convert.ToByte(hexValuesString.ToString().Substring(i * 2, 2), 16);
                    }
                }

                //if (regAddressList.Any())
                //{
                //	if (writeValue.Length != length)
                //	{
                //		var decValue = node.Attributes["LastReadValue"].Value;
                //		int number = int.Parse(decValue);
                //		string hexData = number.ToString("X2");
                //		hexValuesString += hexData;
                //		writeValue = new byte[hexValuesString.Length / 2];

                //		for (int i = 0; i < hexValuesString.Length / 2; i++)
                //		{
                //			if (i + 1 <= length)
                //			{
                //				writeValue[i] = Convert.ToByte(hexValuesString.ToString().Substring(i * 2, 2));
                //			}
                //		}

                //		if ((Convert.ToInt32(node.Attributes["Size"].Value)) == 2 && !isAdapterControl)
                //		{
                //			data = writeValue.Reverse().ToArray();
                //		}
                //		else
                //		{
                //			data = writeValue;
                //		}

                //		if (writeValue.Length == length)
                //		{
                //			data = writeValue;
                //			break;
                //		}
                //		else
                //		{


                //			continue;
                //		}
                //	}
                //	else
                //	{
                //		break;
                //	}
                //}

                //if (registerStartAddress.ToLower() == regAddress.ToLower())
                //{
                //	if ((Convert.ToInt32(node.Attributes["Size"].Value)) == 2)
                //	{
                //		writeValue = new byte[2];
                //	}
                //	else
                //	{
                //		writeValue = new byte[1];
                //	}

                //	regAddressList.Add(node);

                //	string decValue = node.Attributes["LastReadValue"].Value;

                //	Byte tempValueU8 = (byte)Convert.ToInt32(decValue);

                //	writeValue[0] = tempValueU8;

                //	//for (int i = 0; i < decValue.ToString().Length / 2; i++)
                //	//{
                //	//	writeValue[i] = Convert.ToByte(decValue.ToString().Substring(i * 2, 2));
                //	//}

                //	if ((Convert.ToInt32(node.Attributes["Size"].Value)) == 2)
                //	{
                //		data = writeValue.Reverse().ToArray();
                //	}
                //	else
                //                   {
                //		data = writeValue;
                //	}

                //	if (writeValue.Length >= length)
                //	{
                //		return;
                //	}
                //	else
                //	{
                //		int number = int.Parse(decValue);
                //		string hexData = number.ToString("X2");
                //		hexValuesString += hexData;
                //		continue;
                //	}
                //}


                //writeValue = new byte[length];


                //foreach (XmlNode xmlNode in regAddressList)
                //{
                //	var hexV = xmlNode.Attributes["LastReadValue"].Value;
                //	hexValuesString += hexV;
                //}

                //for (int i = 0; i < hexValuesString.Length / 2; i++)
                //{
                //	if (i + 1 <= length)
                //	{
                //		writeValue[i] = Convert.ToByte(hexValuesString.ToString().Substring(i * 2, 2), 16);
                //	}
                //}

                //data = writeValue;
            }
            catch (Exception ex)
            {
                Log.Error(" Virtual Adapter failed to read data from the register address: (" + registerStartAddress + ")", ex);
                throw new Exception(" Virtual Adapter failed to read data from the register address: (" + registerStartAddress + ")", ex);
            }

        }

        private void ReadData(uint? address, ref byte[] data, int length, string deviceFileName = "", bool isAdapterControl = false)
        {
            int raddress = int.Parse(address.ToString());
            string registerStartAddress = null;
            registerStartAddress = "0X" + int.Parse(address.ToString()).ToString("X2");
            
            string currentRegAddress = registerStartAddress;

            try
            {

                string path = @"C:\Devices\" + deviceFileName + ".xml";

                XmlDocument doc = new XmlDocument();

                doc.Load(path);
                List<XmlNode> regAddressList = new List<XmlNode>();
                byte[] writeValue = new byte[2];

                string hexValuesString = string.Empty;
                int decValue = int.Parse(int.Parse(raddress.ToString()).ToString("X2"), System.Globalization.NumberStyles.HexNumber);
                int byteCount = length;

                XmlNodeList nodes = doc.DocumentElement.SelectNodes("//Registers//Register");
                XDocument xDoc = XDocument.Load(path);
                IEnumerable<XElement> regNodes = xDoc.Descendants("Register");

                foreach (XmlNode node in nodes)
                {
                    string regAddress = node.Attributes["Address"].Value;

                    if (currentRegAddress.ToLower() == regAddress.ToLower())
                    {
                        string deciValue = node.Attributes["LastReadValue"].Value;
                        string hexData = string.Empty;
                        int number = int.Parse(deciValue);

                        int regSize = 1;
                        bool isSuccess = int.TryParse(node.Attributes["Size"].Value, out regSize);

                        if (isSuccess)
                        {
                            if ((Convert.ToInt32(node.Attributes["Size"].Value)) == 2)
                            {
                                hexData = number.ToString("X4");

                                if (isAdapterControl)
                                {
                                    for (int i = 0; i < hexData.Length / 2; i++)
                                    {
                                        writeValue[i] = Convert.ToByte(hexData.Substring(i * 2, 2), 16);
                                    }

                                    hexData = BitConverter.ToString(writeValue.Reverse().ToArray()).Replace("-", "");
                                }
                            }
                            else
                            {
                                hexData = number.ToString("X2");
                            }
                        }
                        else
                        {
                            hexData = number.ToString("X2");
                        }

                        hexValuesString += hexData;

                        if (hexValuesString.Length / 2 >= length)
                        {
                            break;
                        }

                        decValue = int.Parse(int.Parse(raddress.ToString()).ToString("X2"), System.Globalization.NumberStyles.HexNumber);
                        raddress = decValue + 1;
                        currentRegAddress = "0X" + raddress.ToString("X2");
                        
                        currentRegAddress = CheckRegisterInReadingNodes(regNodes, currentRegAddress, ref decValue, ref raddress, ref hexValuesString, length);

                        if (hexValuesString.Length / 2 >= length)
                        {
                            break;
                        }
                        else
                        {
                            continue;
                        }
                    }
                }

                data = new byte[length];

                for (int i = 0; i < hexValuesString.Length / 2; i++)
                {
                    if (i < length)
                    {
                        data[i] = Convert.ToByte(hexValuesString.ToString().Substring(i * 2, 2), 16);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(" Virtual Adapter failed to read data from the register address: (" + registerStartAddress + ")", ex);
                throw new Exception(" Virtual Adapter failed to read data from the register address: (" + registerStartAddress + ")", ex);
            }

        }


        //private void Read16Data(uint address, ref byte[] data, int length, string deviceFileName)
        //{
        //	string registerAddress = "0X" + int.Parse(address.ToString()).ToString("X3");

        //	try
        //	{

        //		string path = @"C:\Devices\" + deviceFileName + ".xml";

        //		XmlDocument doc = new XmlDocument();

        //		doc.Load(path);

        //		XmlNodeList nodes = doc.DocumentElement.SelectNodes("//I2CRegisters//Register");

        //		foreach (XmlNode node in nodes)
        //		{
        //			string regAddress = node.Attributes["Address"].Value;

        //			if (registerAddress.ToLower() == regAddress.ToLower())
        //			{
        //				byte[] writeValue = new byte[length];

        //				var hexValue = node.Attributes["LastReadValue"].Value;
        //				int decValue = int.Parse(hexValue, System.Globalization.NumberStyles.HexNumber);

        //				for (int i = 0; i < hexValue.ToString().Length / 2; i++)
        //				{
        //					writeValue[i] = Convert.ToByte(hexValue.ToString().Substring(i * 2, 2), 16);
        //				}

        //				data = writeValue;

        //				break;
        //			}
        //		}
        //	}
        //	catch (Exception ex)
        //	{
        //		Log.Error(" Virtual Adapter failed to read data from the register address: (" + registerAddress + ")", ex);
        //		throw new Exception(" Virtual Adapter failed to read data from the register address: (" + registerAddress + ")", ex);
        //	}
        //}

        private void Read16Data(uint address, ref byte[] data, int length, string deviceFileName)
        {
            string registerAddress = "0X" + int.Parse(address.ToString()).ToString("X3");

            try
            {
                string path = @"C:\Devices\" + deviceFileName + ".xml";

                XmlDocument doc = new XmlDocument();

                doc.Load(path);

                XmlNodeList nodes = doc.DocumentElement.SelectNodes("//I2CRegisters//Register");
                List<XmlNode> regAddressList = new List<XmlNode>();
                byte[] writeValue = new byte[2];


                foreach (XmlNode node in nodes)
                {
                    if (regAddressList.Any())
                    {
                        if (length % 2 + length / 2 != regAddressList.Count)
                        {
                            regAddressList.Add(node);
                        }
                        else
                        {
                            break;
                        }
                    }

                    string regAddress = node.Attributes["Address"].Value;

                    if (registerAddress.ToLower() == regAddress.ToLower())
                    {
                        regAddressList.Add(node);

                        var hexValue = node.Attributes["LastReadValue"].Value;
                        int decValue = int.Parse(hexValue, System.Globalization.NumberStyles.HexNumber);

                        for (int i = 0; i < hexValue.ToString().Length / 2; i++)
                        {
                            writeValue[i] = Convert.ToByte(hexValue.ToString().Substring(i * 2, 2), 16);
                        }

                        data = writeValue;

                        if (writeValue.Length == length)
                        {
                            return;
                        }
                        else
                            continue;
                    }
                }

                writeValue = new byte[length];
                string hexValuesString = string.Empty;

                foreach (XmlNode xmlNode in regAddressList)
                {
                    var hexV = xmlNode.Attributes["LastReadValue"].Value;
                    hexValuesString += hexV;
                }

                for (int i = 0; i < hexValuesString.Length / 2; i++)
                {
                    if (i + 1 <= length)
                    {
                        writeValue[i] = Convert.ToByte(hexValuesString.ToString().Substring(i * 2, 2), 16);
                    }
                }

                data = writeValue;

            }
            catch (Exception ex)
            {
                Log.Error(" Virtual Adapter failed to read data from the register address: (" + registerAddress + ")", ex);
                throw new Exception(" Virtual Adapter failed to read data from the register address: (" + registerAddress + ")", ex);
            }
        }


        private void Write16Data(uint? address, byte[] data, int length, string deviceFileName)
        {
            string registerAddress = "0X" + int.Parse(address.ToString()).ToString("X3");
            string currentRegAddress = registerAddress;

            int raddress = int.Parse(address.ToString());
            int decValue = int.Parse(int.Parse(raddress.ToString()).ToString("X2"), System.Globalization.NumberStyles.HexNumber);

            try
            {
                string result = BitConverter.ToString(data).Replace("-", string.Empty);

                string path = @"C:\Devices\" + deviceFileName + ".xml";

                Log.Debug("VirtualAdapter WriteData " + " Register Address :" + registerAddress.ToString() +
                        " deviceFileName : " + deviceFileName + " Data[0]: " + result + " Length : " + length + "Path : " + path);


                XmlDocument doc = new XmlDocument();
                doc.Load(path);
                XmlNodeList nodes = doc.DocumentElement.SelectNodes("//I2CRegisters//Register");

                XDocument xDoc = XDocument.Load(path);
                IEnumerable<XElement> regNodes = xDoc.Descendants("Register");

                int byteCount = result.Length / 2;
                int byteAssigned = 0;
                string newData = string.Empty;
                
                if (result.Length % 4 != 0)
                {
                    newData = String.Concat(result.Substring(0 , (result.Length / 4) * 4) + 
                                            (result.Substring((result.Length / 4) * 4).PadLeft(4,'0')));

                    result = newData;
                }

                int count = 0;

                //for (int i = 0; i < result.Length; i++)
                //{
                    foreach (XmlNode node in nodes)
                    {
                        string regAddress = node.Attributes["Address"].Value;

                        if (currentRegAddress.ToLower() == regAddress.ToLower())
                        {
                            node.Attributes["LastReadValue"].Value = result.Substring(count * 4, 4);
                            byteAssigned += result.Substring(count * 4, 4).Length / 2;

                            if (byteAssigned < byteCount)
                            {
                                decValue = int.Parse(int.Parse(raddress.ToString()).ToString("X2"), System.Globalization.NumberStyles.HexNumber);
                                raddress = decValue + 1;
                                currentRegAddress = "0X" + raddress.ToString("X2");

                                currentRegAddress = CheckRegisterInNodes(regNodes, currentRegAddress, ref decValue, ref raddress, ref byteAssigned);

                                count++;
                            }
                        }
                    }
                //}

                doc.Save(path);
            }
            catch (Exception ex)
            {
                if (String.IsNullOrEmpty(deviceFileName))
                {
                    Log.Error(" Device file name is empty: (" + registerAddress + ")", ex);
                    throw new Exception(" Device file name is empty: (" + registerAddress + ")", ex);
                }

                Log.Error(" Virtual Adapter failed to write data to the register address: (" + registerAddress + ")", ex);

                throw new Exception(" Virtual Adapter failed to write data to the register address: (" + registerAddress + ")", ex);
            }
        }

        #endregion

        #region Open and Close

        protected override bool OpenDriverInterface()
        {
            return true;
        }

        protected override void CloseDriverInterface()
        {
            return;
        }

        protected override bool ReconfigureInterface()
        {
            return true;
        }

        VirtualCapabilities IVirtual.VirtualGetCapabilities()
        {
            throw new NotImplementedException();
        }

        VirtualConfiguration IVirtual.VirtualGetConfiguration()
        {
            throw new NotImplementedException();
        }

        bool IVirtual.VirtualSetBitRate(int bitRate)
        {
            throw new NotImplementedException();
        }

        bool IVirtual.VirtualSetPec(bool pec)
        {
            throw new NotImplementedException();
        }

        bool IVirtual.VirtualSetDriveByZero(int drive)
        {
            throw new NotImplementedException();
        }

        void IVirtual.VirtualWriteBlock8bit3String(byte targetAddress, ushort? address, int length, ref byte[] data, string deviceFileName)
        {
            WriteData(address, data, length, deviceFileName);

            if (data.Length > 1)
            {
                Log.Debug("VirtualAdapter VirtualWriteBlock - TargetAddress : " + targetAddress.ToString() + " Register Address :" + address.ToString() +
                    " deviceFileName : " + deviceFileName + " Data[0]: " + data[0].ToString() + " Data[1] : " + data[1].ToString() + " Length : " + length);
            }
            else
            {
                Log.Debug("VirtualAdapter VirtualWriteBlock - TargetAddress : " + targetAddress.ToString() + " Register Address :" + address.ToString() +
                    " deviceFileName : " + deviceFileName + " Data[0]: " + data[0].ToString() + " Length : " + length);
            }
        }

        void IVirtual.VirtualWriteBlock(byte targetAddress, byte? address, int length, ref byte[] data, string deviceFileName)
        {
            WriteData(address, data, length, deviceFileName);

            if (data.Length > 1)
            {
                Log.Debug("VirtualAdapter VirtualWriteBlock - TargetAddress : " + targetAddress.ToString() + " Register Address :" + address.ToString() +
                    " deviceFileName : " + deviceFileName + " Data[0]: " + data[0].ToString() + " Data[1] : " + data[1].ToString() + " Length : " + length);
            }
            else
            {
                Log.Debug("VirtualAdapter VirtualWriteBlock - TargetAddress : " + targetAddress.ToString() + " Register Address :" + address.ToString() +
                    " deviceFileName : " + deviceFileName + " Data[0]: " + data[0].ToString() + " Length : " + length);
            }
        }

        void IVirtual.VirtualWrite16Bit(byte targetAddress, ushort? address, int length, ref byte[] data, string deviceFileName)
        {
            Write16Data(address, data, length, deviceFileName);
        }

        void IVirtual.VirtualRead16Bit(byte targetAddress, ushort address, int length, ref byte[] data, string deviceFileName)
        {
            Read16Data(address, ref data, length, deviceFileName);
        }

        void IVirtual.VirtualSendStop()
        {
            throw new NotImplementedException();
        }


        #endregion
    }
}
