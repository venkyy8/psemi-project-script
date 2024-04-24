using ARC1C0608Control.ViewModel;
using ARC1C0608Control.UIControls;
using HardwareInterfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;
using DeviceAccess;
using System.Threading;

namespace ARC1C0608Control
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class ARC1C0608Control : UserControl
    {
        PluginViewModel pvm = null;
        private Device _device;

        public ARC1C0608Control(object device, bool isInternalMode)
        {
            InitializeComponent();
            pvm = new PluginViewModel(device, isInternalMode);
            DataContext = pvm;
            _device = device as Device;
        }


    }
}
