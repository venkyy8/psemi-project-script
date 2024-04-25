using System.Globalization;
using System.Xml.Linq;

namespace DeviceAccess
{
	public partial class Register
	{
		public class Bit
		{
			#region Public Readonly Fields

			public readonly string ID = "";
			public readonly string Name = "";
			public readonly string DisplayName = "";
			public readonly uint Mask = 0x0000;
			public readonly string Description = "";
			public readonly bool Private = false;
			public readonly string RegisterID = "";

			#endregion

			#region Constructor

			public Bit(string registerID, XElement field)
			{
				uint tempInteger;
				bool tempbool;

				RegisterID = registerID;

				if (field.Attribute("Name").Value != null)
				{
					Name = field.Attribute("Name").Value;
				}

				if (field.Attribute("DisplayName") != null)
				{
					DisplayName = field.Attribute("DisplayName").Value;
				}
				else if (field.Attribute("Name") != null)
				{
					DisplayName = Name;
				}

				ID = RegisterID + "_" + DisplayName.ToUpper().Replace(" ", string.Empty).Replace("-", string.Empty).Replace("_", string.Empty);

				if (field.Attribute("Mask") != null)
				{
					if (field.Attribute("Mask").Value.Length >= 2)
					{
						if (field.Attribute("Mask").Value.Substring(0, 2).ToUpper() == "0X")
						{
							if (uint.TryParse(field.Attribute("Mask").Value.Substring(2), NumberStyles.HexNumber, null, out tempInteger))
							{
								Mask = System.Convert.ToUInt16(field.Attribute("Mask").Value, 16);
							}
						}
						else
						{
							if (uint.TryParse(field.Attribute("Mask").Value, out tempInteger))
							{
								Mask = System.Convert.ToUInt16(field.Attribute("Mask").Value);
							}
						}
					}
					else
					{
						if (uint.TryParse(field.Attribute("Mask").Value, out tempInteger))
						{
							Mask = System.Convert.ToUInt16(field.Attribute("Mask").Value);
						}
					}
				}

				if (field.Attribute("Description") != null)
				{
					Description = field.Attribute("Description").Value;
				}

				if (field.Attribute("Private") != null)
				{
					if (bool.TryParse(field.Attribute("Private").Value, out tempbool))
					{
						Private = System.Convert.ToBoolean(field.Attribute("Private").Value);
					}
				}
			}

			#endregion

			#region Public Methods

			public Bit(Bit deepCopyField, bool setReserved = true)
			{
				if (setReserved)
				{
					Name = "RSVD";
					DisplayName = Name;
					ID = RegisterID + "_" + DisplayName.ToUpper().Replace(" ", string.Empty).Replace("-", string.Empty).Replace("_", string.Empty);
					Mask = deepCopyField.Mask;
					Description = "Reserved.";
					Private = deepCopyField.Private;

					RegisterID = deepCopyField.RegisterID;
				}
				else
				{
					ID = deepCopyField.ID;
					Name = deepCopyField.Name;
					DisplayName = deepCopyField.DisplayName;
					Mask = deepCopyField.Mask;
					Description = deepCopyField.Description;
					Private = deepCopyField.Private;

					RegisterID = deepCopyField.RegisterID;
				}
			}

			#endregion
		}
	}
}
