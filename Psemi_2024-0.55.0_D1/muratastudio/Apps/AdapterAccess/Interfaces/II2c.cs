using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace AdapterAccess.Interfaces
{
    public interface II2c
    {
        I2cCapabilities I2cGetCapabilities();
        I2cConfiguration I2cGetConfiguration();
        void I2cSetConfiguration(I2cConfiguration config);
        void I2cEnable();
        void I2cDisable();
        void I2cWriteByte(byte targetAddress, byte address, byte data);
        void I2cWriteBlock(byte targetAddress, byte address, int length, byte[] data);
        byte I2cReadByte(byte targetAddress, byte address);
        void I2cReadBlock(byte targetAddress, byte address, int length, ref byte[] data);
        void I2cSendStop();
    }

    public struct I2cCapabilities
    {
        public int[] SupportedBitRates;
        public int[] SupportedBusLockTimeouts;
        public bool SoftwareControlledPullups;
        public bool StopCommand;
    }

    public struct I2cConfiguration
    {
        public int BitRate;
        public bool PullupsEnabled;
        public int BusLockTimeoutMs;
    }
}
