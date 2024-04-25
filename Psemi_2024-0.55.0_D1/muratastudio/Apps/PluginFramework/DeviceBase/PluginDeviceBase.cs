using HardwareInterfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace PluginFramework
{
	public abstract class PluginDeviceBase : DependencyObject, INotifyPropertyChanged
	{
		#region Public Methods

		public int CountSetBits(int value)
		{
			return Convert.ToString(value, 2).ToCharArray().Count(c => c == '1');
		}

		public int ConvertHexToInt(string hexValue)
		{
			string hex = (hexValue.ToUpper().Contains("0X")) ? hexValue.Substring(2) : hexValue;
			return Convert.ToInt32(hex, 16);
		}

		public int GetMaskedValue(int mask, int value)
		{
			int i = 0;
			for (i = 0; i < 8; i++)
			{
				if ((mask & 1) == 1)
				{
					break;
				}
				else
				{
					mask = (mask >> 1);
				}
			}

			return value << i;
		}

		public bool IsDigit(char ch)
		{
			string hexCharacter = "0123456789";
			return hexCharacter.Contains(ch);
		}

		#endregion

		#region Protected Virtual

		protected virtual void SerializeRegisterMap(string fileName, RegisterMap map)
		{
			FileStream fs = new FileStream(fileName, FileMode.Create);
			BinaryFormatter formatter = new BinaryFormatter();
			try
			{
				formatter.Serialize(fs, map);
			}
			catch (SerializationException e)
			{
				MessageBox.Show("Failed to serialize register map.\r\nReason: " + e.Message);
			}
			finally
			{
				fs.Close();
			}
		}

		protected virtual void XmlSerializeRegisterMap(string fileName, RegisterMap map)
		{
			FileStream fs = new FileStream(fileName, FileMode.Create);
			XmlSerializer formatter = new XmlSerializer(typeof(RegisterMap), new Type[] { typeof(Map), typeof(SystemData) });
			try
			{
				formatter.Serialize(fs, map);
			}
			catch (InvalidOperationException e)
			{
				MessageBox.Show("Failed to serialize register map.\r\nReason: " + e.Message);
			}
			finally
			{
				fs.Close();
			}
		}

		protected virtual RegisterMap DeserializeRegisterMap(string fileName)
		{
			RegisterMap map = null;
			FileStream fs = new FileStream(fileName, FileMode.Open);
			try
			{
				BinaryFormatter formatter = new BinaryFormatter();
				map = (RegisterMap)formatter.Deserialize(fs);
			}
			catch (SerializationException e)
			{
				Console.WriteLine("Failed to deserialize register map.\r\nReason: " + e.Message);
			}
			finally
			{
				fs.Close();
			}
			return map;
		}

		protected virtual RegisterMap XmlDeserializeRegisterMap(string fileName)
		{
			RegisterMap map = null;
			FileStream fs = new FileStream(fileName, FileMode.Open);
			XmlSerializer formatter = new XmlSerializer(typeof(RegisterMap), new Type[] { typeof(Map), typeof(SystemData) });
			try
			{
				map = (RegisterMap)formatter.Deserialize(fs);
			}
			catch (InvalidOperationException e)
			{
				map = null;
				MessageBox.Show("Failed to deserialize register map.\r\nReason: " + e.Message);
			}
			finally
			{
				fs.Close();
			}

			return map;
		}

		#endregion

		#region INotifyPropertyChanged implementation

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(string propertyName = null)
		{
			if (PropertyChanged != null)
				PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion
	}
}
