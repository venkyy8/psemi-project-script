using VADERControl.ViewModel;
using VADERControl.UIControls;
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
using Microsoft.Win32;
using System.Collections.ObjectModel;
using DeviceAccess;
using System.ComponentModel;
using System.Collections;
using VADERControl.Helpers;

namespace VADERControl
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class VADERControl : UserControl
    {
        PluginViewModel pvm = null;
        private Device _device;

        private IRegister ireg;

        public VADERControl(object device, bool isInternalMode)
        {
            InitializeComponent();
            pvm = new PluginViewModel(device, isInternalMode);
            _device = device as Device;
            DataContext = pvm;
            if (pvm != null)
            {
                //ONOFFControls();

            }
                this.Loaded += VADERControl_Loaded;
        }

        public IMappedControl LoadControl(FrameworkElement control, string regName)
        {
            string name = control.Name;
            string[] rname = regName.Split('|');

            Register reg = _device.Registers.FirstOrDefault(r => r.DisplayName == rname[0]);
            IRegister ireg = _device as IRegister;

            MappedRegister mr = new MappedRegister
            {
                RegisterSource = reg,
                Units = reg.Unit,
                DeviceSource = _device
            };

            pvm.MappedRegisters.Add(mr);

            //===========================================================================
            // Input Control
            //===========================================================================
            if (control is InputControl)
            {
                var ic = control as InputControl;
                var val = new Binding();
                val.Source = mr;
                val.Path = new PropertyPath("Value");
                ic.SetBinding(InputControl.DisplayProperty, val);
                if (mr.RegisterSource.ID.ToString().Contains("CFGDEGLITCH")) 
                {                   
                    ic.txtUnits.Visibility = Visibility.Visible;
                    var unit = new Binding();
                    unit.Source = mr;
                    unit.Path = new PropertyPath("Units");                   
                    ic.SetBinding(InputControl.UnitsProperty, unit);
                    if(regName.Split('|').Count() > 0 && regName.Split('|')[1].Contains("VOUT_OK"))
                    {
                        ic.txtUnits.Text = "ms";
                    }
                    else ic.txtUnits.Text = "us";
                }
                else
                {

                    ic.txtUnits.Visibility = Visibility.Collapsed;
                }
                ic.Reg = mr;
                ic.ValidCharacters = GetValidCharacters(mr);
                ic.Write += control_Write;
                var isReadOnly = new Binding();
                isReadOnly.Source = mr;
                isReadOnly.Path = new PropertyPath("IsReadOnly");
                ic.SetBinding(InputControl.IsReadOnlyProperty, isReadOnly);

                return ic;
            }

            return null;
        }

        private string GetValidCharacters(MappedRegister mr)
        {
            string match = "0123456789";
            switch (mr.RegisterSource.DataType)
            {
                case "L11":
                case "S":
                    match = "-.0123456789";
                    break;
                case "F":
                    match = ".0123456789";
                    break;
                case "H":
                    match = "0123456789ABCDEFabcdef";
                    break;
                case "U":
                default:
                    break;
            }
            return match;
        }

        private void control_Write(MappedRegister reg, string value)
        {

            if (pvm == null)
            {
                return;
            }

            pvm.Write(reg, value, pvm, ireg);
           

        }

        void VADERControl_Loaded(object sender, RoutedEventArgs e)
        {
            pvm = this.DataContext as PluginViewModel;
            pvm._loaded = true;
        }
       
        private void LoadAllRegisters_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ClearRegisters_Click(object sender, RoutedEventArgs e)
        {
            pvm.ClearRegistersEvent(sender, e);
        }

        #region PropertyChangedEventHandler
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion
        #region " RaisePropertyChanged Function "
        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyName"></param>
        private void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));

        }
        #endregion

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
    public interface IUIObjects
    {
        string Label { get; set; }
        string Map { get; set; }
        string Description { get; set; }
    }

    public class SendByteObject : IUIObjects
    {
        public string Label { get; set; }
        public string Map { get; set; }
        public string Description { get; set; }
    }

    public class ToggleObject : IUIObjects
    {
        public string Label { get; set; }
        public string Map { get; set; }
        public string Description { get; set; }
    }

    public class BitStatusObject : IUIObjects
    {
        public string Label { get; set; }
        public string Map { get; set; }
        public string Description { get; set; }
    }

    public class Option
    {
        public string Label { get; set; }
        public string Value { get; set; }
    }

}
