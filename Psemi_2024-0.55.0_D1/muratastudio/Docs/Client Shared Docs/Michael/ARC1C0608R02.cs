using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace DeviceAccess
{
	internal class ARC1C0608R02 : CustomizeBase
	{
		/*
			ARC1C0608BA brings the following changes to the CP frequency settings:
			Register 0x39<6:5> = OTP_CP_FREQB,OTP_CP_FREQA => sets the charge-pump frequency mode
			0x39<6:5> = 0:  CP fully automatic switching from 1/8th to 1/2 as load changes. Do not show any Charge-Chump Freq Ratio settings in the main screen or in the register map
			0x39<6:5> = 1 :Show CP Frequency settable to 1/8th, 1/4 and 1/2 using CP_FREQ_DIV0 and CP_FREQ_DIV1.  where 00=1/8th, 01 and 10=1/4, 11=1/2
			0x39<6:5> = 3 or 4:  Same as AB version. Show the "1/2 and 1/4.." and "1/4 and 1/8..." charge-pump settings in the main screen, and CP_FREQ_DIV0 in  the registers.
		*/

		private const byte ACCESS_REG = 0x41;
		private const byte UNLOCK = 0x37;
		private const byte LOCK = 0xFF;
		private const byte FREQ_REG = 0x39;
		private const byte CMD_REG = 0x00;
		private const byte CP_MASK = 0x60;	// <6:5>
		private const byte DCM_MASK = 0x20;	// <5>
		private byte _freq;

		public ARC1C0608R02(Device device)
			: base(device)
		{
		}

		public ARC1C0608R02(Device device, bool isDemo)
			: base(device, isDemo)
		{
		}
		
		#region Base Overrides

		public override void CustomizeDevice(bool isDevMode = false)
		{
			// Read the chips CP frequency settings register
			Device.WriteByte(ACCESS_REG, UNLOCK);	// Unlock device
			_freq = Device.ReadByte(FREQ_REG);		// Read register
			Device.WriteByte(ACCESS_REG, LOCK);		// Lock device

			// Get register value after applying the mask
			_freq = (byte)((_freq & CP_MASK) >> 5);

			switch (_freq)
			{
				case 0:
					RemoveChargePumpSettings();
					break;
				case 1:
					ModifyChargePumpSettings();
					break;
				case 2:
				case 3:
					DisplayChargePumpSettingsAsAB();
					break;
			}
		}

		public override void Unlock()
		{
			// Unlock master device
			this.Device.WriteByte(ACCESS_REG, UNLOCK);
		}

		#endregion

		#region Private Methods

		private void RemoveChargePumpSettings()
		{
			// Remove the settings from the register bits 5 & 6
			var reg = Device.Registers[1];
			var bit6 = reg.Bits[1];
			var bit5 = reg.Bits[2];
			bit6.Description = bit5.Description = "Reserved";
			bit6.DisplayName = bit5.DisplayName = "Reserved";
			bit6.ID = bit5.ID = "CONFIG_RESERVED";
			bit6.Name = bit5.Name = "Reserved";

			// Remove the UI Control
			Device.UiElements.Element("Panels")
				.Elements("Panel").FirstOrDefault(a => a.Attribute("Name").Value == "Configuration")
				.Elements("List").FirstOrDefault(a => a.Attribute("Label").Value == "Charge-pump Freq Ratio").Remove();
		}

		private void ModifyChargePumpSettings()
		{
			var uiControl = Device.UiElements.Element("Panels")
								.Elements("Panel").FirstOrDefault(a => a.Attribute("Name").Value == "Configuration")
								.Elements("List").FirstOrDefault(a => a.Attribute("Label").Value == "Charge-pump Freq Ratio");

			// Change the MASK
			uiControl.Attribute("Mask").Value = "0x60";

			// Remove the default configuration nodes
			uiControl.RemoveNodes();

			// Add new list of nodes
			// Since value 1 and 2 both equal the same setting, we are forced to list the option twice
			uiControl.Add(new XElement("Option", new XAttribute("Label", "1/8 of boost frequency"), new XAttribute("Value", "0")));
			uiControl.Add(new XElement("Option", new XAttribute("Label", "1/4 of boost frequency"), new XAttribute("Value", "1")));
			uiControl.Add(new XElement("Option", new XAttribute("Label", "1/4 of boost frequency"), new XAttribute("Value", "2")));
			uiControl.Add(new XElement("Option", new XAttribute("Label", "1/2 of boost frequency"), new XAttribute("Value", "3")));
		}

		private void DisplayChargePumpSettingsAsAB()
		{
			// Remove the settings from the register at bit 6
			var reg = Device.Registers[1];
			var bit6 = reg.Bits[1];
			var bit5 = reg.Bits[2];
			bit6.Description = "Reserved";
			bit6.DisplayName = "Reserved";
			bit6.ID = "CONFIG_RESERVED";
			bit6.Name = "Reserved";
		}

		#endregion
	}
}
