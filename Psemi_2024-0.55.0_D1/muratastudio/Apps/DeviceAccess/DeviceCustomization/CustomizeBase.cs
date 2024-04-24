using HardwareInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DeviceAccess
{
	public abstract class CustomizeBase
	{
		public enum I2cAccess
		{
			Unlock = 0x6c,
			Lock = 0xff
		}

		public bool IsDemo { get; private set; }

		public Device Device { get; private set; }

		public CustomizeBase(Device device)
		{
			Device = device;
		}

		public CustomizeBase(Device device, bool isDemo = false)
		{
			Device = device;
			IsDemo = isDemo;
		}

		public virtual void CustomizeDevice(bool isDevMode = false)
		{
			// Overriden in derived class to modify the register space information
		}

        public virtual void ModifyDeviceConfig(string controlName, Register reg, List<FrameworkElement> controls)
        {
        }

		public virtual void ModifyDeviceConfig(object obj1, Object obj2)
		{
		}


		public virtual void ModifyPlugin(object plugin, object controls)
		{
		}

		public virtual void ModifyPlugin(List<KeyValuePair<string, object>> values, object controls)
		{
		}

		public virtual void ResetDevice(object criteria)
		{
		}

		public virtual void CheckDevice(object criteria)
		{
		}

		public virtual byte SetAddress(byte address)
		{
			throw new Exception("This device's address is not settable.");
		}

		public virtual void SetI2cAccess(I2cAccess state)
		{
		}

		public virtual void Unlock()
		{
		}

		public virtual void Lock()
		{
		}
	}
}
