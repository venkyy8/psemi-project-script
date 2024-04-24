using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace HardwareInterfaces
{
	public partial class Register
	{
		public class Bit : INotifyPropertyChanged
		{
			#region Private Members

			private string _id = "";
			private string _name = "";
			private string _displayName = "";
			private uint _mask = 0x0000;
			private string _description = "";
			private bool _private = false;
			private string _registerID = "";
			private bool _lastReadValue = false;

			#endregion

			#region Public Readonly Fields

			public string ID
			{
				get { return _id; }

				set
				{
					if (_id != value)
					{
						_id = value;
						OnPropertyChanged("ID");
					}
				}
			}

			public string Name
			{
				get { return _name; }

				set
				{
					if (_name != value)
					{
						_name = value;
						OnPropertyChanged("Name");
					}
				}
			}

			public string DisplayName
			{
				get { return _displayName; }

				set
				{
					if (_displayName != value)
					{
						_displayName = value;
						OnPropertyChanged("DisplayName");
					}
				}
			}

			public uint Mask
			{
				get { return _mask; }

				internal set
				{
					if (_mask != value)
					{
						_mask = value;
						OnPropertyChanged("Mask");
					}
				}
			}

			public string Description
			{
				get { return _description.Replace("\\n", Environment.NewLine); }

				set
				{
					if (_description != value)
					{
						_description = value;
						OnPropertyChanged("Description");
					}
				}
			}

			public bool Private
			{
				get { return _private; }

				set
				{
					if (_private != value)
					{
						_private = value;
						OnPropertyChanged("Private");
					}
				}
			}

			public string RegisterID
			{
				get { return _registerID; }

				set
				{
					if (_registerID != value)
					{
						_registerID = value;
						OnPropertyChanged("RegisterID");
					}
				}
			}

			#endregion

			#region Constructor

			public Bit(string registerID, XElement field)
			{
				uint tempInteger;
				bool tempbool;

				_registerID = registerID;

				if (field.Attribute("Name").Value != null)
				{
					_name = field.Attribute("Name").Value;
				}

				if (field.Attribute("DisplayName") != null)
				{
					_displayName = field.Attribute("DisplayName").Value;
				}
				else if (field.Attribute("Name") != null)
				{
					_displayName = Name;
				}

				_id = RegisterID + "_" + DisplayName.ToUpper().Replace(" ", string.Empty).Replace("-", string.Empty).Replace("_", string.Empty);

				if (field.Attribute("Mask") != null)
				{
					if (field.Attribute("Mask").Value.Length >= 2)
					{
						if (field.Attribute("Mask").Value.Substring(0, 2).ToUpper() == "0X")
						{
							if (uint.TryParse(field.Attribute("Mask").Value.Substring(2), NumberStyles.HexNumber, null, out tempInteger))
							{
								_mask = Convert.ToUInt16(field.Attribute("Mask").Value, 16);
							}
						}
						else
						{
							if (uint.TryParse(field.Attribute("Mask").Value, out tempInteger))
							{
								_mask = Convert.ToUInt16(field.Attribute("Mask").Value);
							}
						}
					}
					else
					{
						if (uint.TryParse(field.Attribute("Mask").Value, out tempInteger))
						{
							_mask = Convert.ToUInt16(field.Attribute("Mask").Value);
						}
					}
				}

				if (field.Attribute("Description") != null)
				{
					_description = field.Attribute("Description").Value;
				}

				if (field.Attribute("Private") != null)
				{
					if (bool.TryParse(field.Attribute("Private").Value, out tempbool))
					{
						_private = Convert.ToBoolean(field.Attribute("Private").Value);
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

			public bool LastReadValue
			{
				get { return _lastReadValue; }
				set
				{
					if (_lastReadValue != value)
					{
						_lastReadValue = value;
						OnPropertyChanged("LastReadValue");
					}
				}
			}

			#endregion

			#region INotificationPropertyChanged Interface

			public event PropertyChangedEventHandler PropertyChanged;

			public void OnPropertyChanged([CallerMemberName] string propertyName = "")
			{
				if (PropertyChanged != null)
				{
					PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
				}
			}

			#endregion
		}
	}
}
