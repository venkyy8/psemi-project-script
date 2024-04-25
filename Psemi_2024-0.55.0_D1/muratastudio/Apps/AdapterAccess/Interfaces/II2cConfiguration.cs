using System.Xml.Linq;

namespace AdapterAccess.Interfaces
{
	public interface II2cConfiguration
	{
		XElement I2cGetConfigurationXml(string elementLabel, XDocument doc);
		void I2cSetConfigurationXml(XNode config);
	}
}
