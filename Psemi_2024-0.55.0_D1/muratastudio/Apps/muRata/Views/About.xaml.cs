using muRata.ViewModel;
using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using muRata.Properties;
using HardwareInterfaces;
using System.ComponentModel;
using System.Threading;
using System.IO;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Runtime.CompilerServices;

namespace muRata.Views
{
    /// <summary>
    /// Interaction logic for About.xaml
    /// </summary>
    public partial class About : Window
    {

        #region Constructor
        public About()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }

        public About(Window parent)
            : this()
        {
            this.Owner = parent;
            this.Product.Content = AssemblyProduct;
            this.Version.Content = AssemblyVersion;
            this.Details.Text = AssemblyDescription;
            this.Copyright.Content = AssemblyCopyright;

            var LogInfo = new Dictionary<string, string>()
            {
                {"0.36.0.0", "Support for Read/Write operation of registers in Demo mode"},
                {"0.37.0.0","PE24103 I2C Mode - Setting Readcount as 2 & Locking MTP registers"},
                {"0.38.0.0","PE24103 I2C Mode - Locking MTP registers in 16 bit addressing based on excel input"},
                {"0.39.0.0","PE24103 Power Good status indicator changes in PMBus, I2C Mode - .csv file upload in MTP GUI."},
                {"0.40.0.0","PE24104 PMBus - Autodetection fix"},
                {"0.41.0.0","Logging feature in Murata Application"},
                {"0.42.0.0","Log Enabling based on command line arguments, adjust readcount based on register size"},
                {"0.43.0.0","Fault indicator mapping - PE24104 i2c device"},
                {"0.44.0.0","Buck status fix for PE24104 device"},
                {"0.45.0.0","Fix for fault indicatos - PE24104 and PE24103(OT_FLT)"},
                {"0.46.0.0","Splash screen for i2c GUI"},
                {"0.46.1.0","Fix for PE24103 Fault label updation and stopping background process after exit"},
                {"0.46.2.0","Fix for telemetry updation in i2c GUI . Search implementation in Registers tab"},
                {"0.47.0.0","Instant protocol data updation to Device and Registers tab in i2c GUI"},
                {"0.47.1.0","Fix for PE23108 and ARC3C0845 register reset of Led status"},
                {"0.47.2.0","Code modifications for MTP Programming in demo mode"},
                {"0.47.3.0","Fix for scaling factor issue in PE23108 and ARC3C0845 and Readcount value response"},
                {"0.48.0.0","Multibyte support in Protocol tab"},
                {"0.49.0.0","Fix for LED intenstity textbox/scrollbar value updations in PE23108R01 & ARC3C084"},
                {"0.50.0.0","IOUT Readings combined in Configurations of PE24103/PE24104 PMBus GUI" },
                {"0.50.1.0","Automatically setting values for MFR_SPECIFIC_MATRIX (0xC5) and MFR_SPECIFIC_PGOOD (0xC4) in PE24103 PMBus device " },
                {"0.52.0.0","PE26100 GIMLI Device " },
                {"0.53.0.0","VOUT Regulation & Current sense labels change and Address {0x30} to {0x32} changes, " +
                "Device enabled and unlocked automatically, when the GUI initiates(Operation on MASTERTESTLB register) changes in PE26100 GIMLI Device" },
                {"0.53.1.0","Disabling I2CTimeOut for an unprogrammed device in PE26100"},
                {"0.53.2.0","Enabling and disabling Input current sense and Input sense resistor based on Vinres Bit, " +
                "IIN_RAMP_RATE pull down selection, PE26100 target address to 0x32" },
                {"0.53.3.0","Adding Telemetry controls to Device Tab of PE26100 device" },
                {"0.54.0.0","VADER Device" },
                {"0.54.1.0","Controls greyed out based on Step-down CP and Reverse Step-up operations, Registers beyond 0x37 visibility in Internal mode" },
                {"0.54.2.0","Removed greyed out of Step-down regulation mode based on Reverse Step-up operations,Renamed step-down regulation mode to power train configuration" },
                {"0.54.3.0","Fix for Application crash and blank issue caused when IC disabled and refresh, IOUT and IIN Current Limit,Vout regulation value selection issues,Fix for UI width issue, NA entry selection for values out of the range for TEMP_OT and TEMP_OT_WARN" },
                {"0.54.4.0","Decimalvalue entry in sense resitor textboxes, NA selection entry for IOUT and IIN Comboboxes,Fixed for updating Target Address in Protocol Tab when switching Activedevice ,Fix for issue of Registers List got emptied in Register tab grid" },
                {"0.55.0.0","Filter the target address for PE23108, Add all the register value from address 0x00 to 0x0B and display the result on the Check sum control in PE23108 Device tab GUI" }
            };

            MainViewModel mvm = ServiceLocator.Current.GetInstance<MainViewModel>();

            List<PluginInfo> pi = new List<PluginInfo>();
            foreach (var item in mvm.ActivePlugins)
            {
                pi.Add(new PluginInfo
                {
                    Name = item.GetPluginInfo.AssemblyName.Split(',')[0],
                    Version = item.GetPluginInfo.AssemblyVersion
                });

            }
            if (mvm.IsDemoMode && mvm.IsInternalMode)
            {
                for (int i = 0; i < LogInfo.Count; i++)
                {
                    Logs.Text += LogInfo.ElementAt(i).Key + " - " + LogInfo.ElementAt(i).Value + "\n";
                }
            }

            this.Plugins.ItemsSource = pi;
            this.Licenses.Text = Properties.Resources.Licenses;

        }

        #endregion


        #region Assembly Attribute Accessors

        public string AssemblyTitle
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
                if (attributes.Length > 0)
                {
                    AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)attributes[0];
                    if (titleAttribute.Title != "")
                    {
                        return titleAttribute.Title;
                    }
                }
                return System.IO.Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().CodeBase);
            }
        }



        public string AssemblyVersion
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
        }

        public string AssemblyDescription
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyDescriptionAttribute)attributes[0]).Description;
            }
        }

        public string AssemblyProduct
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyProductAttribute)attributes[0]).Product;
            }
        }

        public string AssemblyCopyright
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
            }
        }

        public string AssemblyCompany
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyCompanyAttribute)attributes[0]).Company;
            }
        }
        #endregion

        #region Event Handlers

        private void hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            string uri = e.Uri.AbsoluteUri;
            Process.Start(new ProcessStartInfo(uri));
            e.Handled = true;
        }

        #endregion

        class PluginInfo
        {
            public string Name { get; set; }
            public string Version { get; set; }

        }

    }
}