using System.Xml.Linq;

namespace HardwareInterfaces
{
	public interface II2cConfiguration
	{
		XElement I2cGetConfigurationXml(string elementLabel, XDocument doc);
		void I2cSetConfigurationXml(XNode config);
	}
}
