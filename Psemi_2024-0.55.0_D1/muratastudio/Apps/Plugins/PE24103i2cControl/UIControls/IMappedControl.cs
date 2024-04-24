using PE24103i2cControl.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PE24103i2cControl.UIControls
{
    public interface IMappedControl
    {
        MappedRegister Reg { get; set; }
    }
}
