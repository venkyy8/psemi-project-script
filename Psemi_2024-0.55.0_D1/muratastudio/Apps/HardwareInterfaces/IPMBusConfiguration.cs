using System.Xml.Linq;

namespace HardwareInterfaces
{
	public interface IPMBusConfiguration
	{
		XElement I2cGetConfigurationXml(string elementLabel, XDocument doc);
		void I2cSetConfigurationXml(XNode config);
	}
}
