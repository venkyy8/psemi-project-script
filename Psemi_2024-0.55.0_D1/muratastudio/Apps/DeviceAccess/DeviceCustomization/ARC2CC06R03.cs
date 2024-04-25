using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Threading;
using System.Collections;
using System.Windows.Data;

namespace DeviceAccess
{
	internal class ARC2CC06R03 : CustomizeBase
	{
		private const byte ACCESS_REG = 0x41;
		private const byte UNLOCK = 0x37;
		private const byte LOCK = 0xFF;
		private const byte CMD_READ = 0x0F;
		private const byte CMD_READ_SUCCESS = 0x07;
		private const byte PSEMI_ID = 0xA5;

		// Olaf 0
		const byte OLAF0CMD = 0x56;
		const byte OLAF0ADDR = 0x57;
		const byte OLAF0WDATA = 0x58;
		const byte OLAF0RDATA = 0x59;
		const byte OLAD0STATUS = 0x5A;

		// Olaf 1
		const byte OLAF1CMD = 0x5B;
		const byte OLAF1ADDR = 0x5C;
		const byte OLAF1WDATA = 0x5D;
		const byte OLAF1RDATA = 0x5E;
		const byte OLAD1STATUS = 0x5F;

		public ARC2CC06R03(Device device)
			: base(device)
		{
		}

		public ARC2CC06R03(Device device, bool isDemo = false)
			: base(device, isDemo)
		{
			if (IsDemo)
			{
				LoadBuck1();
				LoadBuck2();
			}
		}

		public override void CustomizeDevice(bool isDevMode = false)
		{
			var foundSlave = false;

			// Find Slaves
			if (FindSlave(OLAF0ADDR, OLAF0CMD, OLAF0RDATA))
			{
				LoadBuck1();
				foundSlave |= true;
			}

			if (FindSlave(OLAF1ADDR, OLAF1CMD, OLAF1RDATA))
			{
				LoadBuck2();
				foundSlave |= true;
			}

			if(foundSlave == false)
			{
				// TODO Send message: Slaves not found!
			}

			if (isDevMode)
			{
				// Unlock master device
				this.Device.WriteByte(ACCESS_REG, UNLOCK);

				// Unlock each slave device
				foreach (var item in Device.SlaveDevices)
				{
					item.WriteByte(ACCESS_REG, UNLOCK);
				}
			}
			else
			{
				// The following bits are only available in dev mode
				var reg = this.Device.Registers.Find(r => r.Name == "MASTERSTATUS");
				reg.Bits.ElementAt(0).DisplayName = "Reserved";
				reg.Bits.ElementAt(0).Name = "Reserved";
				reg.Bits.ElementAt(0).Description = "Reserved";
				reg.Bits.ElementAt(1).DisplayName = "Reserved";
				reg.Bits.ElementAt(1).Name = "Reserved";
				reg.Bits.ElementAt(1).Description = "Reserved";
				reg.Bits.ElementAt(2).DisplayName = "Reserved";
				reg.Bits.ElementAt(2).Name = "Reserved";
				reg.Bits.ElementAt(2).Description = "Reserved";
				reg.Bits.ElementAt(3).DisplayName = "Reserved";
				reg.Bits.ElementAt(3).Name = "Reserved";
				reg.Bits.ElementAt(3).Description = "Reserved";
			}
		}

		public override void Lock()
		{
			base.Lock();
		}

		public override void Unlock()
		{
			// Unlock master device
			this.Device.WriteByte(ACCESS_REG, UNLOCK);

			// Unlock each slave device
			foreach (var item in Device.SlaveDevices)
			{
				item.WriteByte(ACCESS_REG, UNLOCK);
			}
		}

		private bool FindSlave(byte address, byte command, byte read)
		{
			// This is where we will detect any Olaf slave devices and load them.
			byte registerResult = 0;
			byte status = 0;

			// Write the address
			Device.WriteByte((uint)address, 0xff);

			// Write the command
			Device.WriteByte((uint)command, CMD_READ);

			// Read the PSemi register. Should be 0xA5 however the result is ignored
			registerResult = Device.ReadByte((uint)read);

			// Verify status of the read was successful
			status = Device.ReadByte((uint)command);

			return (status == CMD_READ_SUCCESS && registerResult == PSEMI_ID);
		}

		private void LoadBuck1()
		{
			var slave = new SlaveDevice(Device, new SlaveDevice.SlaveAccess
			{
				Command = OLAF0CMD,
				Address = OLAF0ADDR,
				WriteData = OLAF0WDATA,
				ReadData = OLAF0RDATA,
				Status = OLAD0STATUS
			});

			slave.DeviceName = "Buck1";
			slave.ParseRegisterData(Device.GetSlaveRegisterMap());
			Device.SlaveDevices.Add(slave);
		}

		private void LoadBuck2()
		{
			var slave = new SlaveDevice(Device, new SlaveDevice.SlaveAccess
			{
				Command = OLAF1CMD,
				Address = OLAF1ADDR,
				WriteData = OLAF1WDATA,
				ReadData = OLAF1RDATA,
				Status = OLAD1STATUS
			});

			slave.DeviceName = "Buck2";
			slave.ParseRegisterData(Device.GetSlaveRegisterMap());
			Device.SlaveDevices.Add(slave);
		}
	}
}
