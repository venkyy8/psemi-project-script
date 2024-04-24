using PE24103Control.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PE24103Control.UIControls
{
    public interface IMappedControl
    {
        MappedRegister Reg { get; set; }
    }
}
