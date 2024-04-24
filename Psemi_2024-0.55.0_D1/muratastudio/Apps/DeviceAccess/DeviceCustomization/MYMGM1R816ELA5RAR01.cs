using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceAccess
{
    internal class MYMGM1R816ELA5RAR01 : CustomizeBase
    {
        private const byte CLEAR_FAULTS = 0x03;

         public MYMGM1R816ELA5RAR01(Device device)
            : base(device)
        {
        }

         public MYMGM1R816ELA5RAR01(Device device, bool isDemo)
            : base(device, isDemo)
        {
        }

        public override void CustomizeDevice(bool isDevMode = false)
        {
            var data = new byte[0];
            this.Device.WriteBlock(CLEAR_FAULTS, 0, ref data);
        }
    }
}
