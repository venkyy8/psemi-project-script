using PE24103i2cControl.ViewModel;
using PE24103i2cControl.UIControls;
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
using PE24103i2cControl.Helpers;
using DeviceAccess;

namespace PE24103i2cControl
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class PE24103i2cControl : UserControl
	{
        private PluginViewModel _pvm;
        private Device _device;
        private bool _loading = true;
        private int _selectedConfig = 0;
        private bool _isPE24103;
        private List<string> _telemetryRegisters;

        public PE24103i2cControl(object device, bool isInternalMode, bool isAllRegRead = false, bool isPE24103= true)
        {
            InitializeComponent();
            int selectedConfig = 0;

            if(isPE24103)
            {
                PE24103TabControl.Visibility = Visibility.Visible;
                PE24104TabControl.Visibility = Visibility.Collapsed;
            }
            else
            {
                PE24103TabControl.Visibility = Visibility.Collapsed;
                PE24104TabControl.Visibility = Visibility.Visible;
            }

            DataContext = new PluginViewModel(device, isInternalMode, out selectedConfig, isPE24103);

            _pvm = DataContext as PluginViewModel;

            _device = device as Device;

            _selectedConfig = selectedConfig;
            _isPE24103 = isPE24103;
            _telemetryRegisters = new List<string>();

            _pvm.IsAllRegistersRead = isAllRegRead;
            _pvm?.TurnOnMasterTestRegister();

            if (_pvm != null)
            {
                _pvm.RegRefresh += RegRefresh;
            }

            if (_pvm.IsAllRegistersRead)
            {
                GridLengthConverter gridLengthConverter = new GridLengthConverter();
                ProgressBarGrid.Height = (GridLength)gridLengthConverter.ConvertFrom("40");
            }

            UpdateTelemetryBasedConfig(selectedConfig, isPE24103);
        }

        private void RegRefresh(object sender, EventArgs e)
        {
            UpdateTelemetryReadingsINGUI();
        }

        private void UpdateTelemetryReadingsINGUI()
        {
            foreach (string telRegName in _telemetryRegisters)
            {
                MappedRegister mappedRegister = _pvm.MappedRegisters.FirstOrDefault(telRegister => 
                                                    telRegister.RegisterSource.DisplayName == telRegName);
                
                if (mappedRegister == null)
                {
                    continue;
                }
                else
                {
                    if (_device != null && _device.Registers != null)
                    {
                        Register reg = _device.Registers.FirstOrDefault(r => r.DisplayName == telRegName);

                        if (reg != null)
                        {
                            _pvm.ExecuteReadCommand(reg);

                            double value = int.Parse(reg.LastReadString, System.Globalization.NumberStyles.HexNumber);

                            value = ApplyFormulaToTelemetryReadings(reg, value);

                            mappedRegister.Value = value;

                            _pvm.MappedRegisters.FirstOrDefault(telRegister =>
                                    telRegister.RegisterSource.DisplayName == telRegName).Value = value;
                        }
                    }
                }
            }
        }

        private void UpdateTelemetryBasedConfig(int selectedConfig, bool isPE24103)
        {
            if (isPE24103)
            {
                _telemetryRegisters.Add("READ_VIN");
                _telemetryRegisters.Add("READ_IIN");
                _telemetryRegisters.Add("ADC_VX_RSLT");
                _telemetryRegisters.Add("READ1_VOUT");
                _telemetryRegisters.Add("READ1_IOUT");
                _telemetryRegisters.Add("READ2_VOUT");
                _telemetryRegisters.Add("READ2_IOUT");
                _telemetryRegisters.Add("READ3_VOUT");
                _telemetryRegisters.Add("READ3_IOUT");
                _telemetryRegisters.Add("READ4_VOUT");
                _telemetryRegisters.Add("READ4_IOUT");
                _telemetryRegisters.Add("READ_TEMP_1");

                if (selectedConfig == 1)
                {
                    this.C1VinPresenter.Content = LoadControl(new InputControl { Name = "C0P1Vin" }, "READ_VIN");
                    this.C1IINPresenter.Content = LoadControl(new InputControl { Name = "C0P1Vin" }, "READ_IIN");
                    this.C1VXPresenter.Content = LoadControl(new InputControl { Name = "C0P1Vin" }, "ADC_VX_RSLT");
                    this.C1VOUT1Presenter.Content = LoadControl(new InputControl { Name = "C0P1Vin" }, "READ1_VOUT");
                    this.C1IOUT1Presenter.Content = LoadControl(new InputControl { Name = "C0P1Vin" }, "READ1_IOUT");
                    this.C1VOUT2Presenter.Content = LoadControl(new InputControl { Name = "C0P1Vin" }, "READ2_VOUT");
                    this.C1IOUT2Presenter.Content = LoadControl(new InputControl { Name = "C0P1Vin" }, "READ2_IOUT");
                    this.C1VOUT3Presenter.Content = LoadControl(new InputControl { Name = "C0P1Vin" }, "READ3_VOUT");
                    this.C1IOUT3Presenter.Content = LoadControl(new InputControl { Name = "C0P1Vin" }, "READ3_IOUT");
                    this.C1VOUT4Presenter.Content = LoadControl(new InputControl { Name = "C0P1Vin" }, "READ4_VOUT");
                    this.C11IOUT4Presenter.Content = LoadControl(new InputControl { Name = "C0P1Vin" }, "READ4_IOUT");
                    this.C1TempPresenter.Content = LoadControl(new InputControl { Name = "C0P1Vin" }, "READ_TEMP_1");
                }
                else if (selectedConfig == 2)
                {
                    this.C2VinPresenter.Content = LoadControl(new InputControl { Name = "C0P1Vin" }, "READ_VIN");
                    this.C2IINPresenter.Content = LoadControl(new InputControl { Name = "C0P1Vin" }, "READ_IIN");
                    this.C2VXPresenter.Content = LoadControl(new InputControl { Name = "C0P1Vin" }, "ADC_VX_RSLT");
                    this.C2VOUT1Presenter.Content = LoadControl(new InputControl { Name = "C0P1Vin" }, "READ1_VOUT");
                    this.C2IOUT1Presenter.Content = LoadControl(new InputControl { Name = "C0P1Vin" }, "READ1_IOUT");
                    this.C2VOUT23Presenter.Content = LoadControl(new InputControl { Name = "C0P1Vin" }, "READ2_VOUT");
                    this.C2IOUT23Presenter.Content = LoadControl(new InputControl { Name = "C0P1Vin" }, "READ2_IOUT");
                    this.C2VOUT4Presenter.Content = LoadControl(new InputControl { Name = "C0P1Vin" }, "READ4_VOUT");
                    this.C21IOUT4Presenter.Content = LoadControl(new InputControl { Name = "C0P1Vin" }, "READ4_IOUT");
                    this.C2TempPresenter.Content = LoadControl(new InputControl { Name = "C0P1Vin" }, "READ_TEMP_1");
                }
                else if (selectedConfig == 3)
                {
                    this.C3VinPresenter.Content = LoadControl(new InputControl { Name = "C0P1Vin" }, "READ_VIN");
                    this.C3IINPresenter.Content = LoadControl(new InputControl { Name = "C0P1Vin" }, "READ_IIN");
                    this.C3VXPresenter.Content = LoadControl(new InputControl { Name = "C0P1Vin" }, "ADC_VX_RSLT");
                    this.C3VOUT12Presenter.Content = LoadControl(new InputControl { Name = "C0P1Vin" }, "READ1_VOUT");
                    this.C3IOUT12Presenter.Content = LoadControl(new InputControl { Name = "C0P1Vin" }, "READ1_IOUT");
                    this.C3VOUT34Presenter.Content = LoadControl(new InputControl { Name = "C0P1Vin" }, "READ3_VOUT");
                    this.C3IOUT34Presenter.Content = LoadControl(new InputControl { Name = "C0P1Vin" }, "READ3_IOUT");
                    this.C3TempPresenter.Content = LoadControl(new InputControl { Name = "C0P1Vin" }, "READ_TEMP_1");
                }
                else if (selectedConfig == 4)
                {
                    this.C4VinPresenter.Content = LoadControl(new InputControl { Name = "C0P1Vin" }, "READ_VIN");
                    this.C4IINPresenter.Content = LoadControl(new InputControl { Name = "C0P1Vin" }, "READ_IIN");
                    this.C4VXPresenter.Content = LoadControl(new InputControl { Name = "C0P1Vin" }, "ADC_VX_RSLT");
                    this.C4VOUT123Presenter.Content = LoadControl(new InputControl { Name = "C0P1Vin" }, "READ1_VOUT");
                    this.C4IOUT123Presenter.Content = LoadControl(new InputControl { Name = "C0P1Vin" }, "READ1_IOUT");
                    this.C4VOUT4Presenter.Content = LoadControl(new InputControl { Name = "C0P1Vin" }, "READ4_VOUT");
                    this.C4IOUT4Presenter.Content = LoadControl(new InputControl { Name = "C0P1Vin" }, "READ4_IOUT");
                    this.C4TempPresenter.Content = LoadControl(new InputControl { Name = "C0P1Vin" }, "READ_TEMP_1");
                }
                else if (selectedConfig == 5)
                {
                    this.C5VinPresenter.Content = LoadControl(new InputControl { Name = "C0P1Vin" }, "READ_VIN");
                    this.C5IINPresenter.Content = LoadControl(new InputControl { Name = "C0P1Vin" }, "READ_IIN");
                    this.C5VXPresenter.Content = LoadControl(new InputControl { Name = "C0P1Vin" }, "ADC_VX_RSLT");
                    this.C5VOUT1234Presenter.Content = LoadControl(new InputControl { Name = "C0P1Vin" }, "READ1_VOUT");
                    this.C5IOUT1234Presenter.Content = LoadControl(new InputControl { Name = "C0P1Vin" }, "READ1_IOUT");
                    this.C5TempPresenter.Content = LoadControl(new InputControl { Name = "C0P1Vin" }, "READ_TEMP_1");
                }
            }
            else
            {
                _telemetryRegisters.Add("READ_VIN");
                _telemetryRegisters.Add("READ_IIN");
                _telemetryRegisters.Add("ADC_VX_RSLT");
                _telemetryRegisters.Add("READ2_VOUT");
                _telemetryRegisters.Add("READ2_IOUT");
                _telemetryRegisters.Add("READ3_VOUT");
                _telemetryRegisters.Add("READ3_IOUT");
                _telemetryRegisters.Add("READ_TEMP_1");

                if (selectedConfig == 1)
                {
                    this.PE24104C1VinPresenter.Content = LoadControl(new InputControl { Name = "C0P1Vin" }, "READ_VIN");
                    this.PE24104C1IINPresenter.Content = LoadControl(new InputControl { Name = "C0P1Vin" }, "READ_IIN");
                    this.PE24104C1VXPresenter.Content = LoadControl(new InputControl { Name = "C0P1Vin" }, "ADC_VX_RSLT");
                    this.PE24104C1VOUT1Presenter.Content = LoadControl(new InputControl { Name = "C0P1Vin" }, "READ2_VOUT");
                    this.PE24104C1IOUT1Presenter.Content = LoadControl(new InputControl { Name = "C0P1Vin" }, "READ2_IOUT");
                    this.PE24104C1VOUT2Presenter.Content = LoadControl(new InputControl { Name = "C0P1Vin" }, "READ3_VOUT");
                    this.PE24104C1IOUT2Presenter.Content = LoadControl(new InputControl { Name = "C0P1Vin" }, "READ3_IOUT");
                    this.PE24104C1TempPresenter.Content = LoadControl(new InputControl { Name = "C0P1Vin" }, "READ_TEMP_1");
                }
                else if (selectedConfig == 2)
                {
                    this.PE24104C2VinPresenter.Content = LoadControl(new InputControl { Name = "C0P1Vin" }, "READ_VIN");
                    this.PE24104C2IINPresenter.Content = LoadControl(new InputControl { Name = "C0P1Vin" }, "READ_IIN");
                    this.PE24104C2VXPresenter.Content = LoadControl(new InputControl { Name = "C0P1Vin" }, "ADC_VX_RSLT");
                    this.PE24104C2VOUT12Presenter.Content = LoadControl(new InputControl { Name = "C0P1Vin" }, "READ2_VOUT");
                    this.PE24104C2IOUT12Presenter.Content = LoadControl(new InputControl { Name = "C0P1Vin" }, "READ2_IOUT");
                    this.PE24104C2TempPresenter.Content = LoadControl(new InputControl { Name = "C0P1Vin" }, "READ_TEMP_1");
                }
            }
        }

        private IMappedControl LoadControl(FrameworkElement control, string regName)
        {
            string name = control.Name;
            control.ToolTip = regName;
            string[] rname = regName.Split('|');

            Register reg = _device.Registers.FirstOrDefault(r => r.DisplayName == rname[0]);
            IRegister ireg = _device as IRegister;

            if (reg == null)
            {
                var ic = control as InputControl;
                return ic;
            }
            ireg.ReadRegisterValue(reg);
            _pvm.ExecuteReadCommand(reg);

            double value = int.Parse(reg.LastReadString, System.Globalization.NumberStyles.HexNumber);

            value = ApplyFormulaToTelemetryReadings(reg, value);

            MappedRegister mr = new MappedRegister
            {
                Config = int.Parse(name.Substring(1, 1)),
                Page = int.Parse(name.Substring(3, 1)),
                RegisterSource = reg,
                Units = reg.Unit,
                DeviceSource = _device,
                IsReadOnly = false,
                Value = value
            };

            _pvm.MappedRegisters.Add(mr);

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
                
                 //var unit = new Binding();
                 //unit.Source = mr;
                 //unit.Path = new PropertyPath("Units");
                 //ic.SetBinding(InputControl.UnitsProperty, unit);
                 //ic.Reg = mr;
                 //ic.ValidCharacters = GetValidCharacters(mr);
                 //ic.Write += control_Write;

                var isReadOnly = new Binding();
                isReadOnly.Source = mr;
                isReadOnly.Path = new PropertyPath("IsReadOnly");
                ic.SetBinding(InputControl.IsReadOnlyProperty, isReadOnly);

                return ic;
            }
            //===========================================================================
            // List Control
            //===========================================================================
            if (control is ListControl)
            {
                var lc = control as ListControl;
                var val = new Binding();
                val.Source = mr;
                val.Path = new PropertyPath("RawValue");
                lc.SetBinding(ListControl.ValueProperty, val);

                ushort mask;
                lc.ItemsSource = LoadOptions(rname[0], out mask);
                lc.DisplayMemberPath = "Label";
                lc.SelectedValuePath = "Value";
                mr.Mask = mask;
                lc.Reg = mr;
                lc.Write += control_Write;
                return lc;
            }
            //===========================================================================
            // BitStatus Control
            //===========================================================================
            if (control is BitStatusLabel)
            {
                var bitStatusObject = new BitStatusObject { Label = rname[1], Map = regName, Description = "" };
                var bitStatus = control as BitStatusLabel;
                bitStatus.RegisterSource = ireg.GetRegister(regName.Split('|')[0].Replace("_", ""));
                mr.Bit = ireg.GetRegisterBit(regName.Split('|')[0].Replace("_", "") + "_" + regName.Split('|')[1].Replace("_", ""));
                bitStatus.Tag = bitStatusObject;
                bitStatus.Content = regName.Split('|')[1];
                //bitStatus.DataContext = this;

                var set = new Binding();
                set.Source = mr;
                set.Path = new PropertyPath("BitValue");
                bitStatus.SetBinding(BitStatusLabel.IsSetProperty, set);

                //set = new Binding();
                //set.Source = bitStatus.RegisterSource;
                //set.Path = new PropertyPath("LastReadValueError");
                //bitStatus.SetBinding(BitStatusLabel.IsErrorProperty, set);
                bitStatus.HorizontalAlignment = HorizontalAlignment.Stretch;
                bitStatus.Reg = mr;
                return bitStatus;
            }
            //===========================================================================
            // SendByteButton Control
            //===========================================================================
            if (control is SendByteButton)
            {
                var sendObject = new SendByteObject { Label = regName, Map = regName, Description = "" };
                var sendButton = new SendByteButton();
                sendButton.Content = "Send Command";
                sendButton.Style = Application.Current.FindResource("ArcticSandButton") as Style;
                sendButton.RegisterSource = ireg.GetRegister(regName.Split('|')[0].Replace("_", ""));
                sendButton.Tag = sendObject;
                sendButton.Command = _pvm.SendByteCommand;
                sendButton.CommandParameter = ireg.GetRegister(regName.Split('|')[0].Replace("_", ""));
                sendButton.DataContext = this;
                sendButton.HorizontalAlignment = HorizontalAlignment.Stretch;
                sendButton.MaxWidth = 300;
                sendButton.Reg = mr;
                return sendButton;
            }

            return null;
        }

        private static double ApplyFormulaToTelemetryReadings(Register reg, double value)
        {
            if (reg.Name == "READ_VIN" || reg.Name == "ADC_VX_RSLT" || reg.Name == "READ1_VOUT" ||
                reg.Name == "READ2_VOUT" || reg.Name == "READ3_VOUT" || reg.Name == "READ4_VOUT")
            {
                value = Math.Round((value / 2048), 2);
            }
            else if (reg.Name == "READ_IIN" || reg.Name == "READ1_IOUT" || reg.Name == "READ2_IOUT" ||
                reg.Name == "READ3_IOUT" || reg.Name == "READ4_IOUT")
            {
                var x = (value - Math.Pow(2, 15) - Math.Pow(2, 14) - Math.Pow(2, 12) - Math.Pow(2, 11));
                if (x > 1023)
                {
                    value = Math.Round((((x - 1024) / 32) - 32), 2);
                }
                else
                {
                    value = Math.Round((x / 32), 2);
                }
            }
            else if (reg.Name == "READ_TEMP_1")
            {
                var x = (value - Math.Pow(2, 15) - Math.Pow(2, 14) - Math.Pow(2, 13) - Math.Pow(2, 12));
                if (x > 1023)
                {
                    value = Math.Round((((x - 1024) / 4) - 256), 2);
                }
                else
                {
                    value = Math.Round((x / 4), 2);
                }
            }

            return value;
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
            if (_loading)
            {
                return;
            }
            _pvm.Write(reg, value);
        }

        private List<Option> LoadOptions(string registerName, out ushort mask)
        {
            List<Option> options = new List<Option>();

            switch (registerName)
            {
                case "OPERATION":
                    options.Add(new Option { Label = "Buck Off, Fast Shutdown", Value = "4" });
                    options.Add(new Option { Label = "Buck Off, Mrgn Low, Fast Shutdown, No Mrgn fault", Value = "20" });
                    options.Add(new Option { Label = "Buck Off, Mrgn Low, Fast Shutdown", Value = "24" });
                    options.Add(new Option { Label = "Buck Off, Mrgn High, Fast Shutdown, No Mrgn fault", Value = "36" });
                    options.Add(new Option { Label = "Buck Off, Mrgn High, Fast Shutdown", Value = "40" });
                    options.Add(new Option { Label = "Buck Off, Ramp Shutdown", Value = "68" });
                    options.Add(new Option { Label = "Buck Off, Mrgn Low, Ramp Shutdown, No Mrgn fault", Value = "84" });
                    options.Add(new Option { Label = "Buck Off, Mrgn Low, Ramp Shutdown", Value = "88" });
                    options.Add(new Option { Label = "Buck Off, Mrgn High, Ramp Shutdown, No Mrgn fault", Value = "100" });
                    options.Add(new Option { Label = "Buck Off, Mrgn High, Ramp Shutdown", Value = "104" });

                    options.Add(new Option { Label = "Buck On, Fast Shutdown", Value = "132" });
                    options.Add(new Option { Label = "Buck On, Mrgn Low, Fast Shutdown, No Mrgn fault", Value = "148" });
                    options.Add(new Option { Label = "Buck On, Mrgn Low, Fast Shutdown", Value = "152" });
                    options.Add(new Option { Label = "Buck On, Mrgn High, Fast Shutdown, No Mrgn fault", Value = "164" });
                    options.Add(new Option { Label = "Buck On, Mrgn High, Fast Shutdown", Value = "168" });
                    options.Add(new Option { Label = "Buck On, Ramp Shutdown", Value = "196" });
                    options.Add(new Option { Label = "Buck On, Mrgn Low, Ramp Shutdown, No Mrgn fault", Value = "212" });
                    options.Add(new Option { Label = "Buck On, Mrgn Low, Ramp Shutdown", Value = "216" });
                    options.Add(new Option { Label = "Buck On, Mrgn High, Ramp Shutdown, No Mrgn fault", Value = "228" });
                    options.Add(new Option { Label = "Buck On, Mrgn High, Ramp Shutdown", Value = "232" });
                    mask = 0xFF;
                    break;
                case "ON_OFF_CONFIG":
                    options.Add(new Option { Label = "Converter Powers Up", Value = "0" });
                    options.Add(new Option { Label = "Converter Does Not", Value = "24" });
                    mask = 0xFF;
                    break;
                case "FREQUENCY_SWITCH":
                    options.Add(new Option { Label = "600 kHz", Value = "2348" });
                    options.Add(new Option { Label = "1.0 MHz", Value = "2548" });
                    options.Add(new Option { Label = "1.2 MHz", Value = "2648" });
                    options.Add(new Option { Label = "1.33 MHz", Value = "2715" });
                    options.Add(new Option { Label = "1.5 MHz", Value = "2798" });
                    options.Add(new Option { Label = "1.71 MHz", Value = "2905" });
                    options.Add(new Option { Label = "2.0 MHz", Value = "3048" });
                    mask = 0xFFF;
                    break;
                default:
                    mask = 0xFF;
                    break;
            }

            return options;
        }

        private void ImageAwesome_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_pvm.IsAllRegistersRead)
            {
                GridLengthConverter gridLengthConverter = new GridLengthConverter();
                ProgressBarGrid.Height = (GridLength)gridLengthConverter.ConvertFrom("40");
            }
        }

        //private void btnRefresh_Click(object sender, RoutedEventArgs e)
        //{

        //}

        //private void btnPoll_Click(object sender, RoutedEventArgs e)
        //{

        //}

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateTelemetryReadingsINGUI();
        }
    }

    //public interface IUIObjects
    //{
    //    string Label { get; set; }
    //    string Map { get; set; }
    //    string Description { get; set; }
    //}

    //public class ListObject : IUIObjects
    //{
    //    public string Mask { get; set; }

    //    public string Description { get; set; }

    //    public string Label { get; set; }

    //    public string Map { get; set; }
    //}

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
