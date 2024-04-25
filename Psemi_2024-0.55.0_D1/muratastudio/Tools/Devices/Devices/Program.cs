
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Devices
{
	class Program
	{
		static void Main(string[] args)
        {
            CreateDummyDevice();

        }

        private static void CreateDummyDevice()
        {
            string folderName = @"c:\Devices";
            System.IO.Directory.CreateDirectory(folderName);
            string fileNameWrite = "Devices.xml";
            string path = System.IO.Path.Combine(folderName, fileNameWrite);
            string source_path = @"C:\Users\Admin\Desktop\ARC2CC06-R03.xml";

            XDocument deviceConfig = null;
            deviceConfig = XDocument.Load(source_path);
            IEnumerable<XElement> registersData = deviceConfig.Descendants("Registers");
            List<XElement> reg = registersData.ToList();
            IEnumerable<XElement> registersData1 = deviceConfig.Descendants("ChildRegisters");
            List<XElement> reg1 = registersData1.ToList();
            reg.AddRange(reg1);

            XmlSerializeRegisterMap(path, reg);

            string newValues = "00";
            string xPath = @"C:\Devices\Devices.xml";
            XmlDocument xDoc = new XmlDocument();
            xDoc.Load(xPath);

            XmlElement root = xDoc.DocumentElement;
            XmlNodeList nodes = root.SelectNodes("//Register");
            XmlNodeList nodes1 = root.SelectNodes("//ChildRegisters//Register");

            foreach (XmlNode node in nodes)
            {
                foreach (XmlNode node1 in nodes1)
                {
                    //Removing Attributes from nodes of Registers & Child Registers
                    node.Attributes.RemoveNamedItem("DataType");
                    node.Attributes.RemoveNamedItem("Size");
                    node.Attributes.RemoveNamedItem("Format");
                    node.Attributes.RemoveNamedItem("LockBitMask");
                    node.Attributes.RemoveNamedItem("TriggeredRead");
                    node.Attributes.RemoveNamedItem("Unit");
                    node.Attributes.RemoveNamedItem("ReadOnly");
                    node.Attributes.RemoveNamedItem("Private");
                    node.Attributes.RemoveNamedItem("Description");

                    //Adding a new Attribute LastReadValue which will be assigned as 00 initially and
                    //appended to the Register & Child Register nodes

                    XmlAttribute xKey = xDoc.CreateAttribute("LastReadValue");
                    xKey.Value = newValues;
                    node.Attributes.Append(xKey);
                }
            }

            xDoc.Save(xPath);

            List<Byte> data1 = new List<Byte> { 20, 30, 40, 50, 60 };

            foreach (Byte data in data1)
            {
                MethodToRead(48, 10, data);
            }
        }

        private static void MethodToRead(byte targetAddress, byte address, byte data)
        {
			string registerAddress = "0X" + int.Parse(address.ToString()).ToString("X2");
            
			string path = @"C:\Devices\Devices.xml";

			XmlDocument doc = new XmlDocument();
			doc.Load(path);

			XmlElement root = doc.DocumentElement;
			XmlNodeList nodes = root.SelectNodes("//ChildRegisters//Register");

			foreach (XmlNode node in nodes)
			{
                string regAddress = node.Attributes["Address"].Value;

				if (registerAddress == regAddress)
				{
					node.Attributes["LastReadValue"].Value = data.ToString();
					break;
				}
			}

			doc.Save(path);
		}

        private static void XmlSerializeRegisterMap(string path, List<XElement> reg)
		{
			FileStream fs = new FileStream(path, FileMode.Create);
			XmlSerializer formatter = new XmlSerializer(typeof(List<XElement>));
			try
			{
				formatter.Serialize(fs, reg);
			}
			catch (InvalidOperationException e)
			{
				Console.WriteLine("Failed to serialize register map.\r\nReason: " + e.Message);
				Console.ReadLine();
			}
			finally
			{
				fs.Close();
			}
		}
	}
}
		