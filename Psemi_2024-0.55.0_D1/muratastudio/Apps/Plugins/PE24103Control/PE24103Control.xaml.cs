using PE24103Control.ViewModel;
using HardwareInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
using System.IO;
using DeviceAccess;
using PE24103Control.UIControls;
using System.ComponentModel;
using PE24103Control.Helpers;
using System.Threading;

namespace PE24103Control
{
	/// <summary>
	/// Interaction logic for DocumentViewerControl.xaml
	/// </summary>
    public partial class PE24103Control : UserControl
	{
        #region Private Members

        PluginViewModel _pvm;
        private Device _device;
        private bool _loading = true;
        private bool _isMYT0424 = false;
        private bool _isPE24103 = false;
        private IRegister ireg;
        internal object bitStatus;

        public IMappedControl Page_IOUT_Readings { get; private set; }

        #endregion

        #region properties

        #endregion

        #region Constructors

        public PE24103Control()
		{
			InitializeComponent();
		}

        public PE24103Control(object device, bool isInternalMode, bool isPE24103 = true, bool isMYT0424 = false, Visibility R2D2PMBusVisibility = Visibility.Visible)
        {
            InitializeComponent();

            _isPE24103 = isPE24103;
            _isMYT0424 = isMYT0424;

           _pvm = new PluginViewModel(device, isInternalMode, isPE24103, isMYT0424);
           
            if (isPE24103)
            {
                if (isMYT0424)
                {
                    TabControl1.Visibility = Visibility.Collapsed;
                    TabControl2.Visibility = Visibility.Collapsed;
                    TabControl3.Visibility = Visibility.Visible;
                }
                else
                {
                    TabControl1.Visibility = Visibility.Visible;
                    TabControl2.Visibility = Visibility.Collapsed;
                    TabControl3.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                TabControl1.Visibility = Visibility.Collapsed;
                TabControl2.Visibility = Visibility.Visible;
                TabControl3.Visibility = Visibility.Collapsed;
            }

            _device = device as Device;
            DataContext = _pvm;

            if (_pvm != null)
            {
                ConfigureControls(_pvm.SelectedConfig, isPE24103);

                _pvm.WriteToRegister(_pvm.SelectedConfig);
                _pvm.RegRefresh += _pvm_RegRefresh;
                _pvm.Poll += _pvm_Poll;
            }
            Thread t = new Thread(() => ReadAll(false));
            t.Start();
        }

        private void _pvm_RegRefresh(object sender, EventArgs e)
        {
            CheckIoutReadings();
            CheckPowerGoodFaultIndicatorStatus();
        }

        private void _pvm_Poll(object sender, EventArgs e)
        {
            CheckIoutReadings();
            CheckPowerGoodFaultIndicatorStatus();
        }

        #endregion


        #region Functions

        private void ReadAll(bool dynamicOnly)
        {
            _pvm.ReadAll(dynamicOnly);
            _loading = false;
            CheckPowerGoodFaultIndicatorStatus();
            CheckIoutReadings();
        }

        private void ConfigureControls(int selectedConfig, bool isR2D2)
        {
            // Global Settings
            this.C0P1OnOffConfigPresenter.Content = LoadControl(new ListControl { Name = "C0P1OnOffConfig" }, "ON_OFF_CONFIG");
            this.C0P1FreqSwitchPresenter.Content = LoadControl(new ListControl { Name = "C0P1FreqSwitch" }, "FREQUENCY_SWITCH");
            this.C0P1StorePresenter.Content = LoadControl(new SendByteButton { Name = "C0P1Store" }, "STORE_USER_ALL");
            this.C0P1RestorePresenter.Content = LoadControl(new SendByteButton { Name = "C0P1Restore" }, "RESTORE_USER_ALL");
            this.C0P1ClearFaultsPresenter.Content = LoadControl(new SendByteButton { Name = "C0P1ClearFaults" }, "CLEAR_FAULTS");

            // Input Voltage
            this.C0P1VinPresenter.Content = LoadControl(new InputControl { Name = "C0P1Vin" }, "READ_VIN");
            this.C0P1VinOnPresenter.Content = LoadControl(new InputControl { Name = "C0P1VinOn" }, "VIN_ON");
            this.C0P1VinOffPresenter.Content = LoadControl(new InputControl { Name = "C0P1VinOff" }, "VIN_OFF");
            this.C0P1VinOvPresenter.Content = LoadControl(new InputControl { Name = "C0P1VinOv" }, "VIN_OV_FAULT_LIMIT");
            this.C0P1VinUvPresenter.Content = LoadControl(new InputControl { Name = "C0P1VinUv" }, "VIN_UV_FAULT_LIMIT");

            this.C0P1OvStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C0P1VinOv" }, "STATUS_INPUT|VIN_OV_FAULT");
            this.C0P1UvStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C0P1VinUv" }, "STATUS_INPUT|VIN_UV_FAULT");


            // Input Current
            this.C0P1InnPresenter.Content = LoadControl(new InputControl { Name = "C0P1Inn" }, "READ_IIN");
            this.C0P1InnOcFaultPresenter.Content = LoadControl(new InputControl { Name = "C0P1InnOcFault" }, "IIN_OC_FAULT_LIMIT");
            this.C0P1OcStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C0P1OcStatus" }, "STATUS_INPUT|IIN_OC_FAULT");

            // Temperature
            this.C0P1TemperaturePresenter.Content = LoadControl(new InputControl { Name = "C0P1Temperature" }, "READ_TEMPERATURE_1");
            this.C0P1OtFaultPresenter.Content = LoadControl(new InputControl { Name = "C0P1OtFault" }, "OT_FAULT_LIMIT");
            this.C0P1OtStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C0P1OtStatus" }, "STATUS_TEMPERATURE|OT_FAULT");

            if (isR2D2)
            {
                if (_isPE24103 && _isMYT0424)
                {
                    switch (selectedConfig)
                    {
                        case 1:
                            #region Configuration 1 (1/2/3/4)
                            // BUCK 1
                            this.T3C1P1B1VoutPresenter.Content = LoadControl(new InputControl { Name = "C1P1B1Vout" }, "READ_VOUT");
                            this.T3C1P1B1IoutPresenter.Content = LoadControl(new InputControl { Name = "C1P1B1Iout" }, "READ_IOUT");

                            this.T3C1P1B1OperationPresenter.Content = LoadControl(new ListControl { Name = "C1P1B1Operation" }, "OPERATION");
                            this.T3C1P1B1VoutCommandPresenter.Content = LoadControl(new InputControl { Name = "C1P1B1VoutCommand" }, "VOUT_COMMAND");
                            this.T3C1P1B1VoutMarginHighPresenter.Content = LoadControl(new InputControl { Name = "C1P1B1VoutMarginHigh" }, "VOUT_MARGIN_HIGH");
                            this.T3C1P1B1VoutMarginLowPresenter.Content = LoadControl(new InputControl { Name = "C1P1B1VoutMarginLow" }, "VOUT_MARGIN_LOW");
                            this.T3C1P1B1VoutTransRatePresenter.Content = LoadControl(new InputControl { Name = "C1P1B1VoutTransRate" }, "VOUT_TRANSITION_RATE");
                            this.T3C1P1B1VoutOVFLimitPresenter.Content = LoadControl(new InputControl { Name = "C1P1B1VoutOVFLimit" }, "VOUT_OV_FAULT_LIMIT");
                            this.T3C1P1B1VoutUVFLimitPresenter.Content = LoadControl(new InputControl { Name = "C1P1B1VoutUVFLimit" }, "VOUT_UV_FAULT_LIMIT");
                            this.T3C1P1B1IoutOCFLimitPresenter.Content = LoadControl(new InputControl { Name = "C1P1B1IoutOCFLimit" }, "IOUT_OC_FAULT_LIMIT");
                            this.T3C1P1B1TonDelayPresenter.Content = LoadControl(new InputControl { Name = "C1P1B1TonDelay" }, "TON_DELAY");
                            this.T3C1P1B1TonRisePresenter.Content = LoadControl(new InputControl { Name = "C1P1B1TonRise" }, "TON_RISE");
                            this.T3C1P1B1ToffDelayPresenter.Content = LoadControl(new InputControl { Name = "C1P1B1ToffDelay" }, "TOFF_DELAY");
                            this.T3C1P1B1ToffFallPresenter.Content = LoadControl(new InputControl { Name = "C1P1B1ToffFall" }, "TOFF_FALL");
                            // Status
                            this.T3C1P1B1PowerStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P1B1PowerStatus", OnSetBackgroundColor = new SolidColorBrush(Color.FromRgb(0x75, 0xdf, 0x4a)), OnSetForegroundColor = new SolidColorBrush(Colors.Black) }, "STATUS_WORD|POWER_GOOD");
                            this.T3C1P1B1VoutStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P1B1VoutStatus" }, "STATUS_WORD|VOUT_STATUS");
                            this.T3C1P1B1VoutOvStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P1B1VoutOvStatus" }, "STATUS_VOUT|VOUT_OV_FAULT");
                            this.T3C1P1B1VoutUvStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P1B1VoutUvStatus" }, "STATUS_VOUT|VOUT_UV_FAULT");
                            this.T3C1P1B1IoutStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P1B1IoutStatus" }, "STATUS_WORD|IOUT_STATUS");
                            this.T3C1P1B1IoutOcStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P1B1IoutOcStatus" }, "STATUS_IOUT|IOUT_OC_FAULT");
                            this.T3C1P1B1IoutUcStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P1B1IoutUcStatus" }, "STATUS_IOUT|IOUT_UC_FAULT");

                            // BUCK 2
                            this.T3C1P2B2VoutPresenter.Content = LoadControl(new InputControl { Name = "C1P2B2Vout" }, "READ_VOUT");
                            this.T3C1P2B2IoutPresenter.Content = LoadControl(new InputControl { Name = "C1P2B2Iout" }, "READ_IOUT");

                            this.T3C1P2B2OperationPresenter.Content = LoadControl(new ListControl { Name = "C1P2B2Operation" }, "OPERATION");
                            this.T3C1P2B2VoutCommandPresenter.Content = LoadControl(new InputControl { Name = "C1P2B2VoutCommand" }, "VOUT_COMMAND");
                            this.T3C1P2B2VoutMarginHighPresenter.Content = LoadControl(new InputControl { Name = "C1P2B2VoutMarginHigh" }, "VOUT_MARGIN_HIGH");
                            this.T3C1P2B2VoutMarginLowPresenter.Content = LoadControl(new InputControl { Name = "C1P2B2VoutMarginLow" }, "VOUT_MARGIN_LOW");
                            this.T3C1P2B2VoutTransRatePresenter.Content = LoadControl(new InputControl { Name = "C1P2B2VoutTransRate" }, "VOUT_TRANSITION_RATE");
                            this.T3C1P2B2VoutOVFLimitPresenter.Content = LoadControl(new InputControl { Name = "C1P2B2VoutOVFLimit" }, "VOUT_OV_FAULT_LIMIT");
                            this.T3C1P2B2VoutUVFLimitPresenter.Content = LoadControl(new InputControl { Name = "C1P2B2VoutUVFLimit" }, "VOUT_UV_FAULT_LIMIT");
                            this.T3C1P2B2IoutOCFLimitPresenter.Content = LoadControl(new InputControl { Name = "C1P2B2IoutOCFLimit" }, "IOUT_OC_FAULT_LIMIT");
                            this.T3C1P2B2TonDelayPresenter.Content = LoadControl(new InputControl { Name = "C1P2B2TonDelay" }, "TON_DELAY");
                            this.T3C1P2B2TonRisePresenter.Content = LoadControl(new InputControl { Name = "C1P2B2TonRise" }, "TON_RISE");
                            this.T3C1P2B2ToffDelayPresenter.Content = LoadControl(new InputControl { Name = "C1P2B2ToffDelay" }, "TOFF_DELAY");
                            this.T3C1P2B2ToffFallPresenter.Content = LoadControl(new InputControl { Name = "C1P2B2ToffFall" }, "TOFF_FALL");
                            // Status
                            this.T3C1P2B2PowerStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P2B2PowerStatus", OnSetBackgroundColor = new SolidColorBrush(Color.FromRgb(0x75, 0xdf, 0x4a)), OnSetForegroundColor = new SolidColorBrush(Colors.Black) }, "STATUS_WORD|POWER_GOOD");
                            this.T3C1P2B2VoutStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P2B2VoutStatus" }, "STATUS_WORD|VOUT_STATUS");
                            this.T3C1P2B2VoutOvStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P2B2VoutOvStatus" }, "STATUS_VOUT|VOUT_OV_FAULT");
                            this.T3C1P2B2VoutUvStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P2B2VoutUvStatus" }, "STATUS_VOUT|VOUT_UV_FAULT");
                            this.T3C1P2B2IoutStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P2B2IoutStatus" }, "STATUS_WORD|IOUT_STATUS");
                            this.T3C1P2B2IoutOcStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P2B2IoutOcStatus" }, "STATUS_IOUT|IOUT_OC_FAULT");
                            this.T3C1P2B2IoutUcStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P2B2IoutUcStatus" }, "STATUS_IOUT|IOUT_UC_FAULT");
                            // BUCK 3
                            this.T3C1P3B3VoutPresenter.Content = LoadControl(new InputControl { Name = "C1P3B3Vout" }, "READ_VOUT");
                            this.T3C1P3B3IoutPresenter.Content = LoadControl(new InputControl { Name = "C1P3B3Iout" }, "READ_IOUT");

                            this.T3C1P3B3OperationPresenter.Content = LoadControl(new ListControl { Name = "C1P3B3Operation" }, "OPERATION");
                            this.T3C1P3B3VoutCommandPresenter.Content = LoadControl(new InputControl { Name = "C1P3B3VoutCommand" }, "VOUT_COMMAND");
                            this.T3C1P3B3VoutMarginHighPresenter.Content = LoadControl(new InputControl { Name = "C1P3B3VoutMarginHigh" }, "VOUT_MARGIN_HIGH");
                            this.T3C1P3B3VoutMarginLowPresenter.Content = LoadControl(new InputControl { Name = "C1P3B3VoutMarginLow" }, "VOUT_MARGIN_LOW");
                            this.T3C1P3B3VoutTransRatePresenter.Content = LoadControl(new InputControl { Name = "C1P3B3VoutTransRate" }, "VOUT_TRANSITION_RATE");
                            this.T3C1P3B3VoutOVFLimitPresenter.Content = LoadControl(new InputControl { Name = "C1P3B3VoutOVFLimit" }, "VOUT_OV_FAULT_LIMIT");
                            this.T3C1P3B3VoutUVFLimitPresenter.Content = LoadControl(new InputControl { Name = "C1P3B3VoutUVFLimit" }, "VOUT_UV_FAULT_LIMIT");
                            this.T3C1P3B3IoutOCFLimitPresenter.Content = LoadControl(new InputControl { Name = "C1P3B3IoutOCFLimit" }, "IOUT_OC_FAULT_LIMIT");
                            this.T3C1P3B3TonDelayPresenter.Content = LoadControl(new InputControl { Name = "C1P3B3TonDelay" }, "TON_DELAY");
                            this.T3C1P3B3TonRisePresenter.Content = LoadControl(new InputControl { Name = "C1P3B3TonRise" }, "TON_RISE");
                            this.T3C1P3B3ToffDelayPresenter.Content = LoadControl(new InputControl { Name = "C1P3B3ToffDelay" }, "TOFF_DELAY");
                            this.T3C1P3B3ToffFallPresenter.Content = LoadControl(new InputControl { Name = "C1P3B3ToffFall" }, "TOFF_FALL");
                            // Status
                            this.T3C1P3B3PowerStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P3B3PowerStatus", OnSetBackgroundColor = new SolidColorBrush(Color.FromRgb(0x75, 0xdf, 0x4a)), OnSetForegroundColor = new SolidColorBrush(Colors.Black) }, "STATUS_WORD|POWER_GOOD");
                            this.T3C1P3B3VoutStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P3B3VoutStatus" }, "STATUS_WORD|VOUT_STATUS");
                            this.T3C1P3B3VoutOvStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P3B3VoutOvStatus" }, "STATUS_VOUT|VOUT_OV_FAULT");
                            this.T3C1P3B3VoutUvStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P3B3VoutUvStatus" }, "STATUS_VOUT|VOUT_UV_FAULT");
                            this.T3C1P3B3IoutStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P3B3IoutStatus" }, "STATUS_WORD|IOUT_STATUS");
                            this.T3C1P3B3IoutOcStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P3B3IoutOcStatus" }, "STATUS_IOUT|IOUT_OC_FAULT");
                            this.T3C1P3B3IoutUcStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P3B3IoutUcStatus" }, "STATUS_IOUT|IOUT_UC_FAULT");
                            // BUCK 4
                            this.T3C1P4B4VoutPresenter.Content = LoadControl(new InputControl { Name = "C1P4B4Vout" }, "READ_VOUT");
                            this.T3C1P4B4IoutPresenter.Content = LoadControl(new InputControl { Name = "C1P4B4Iout" }, "READ_IOUT");

                            this.T3C1P4B4OperationPresenter.Content = LoadControl(new ListControl { Name = "C1P4B4Operation" }, "OPERATION");
                            this.T3C1P4B4VoutCommandPresenter.Content = LoadControl(new InputControl { Name = "C1P4B4VoutCommand" }, "VOUT_COMMAND");
                            this.T3C1P4B4VoutMarginHighPresenter.Content = LoadControl(new InputControl { Name = "C1P4B4VoutMarginHigh" }, "VOUT_MARGIN_HIGH");
                            this.T3C1P4B4VoutMarginLowPresenter.Content = LoadControl(new InputControl { Name = "C1P4B4VoutMarginLow" }, "VOUT_MARGIN_LOW");
                            this.T3C1P4B4VoutTransRatePresenter.Content = LoadControl(new InputControl { Name = "C1P4B4VoutTransRate" }, "VOUT_TRANSITION_RATE");
                            this.T3C1P4B4VoutOVFLimitPresenter.Content = LoadControl(new InputControl { Name = "C1P4B4VoutOVFLimit" }, "VOUT_OV_FAULT_LIMIT");
                            this.T3C1P4B4VoutUVFLimitPresenter.Content = LoadControl(new InputControl { Name = "C1P4B4VoutUVFLimit" }, "VOUT_UV_FAULT_LIMIT");
                            this.T3C1P4B4IoutOCFLimitPresenter.Content = LoadControl(new InputControl { Name = "C1P4B4IoutOCFLimit" }, "IOUT_OC_FAULT_LIMIT");
                            this.T3C1P4B4TonDelayPresenter.Content = LoadControl(new InputControl { Name = "C1P4B4TonDelay" }, "TON_DELAY");
                            this.T3C1P4B4TonRisePresenter.Content = LoadControl(new InputControl { Name = "C1P4B4TonRise" }, "TON_RISE");
                            this.T3C1P4B4ToffDelayPresenter.Content = LoadControl(new InputControl { Name = "C1P4B4ToffDelay" }, "TOFF_DELAY");
                            this.T3C1P4B4ToffFallPresenter.Content = LoadControl(new InputControl { Name = "C1P4B4ToffFall" }, "TOFF_FALL");
                            // Status
                            this.T3C1P4B4PowerStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P4B4PowerStatus", OnSetBackgroundColor = new SolidColorBrush(Color.FromRgb(0x75, 0xdf, 0x4a)), OnSetForegroundColor = new SolidColorBrush(Colors.Black) }, "STATUS_WORD|POWER_GOOD");
                            this.T3C1P4B4VoutStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P4B4VoutStatus" }, "STATUS_WORD|VOUT_STATUS");
                            this.T3C1P4B4VoutOvStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P4B4VoutOvStatus" }, "STATUS_VOUT|VOUT_OV_FAULT");
                            this.T3C1P4B4VoutUvStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P4B4VoutUvStatus" }, "STATUS_VOUT|VOUT_UV_FAULT");
                            this.T3C1P4B4IoutStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P4B4IoutStatus" }, "STATUS_WORD|IOUT_STATUS");
                            this.T3C1P4B4IoutOcStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P4B4IoutOcStatus" }, "STATUS_IOUT|IOUT_OC_FAULT");
                            this.T3C1P4B4IoutUcStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P4B4IoutUcStatus" }, "STATUS_IOUT|IOUT_UC_FAULT");
                            break;
                        #endregion
                        case 2:
                            #region Configuration 2 (1/2,3/4)
                            // BUCK 1
                            this.T3C2P1B1VoutPresenter.Content = LoadControl(new InputControl { Name = "C2P1B1Vout" }, "READ_VOUT");
                            this.T3C2P1B1IoutPresenter.Content = LoadControl(new InputControl { Name = "C2P1B1Iout" }, "READ_IOUT");

                            this.T3C2P1B1OperationPresenter.Content = LoadControl(new ListControl { Name = "C2P1B1Operation" }, "OPERATION");
                            this.T3C2P1B1VoutCommandPresenter.Content = LoadControl(new InputControl { Name = "C2P1B1VoutCommand" }, "VOUT_COMMAND");
                            this.T3C2P1B1VoutMarginHighPresenter.Content = LoadControl(new InputControl { Name = "C2P1B1VoutMarginHigh" }, "VOUT_MARGIN_HIGH");
                            this.T3C2P1B1VoutMarginLowPresenter.Content = LoadControl(new InputControl { Name = "C2P1B1VoutMarginLow" }, "VOUT_MARGIN_LOW");
                            this.T3C2P1B1VoutTransRatePresenter.Content = LoadControl(new InputControl { Name = "C2P1B1VoutTransRate" }, "VOUT_TRANSITION_RATE");
                            this.T3C2P1B1VoutOVFLimitPresenter.Content = LoadControl(new InputControl { Name = "C2P1B1VoutOVFLimit" }, "VOUT_OV_FAULT_LIMIT");
                            this.T3C2P1B1VoutUVFLimitPresenter.Content = LoadControl(new InputControl { Name = "C2P1B1VoutUVFLimit" }, "VOUT_UV_FAULT_LIMIT");
                            this.T3C2P1B1IoutOCFLimitPresenter.Content = LoadControl(new InputControl { Name = "C2P1B1IoutOCFLimit" }, "IOUT_OC_FAULT_LIMIT");
                            this.T3C2P1B1TonDelayPresenter.Content = LoadControl(new InputControl { Name = "C2P1B1TonDelay" }, "TON_DELAY");
                            this.T3C2P1B1TonRisePresenter.Content = LoadControl(new InputControl { Name = "C2P1B1TonRise" }, "TON_RISE");
                            this.T3C2P1B1ToffDelayPresenter.Content = LoadControl(new InputControl { Name = "C2P1B1ToffDelay" }, "TOFF_DELAY");
                            this.T3C2P1B1ToffFallPresenter.Content = LoadControl(new InputControl { Name = "C2P1B1ToffFall" }, "TOFF_FALL");
                            // Status
                            this.T3C2P1B1PowerStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C2P1B1PowerStatus", OnSetBackgroundColor = new SolidColorBrush(Color.FromRgb(0x75, 0xdf, 0x4a)), OnSetForegroundColor = new SolidColorBrush(Colors.Black) }, "STATUS_WORD|POWER_GOOD");
                            this.T3C2P1B1VoutStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C2P1B1VoutStatus" }, "STATUS_WORD|VOUT_STATUS");
                            this.T3C2P1B1VoutOvStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C2P1B1VoutOvStatus" }, "STATUS_VOUT|VOUT_OV_FAULT");
                            this.T3C2P1B1VoutUvStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C2P1B1VoutUvStatus" }, "STATUS_VOUT|VOUT_UV_FAULT");
                            this.T3C2P1B1IoutStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C2P1B1IoutStatus" }, "STATUS_WORD|IOUT_STATUS");
                            this.T3C2P1B1IoutOcStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C2P1B1IoutOcStatus" }, "STATUS_IOUT|IOUT_OC_FAULT");
                            this.T3C2P1B1IoutUcStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C2P1B1IoutUcStatus" }, "STATUS_IOUT|IOUT_UC_FAULT");
                            // BUCK 2/3
                            this.T3C2P2B2VoutPresenter.Content = LoadControl(new InputControl { Name = "C2P2B2Vout" }, "READ_VOUT");
                            this.T3C2P2B2IoutPresenter.Content = LoadControl(new InputControl { Name = "C2P2B2Iout" }, "READ_IOUT");

                            this.T3C2P2B2OperationPresenter.Content = LoadControl(new ListControl { Name = "C2P2B2Operation" }, "OPERATION");
                            this.T3C2P2B2VoutCommandPresenter.Content = LoadControl(new InputControl { Name = "C2P2B2VoutCommand" }, "VOUT_COMMAND");
                            this.T3C2P2B2VoutMarginHighPresenter.Content = LoadControl(new InputControl { Name = "C2P2B2VoutMarginHigh" }, "VOUT_MARGIN_HIGH");
                            this.T3C2P2B2VoutMarginLowPresenter.Content = LoadControl(new InputControl { Name = "C2P2B2VoutMarginLow" }, "VOUT_MARGIN_LOW");
                            this.T3C2P2B2VoutTransRatePresenter.Content = LoadControl(new InputControl { Name = "C2P2B2VoutTransRate" }, "VOUT_TRANSITION_RATE");
                            this.T3C2P2B2VoutOVFLimitPresenter.Content = LoadControl(new InputControl { Name = "C2P2B2VoutOVFLimit" }, "VOUT_OV_FAULT_LIMIT");
                            this.T3C2P2B2VoutUVFLimitPresenter.Content = LoadControl(new InputControl { Name = "C2P2B2VoutUVFLimit" }, "VOUT_UV_FAULT_LIMIT");
                            this.T3C2P2B2IoutOCFLimitPresenter.Content = LoadControl(new InputControl { Name = "C2P2B2IoutOCFLimit" }, "IOUT_OC_FAULT_LIMIT");
                            this.T3C2P2B2TonDelayPresenter.Content = LoadControl(new InputControl { Name = "C2P2B2TonDelay" }, "TON_DELAY");
                            this.T3C2P2B2TonRisePresenter.Content = LoadControl(new InputControl { Name = "C2P2B2TonRise" }, "TON_RISE");
                            this.T3C2P2B2ToffDelayPresenter.Content = LoadControl(new InputControl { Name = "C2P2B2ToffDelay" }, "TOFF_DELAY");
                            this.T3C2P2B2ToffFallPresenter.Content = LoadControl(new InputControl { Name = "C2P2B2ToffFall" }, "TOFF_FALL");
                            // Status
                            this.T3C2P2B2PowerStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C2P2B2PowerStatus", OnSetBackgroundColor = new SolidColorBrush(Color.FromRgb(0x75, 0xdf, 0x4a)), OnSetForegroundColor = new SolidColorBrush(Colors.Black) }, "STATUS_WORD|POWER_GOOD");
                            this.T3C2P2B2VoutStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C2P2B2VoutStatus" }, "STATUS_WORD|VOUT_STATUS");
                            this.T3C2P2B2VoutOvStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C2P2B2VoutOvStatus" }, "STATUS_VOUT|VOUT_OV_FAULT");
                            this.T3C2P2B2VoutUvStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C2P2B2VoutUvStatus" }, "STATUS_VOUT|VOUT_UV_FAULT");
                            this.T3C2P2B2IoutStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C2P2B2IoutStatus" }, "STATUS_WORD|IOUT_STATUS");
                            this.T3C2P2B2IoutOcStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C2P2B2IoutOcStatus" }, "STATUS_IOUT|IOUT_OC_FAULT");
                            this.T3C2P2B2IoutUcStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C2P2B2IoutUcStatus" }, "STATUS_IOUT|IOUT_UC_FAULT");

                            // BUCK 4
                            this.T3C2P4B4VoutPresenter.Content = LoadControl(new InputControl { Name = "C2P4B4Vout" }, "READ_VOUT");
                            this.T3C2P4B4IoutPresenter.Content = LoadControl(new InputControl { Name = "C2P4B4Iout" }, "READ_IOUT");

                            this.T3C2P4B4OperationPresenter.Content = LoadControl(new ListControl { Name = "C2P4B4Operation" }, "OPERATION");
                            this.T3C2P4B4VoutCommandPresenter.Content = LoadControl(new InputControl { Name = "C2P4B4VoutCommand" }, "VOUT_COMMAND");
                            this.T3C2P4B4VoutMarginHighPresenter.Content = LoadControl(new InputControl { Name = "C2P4B4VoutMarginHigh" }, "VOUT_MARGIN_HIGH");
                            this.T3C2P4B4VoutMarginLowPresenter.Content = LoadControl(new InputControl { Name = "C2P4B4VoutMarginLow" }, "VOUT_MARGIN_LOW");
                            this.T3C2P4B4VoutTransRatePresenter.Content = LoadControl(new InputControl { Name = "C2P4B4VoutTransRate" }, "VOUT_TRANSITION_RATE");
                            this.T3C2P4B4VoutOVFLimitPresenter.Content = LoadControl(new InputControl { Name = "C2P4B4VoutOVFLimit" }, "VOUT_OV_FAULT_LIMIT");
                            this.T3C2P4B4VoutUVFLimitPresenter.Content = LoadControl(new InputControl { Name = "C2P4B4VoutUVFLimit" }, "VOUT_UV_FAULT_LIMIT");
                            this.T3C2P4B4IoutOCFLimitPresenter.Content = LoadControl(new InputControl { Name = "C2P4B4IoutOCFLimit" }, "IOUT_OC_FAULT_LIMIT");
                            this.T3C2P4B4TonDelayPresenter.Content = LoadControl(new InputControl { Name = "C2P4B4TonDelay" }, "TON_DELAY");
                            this.T3C2P4B4TonRisePresenter.Content = LoadControl(new InputControl { Name = "C2P4B4TonRise" }, "TON_RISE");
                            this.T3C2P4B4ToffDelayPresenter.Content = LoadControl(new InputControl { Name = "C2P4B4ToffDelay" }, "TOFF_DELAY");
                            this.T3C2P4B4ToffFallPresenter.Content = LoadControl(new InputControl { Name = "C2P4B4ToffFall" }, "TOFF_FALL");
                            // Status
                            this.T3C2P4B4PowerStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C2P4B4PowerStatus" }, "STATUS_WORD|POWER_GOOD");
                            this.T3C2P4B4VoutStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C2P4B4VoutStatus" }, "STATUS_WORD|VOUT_STATUS");
                            this.T3C2P4B4VoutOvStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C2P4B4VoutOvStatus" }, "STATUS_VOUT|VOUT_OV_FAULT");
                            this.T3C2P4B4VoutUvStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C2P4B4VoutUvStatus" }, "STATUS_VOUT|VOUT_UV_FAULT");
                            this.T3C2P4B4IoutStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C2P4B4IoutStatus" }, "STATUS_WORD|IOUT_STATUS");
                            this.T3C2P4B4IoutOcStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C2P4B4IoutOcStatus" }, "STATUS_IOUT|IOUT_OC_FAULT");
                            this.T3C2P4B4IoutUcStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C2P4B4IoutUcStatus" }, "STATUS_IOUT|IOUT_UC_FAULT");
                            break;
                        #endregion
                        case 3:
                            #region Configuration 3 (1,2/3,4)
                            // BUCK 1/2
                            this.T3C3P1B1VoutPresenter.Content = LoadControl(new InputControl { Name = "C3P1B1Vout" }, "READ_VOUT");
                            this.T3C3P1B1IoutPresenter.Content = LoadControl(new InputControl { Name = "C3P1B1Iout" }, "READ_IOUT");

                            this.T3C3P1B1OperationPresenter.Content = LoadControl(new ListControl { Name = "C3P1B1Operation" }, "OPERATION");
                            this.T3C3P1B1VoutCommandPresenter.Content = LoadControl(new InputControl { Name = "C3P1B1VoutCommand" }, "VOUT_COMMAND");
                            this.T3C3P1B1VoutMarginHighPresenter.Content = LoadControl(new InputControl { Name = "C3P1B1VoutMarginHigh" }, "VOUT_MARGIN_HIGH");
                            this.T3C3P1B1VoutMarginLowPresenter.Content = LoadControl(new InputControl { Name = "C3P1B1VoutMarginLow" }, "VOUT_MARGIN_LOW");
                            this.T3C3P1B1VoutTransRatePresenter.Content = LoadControl(new InputControl { Name = "C3P1B1VoutTransRate" }, "VOUT_TRANSITION_RATE");
                            this.T3C3P1B1VoutOVFLimitPresenter.Content = LoadControl(new InputControl { Name = "C3P1B1VoutOVFLimit" }, "VOUT_OV_FAULT_LIMIT");
                            this.T3C3P1B1VoutUVFLimitPresenter.Content = LoadControl(new InputControl { Name = "C3P1B1VoutUVFLimit" }, "VOUT_UV_FAULT_LIMIT");
                            this.T3C3P1B1IoutOCFLimitPresenter.Content = LoadControl(new InputControl { Name = "C3P1B1IoutOCFLimit" }, "IOUT_OC_FAULT_LIMIT");
                            this.T3C3P1B1TonDelayPresenter.Content = LoadControl(new InputControl { Name = "C3P1B1TonDelay" }, "TON_DELAY");
                            this.T3C3P1B1TonRisePresenter.Content = LoadControl(new InputControl { Name = "C3P1B1TonRise" }, "TON_RISE");
                            this.T3C3P1B1ToffDelayPresenter.Content = LoadControl(new InputControl { Name = "C3P1B1ToffDelay" }, "TOFF_DELAY");
                            this.T3C3P1B1ToffFallPresenter.Content = LoadControl(new InputControl { Name = "C3P1B1ToffFall" }, "TOFF_FALL");
                            // Status
                            this.T3C3P1B1PowerStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C3P1B1PowerStatus", OnSetBackgroundColor = new SolidColorBrush(Color.FromRgb(0x75, 0xdf, 0x4a)), OnSetForegroundColor = new SolidColorBrush(Colors.Black) }, "STATUS_WORD|POWER_GOOD");
                            this.T3C3P1B1VoutStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C3P1B1VoutStatus" }, "STATUS_WORD|VOUT_STATUS");
                            this.T3C3P1B1VoutOvStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C3P1B1VoutOvStatus" }, "STATUS_VOUT|VOUT_OV_FAULT");
                            this.T3C3P1B1VoutUvStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C3P1B1VoutUvStatus" }, "STATUS_VOUT|VOUT_UV_FAULT");
                            this.T3C3P1B1IoutStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C3P1B1IoutStatus" }, "STATUS_WORD|IOUT_STATUS");
                            this.T3C3P1B1IoutOcStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C3P1B1IoutOcStatus" }, "STATUS_IOUT|IOUT_OC_FAULT");
                            this.T3C3P1B1IoutUcStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C3P1B1IoutUcStatus" }, "STATUS_IOUT|IOUT_UC_FAULT");

                            // BUCK 3/4
                            this.T3C3P3B3VoutPresenter.Content = LoadControl(new InputControl { Name = "C3P3B3Vout" }, "READ_VOUT");
                            this.T3C3P3B3IoutPresenter.Content = LoadControl(new InputControl { Name = "C3P3B3Iout" }, "READ_IOUT");

                            this.T3C3P3B3OperationPresenter.Content = LoadControl(new ListControl { Name = "C3P3B3Operation" }, "OPERATION");
                            this.T3C3P3B3VoutCommandPresenter.Content = LoadControl(new InputControl { Name = "C3P3B3VoutCommand" }, "VOUT_COMMAND");
                            this.T3C3P3B3VoutMarginHighPresenter.Content = LoadControl(new InputControl { Name = "C3P3B3VoutMarginHigh" }, "VOUT_MARGIN_HIGH");
                            this.T3C3P3B3VoutMarginLowPresenter.Content = LoadControl(new InputControl { Name = "C3P3B3VoutMarginLow" }, "VOUT_MARGIN_LOW");
                            this.T3C3P3B3VoutTransRatePresenter.Content = LoadControl(new InputControl { Name = "C3P3B3VoutTransRate" }, "VOUT_TRANSITION_RATE");
                            this.T3C3P3B3VoutOVFLimitPresenter.Content = LoadControl(new InputControl { Name = "C3P3B3VoutOVFLimit" }, "VOUT_OV_FAULT_LIMIT");
                            this.T3C3P3B3VoutUVFLimitPresenter.Content = LoadControl(new InputControl { Name = "C3P3B3VoutUVFLimit" }, "VOUT_UV_FAULT_LIMIT");
                            this.T3C3P3B3IoutOCFLimitPresenter.Content = LoadControl(new InputControl { Name = "C3P3B3IoutOCFLimit" }, "IOUT_OC_FAULT_LIMIT");
                            this.T3C3P3B3TonDelayPresenter.Content = LoadControl(new InputControl { Name = "C3P3B3TonDelay" }, "TON_DELAY");
                            this.T3C3P3B3TonRisePresenter.Content = LoadControl(new InputControl { Name = "C3P3B3TonRise" }, "TON_RISE");
                            this.T3C3P3B3ToffDelayPresenter.Content = LoadControl(new InputControl { Name = "C3P3B3ToffDelay" }, "TOFF_DELAY");
                            this.T3C3P3B3ToffFallPresenter.Content = LoadControl(new InputControl { Name = "C3P3B3ToffFall" }, "TOFF_FALL");
                            // Status
                            this.T3C3P3B3PowerStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C3P3B3PowerStatus", OnSetBackgroundColor = new SolidColorBrush(Color.FromRgb(0x75, 0xdf, 0x4a)), OnSetForegroundColor = new SolidColorBrush(Colors.Black) }, "STATUS_WORD|POWER_GOOD");
                            this.T3C3P3B3VoutStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C3P3B3VoutStatus" }, "STATUS_WORD|VOUT_STATUS");
                            this.T3C3P3B3VoutOvStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C3P3B3VoutOvStatus" }, "STATUS_VOUT|VOUT_OV_FAULT");
                            this.T3C3P3B3VoutUvStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C3P3B3VoutUvStatus" }, "STATUS_VOUT|VOUT_UV_FAULT");
                            this.T3C3P3B3IoutStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C3P3B3IoutStatus" }, "STATUS_WORD|IOUT_STATUS");
                            this.T3C3P3B3IoutOcStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C3P3B3IoutOcStatus" }, "STATUS_IOUT|IOUT_OC_FAULT");
                            this.T3C3P3B3IoutUcStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C3P3B3IoutUcStatus" }, "STATUS_IOUT|IOUT_UC_FAULT");
                            break;
                        #endregion
                        case 4:
                            #region Configuration 4 (1,2,3/4)
                            // BUCK 1/2/3
                            this.T3C4P1B1VoutPresenter.Content = LoadControl(new InputControl { Name = "C4P1B1Vout" }, "READ_VOUT");
                            this.T3C4P1B1IoutPresenter.Content = LoadControl(new InputControl { Name = "C4P1B1Iout" }, "READ_IOUT");

                            this.T3C4P1B1OperationPresenter.Content = LoadControl(new ListControl { Name = "C4P1B1Operation" }, "OPERATION");
                            this.T3C4P1B1VoutCommandPresenter.Content = LoadControl(new InputControl { Name = "C4P1B1VoutCommand" }, "VOUT_COMMAND");
                            this.T3C4P1B1VoutMarginHighPresenter.Content = LoadControl(new InputControl { Name = "C4P1B1VoutMarginHigh" }, "VOUT_MARGIN_HIGH");
                            this.T3C4P1B1VoutMarginLowPresenter.Content = LoadControl(new InputControl { Name = "C4P1B1VoutMarginLow" }, "VOUT_MARGIN_LOW");
                            this.T3C4P1B1VoutTransRatePresenter.Content = LoadControl(new InputControl { Name = "C4P1B1VoutTransRate" }, "VOUT_TRANSITION_RATE");
                            this.T3C4P1B1VoutOVFLimitPresenter.Content = LoadControl(new InputControl { Name = "C4P1B1VoutOVFLimit" }, "VOUT_OV_FAULT_LIMIT");
                            this.T3C4P1B1VoutUVFLimitPresenter.Content = LoadControl(new InputControl { Name = "C4P1B1VoutUVFLimit" }, "VOUT_UV_FAULT_LIMIT");
                            this.T3C4P1B1IoutOCFLimitPresenter.Content = LoadControl(new InputControl { Name = "C4P1B1IoutOCFLimit" }, "IOUT_OC_FAULT_LIMIT");
                            this.T3C4P1B1TonDelayPresenter.Content = LoadControl(new InputControl { Name = "C4P1B1TonDelay" }, "TON_DELAY");
                            this.T3C4P1B1TonRisePresenter.Content = LoadControl(new InputControl { Name = "C4P1B1TonRise" }, "TON_RISE");
                            this.T3C4P1B1ToffDelayPresenter.Content = LoadControl(new InputControl { Name = "C4P1B1ToffDelay" }, "TOFF_DELAY");
                            this.T3C4P1B1ToffFallPresenter.Content = LoadControl(new InputControl { Name = "C4P1B1ToffFall" }, "TOFF_FALL");
                            // Status
                            this.T3C4P1B1PowerStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C4P1B1PowerStatus", OnSetBackgroundColor = new SolidColorBrush(Color.FromRgb(0x75, 0xdf, 0x4a)), OnSetForegroundColor = new SolidColorBrush(Colors.Black) }, "STATUS_WORD|POWER_GOOD");
                            this.T3C4P1B1VoutStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C4P1B1VoutStatus" }, "STATUS_WORD|VOUT_STATUS");
                            this.T3C4P1B1VoutOvStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C4P1B1VoutOvStatus" }, "STATUS_VOUT|VOUT_OV_FAULT");
                            this.T3C4P1B1VoutUvStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C4P1B1VoutUvStatus" }, "STATUS_VOUT|VOUT_UV_FAULT");
                            this.T3C4P1B1IoutStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C4P1B1IoutStatus" }, "STATUS_WORD|IOUT_STATUS");
                            this.T3C4P1B1IoutOcStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C4P1B1IoutOcStatus" }, "STATUS_IOUT|IOUT_OC_FAULT");
                            this.T3C4P1B1IoutUcStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C4P1B1IoutUcStatus" }, "STATUS_IOUT|IOUT_UC_FAULT");
                            // BUCK 4
                            this.T3C4P4B4VoutPresenter.Content = LoadControl(new InputControl { Name = "C4P4B4Vout" }, "READ_VOUT");
                            this.T3C4P4B4IoutPresenter.Content = LoadControl(new InputControl { Name = "C4P4B4Iout" }, "READ_IOUT");

                            this.T3C4P4B4OperationPresenter.Content = LoadControl(new ListControl { Name = "C4P4B4Operation" }, "OPERATION");
                            this.T3C4P4B4VoutCommandPresenter.Content = LoadControl(new InputControl { Name = "C4P4B4VoutCommand" }, "VOUT_COMMAND");
                            this.T3C4P4B4VoutMarginHighPresenter.Content = LoadControl(new InputControl { Name = "C4P4B4VoutMarginHigh" }, "VOUT_MARGIN_HIGH");
                            this.T3C4P4B4VoutMarginLowPresenter.Content = LoadControl(new InputControl { Name = "C4P4B4VoutMarginLow" }, "VOUT_MARGIN_LOW");
                            this.T3C4P4B4VoutTransRatePresenter.Content = LoadControl(new InputControl { Name = "C4P4B4VoutTransRate" }, "VOUT_TRANSITION_RATE");
                            this.T3C4P4B4VoutOVFLimitPresenter.Content = LoadControl(new InputControl { Name = "C4P4B4VoutOVFLimit" }, "VOUT_OV_FAULT_LIMIT");
                            this.T3C4P4B4VoutUVFLimitPresenter.Content = LoadControl(new InputControl { Name = "C4P4B4VoutUVFLimit" }, "VOUT_UV_FAULT_LIMIT");
                            this.T3C4P4B4IoutOCFLimitPresenter.Content = LoadControl(new InputControl { Name = "C4P4B4IoutOCFLimit" }, "IOUT_OC_FAULT_LIMIT");
                            this.T3C4P4B4TonDelayPresenter.Content = LoadControl(new InputControl { Name = "C4P4B4TonDelay" }, "TON_DELAY");
                            this.T3C4P4B4TonRisePresenter.Content = LoadControl(new InputControl { Name = "C4P4B4TonRise" }, "TON_RISE");
                            this.T3C4P4B4ToffDelayPresenter.Content = LoadControl(new InputControl { Name = "C4P4B4ToffDelay" }, "TOFF_DELAY");
                            this.T3C4P4B4ToffFallPresenter.Content = LoadControl(new InputControl { Name = "C4P4B4ToffFall" }, "TOFF_FALL");
                            // Status
                            this.T3C4P4B4PowerStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C4P4B4PowerStatus", OnSetBackgroundColor = new SolidColorBrush(Color.FromRgb(0x75, 0xdf, 0x4a)), OnSetForegroundColor = new SolidColorBrush(Colors.Black) }, "STATUS_WORD|POWER_GOOD");
                            this.T3C4P4B4VoutStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C4P4B4VoutStatus" }, "STATUS_WORD|VOUT_STATUS");
                            this.T3C4P4B4VoutOvStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C4P4B4VoutOvStatus" }, "STATUS_VOUT|VOUT_OV_FAULT");
                            this.T3C4P4B4VoutUvStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C4P4B4VoutUvStatus" }, "STATUS_VOUT|VOUT_UV_FAULT");
                            this.T3C4P4B4IoutStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C4P4B4IoutStatus" }, "STATUS_WORD|IOUT_STATUS");
                            this.T3C4P4B4IoutOcStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C4P4B4IoutOcStatus" }, "STATUS_IOUT|IOUT_OC_FAULT");
                            this.T3C4P4B4IoutUcStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C4P4B4IoutUcStatus" }, "STATUS_IOUT|IOUT_UC_FAULT");
                            break;
                        #endregion
                        case 5:
                            #region Configuration 5 (1,2,3,4)
                            // BUCK 1
                            this.T3C5P1B1VoutPresenter.Content = LoadControl(new InputControl { Name = "C5P1B1Vout" }, "READ_VOUT");
                            this.T3C5P1B1IoutPresenter.Content = LoadControl(new InputControl { Name = "C5P1B1Iout" }, "READ_IOUT");

                            this.T3C5P1B1OperationPresenter.Content = LoadControl(new ListControl { Name = "C5P1B1Operation" }, "OPERATION");
                            this.T3C5P1B1VoutCommandPresenter.Content = LoadControl(new InputControl { Name = "C5P1B1VoutCommand" }, "VOUT_COMMAND");
                            this.T3C5P1B1VoutMarginHighPresenter.Content = LoadControl(new InputControl { Name = "C5P1B1VoutMarginHigh" }, "VOUT_MARGIN_HIGH");
                            this.T3C5P1B1VoutMarginLowPresenter.Content = LoadControl(new InputControl { Name = "C5P1B1VoutMarginLow" }, "VOUT_MARGIN_LOW");
                            this.T3C5P1B1VoutTransRatePresenter.Content = LoadControl(new InputControl { Name = "C5P1B1VoutTransRate" }, "VOUT_TRANSITION_RATE");
                            this.T3C5P1B1VoutOVFLimitPresenter.Content = LoadControl(new InputControl { Name = "C5P1B1VoutOVFLimit" }, "VOUT_OV_FAULT_LIMIT");
                            this.T3C5P1B1VoutUVFLimitPresenter.Content = LoadControl(new InputControl { Name = "C5P1B1VoutUVFLimit" }, "VOUT_UV_FAULT_LIMIT");
                            this.T3C5P1B1IoutOCFLimitPresenter.Content = LoadControl(new InputControl { Name = "C5P1B1IoutOCFLimit" }, "IOUT_OC_FAULT_LIMIT");
                            this.T3C5P1B1TonDelayPresenter.Content = LoadControl(new InputControl { Name = "C5P1B1TonDelay" }, "TON_DELAY");
                            this.T3C5P1B1TonRisePresenter.Content = LoadControl(new InputControl { Name = "C5P1B1TonRise" }, "TON_RISE");
                            this.T3C5P1B1ToffDelayPresenter.Content = LoadControl(new InputControl { Name = "C5P1B1ToffDelay" }, "TOFF_DELAY");
                            this.T3C5P1B1ToffFallPresenter.Content = LoadControl(new InputControl { Name = "C5P1B1ToffFall" }, "TOFF_FALL");
                            // Status
                            this.T3C5P1B1PowerStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C5P1B1PowerStatus", OnSetBackgroundColor = new SolidColorBrush(Color.FromRgb(0x75, 0xdf, 0x4a)), OnSetForegroundColor = new SolidColorBrush(Colors.Black) }, "STATUS_WORD|POWER_GOOD");
                            this.T3C5P1B1VoutStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C5P1B1VoutStatus" }, "STATUS_WORD|VOUT_STATUS");
                            this.T3C5P1B1VoutOvStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C5P1B1VoutOvStatus" }, "STATUS_VOUT|VOUT_OV_FAULT");
                            this.T3C5P1B1VoutUvStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C5P1B1VoutUvStatus" }, "STATUS_VOUT|VOUT_UV_FAULT");
                            this.T3C5P1B1IoutStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C5P1B1IoutStatus" }, "STATUS_WORD|IOUT_STATUS");
                            this.T3C5P1B1IoutOcStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C5P1B1IoutOcStatus" }, "STATUS_IOUT|IOUT_OC_FAULT");
                            this.T3C5P1B1IoutUcStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C5P1B1IoutUcStatus" }, "STATUS_IOUT|IOUT_UC_FAULT");
                            break;
                            #endregion
                    }
                }
                else
                {
                    switch (selectedConfig)
                    {
                        case 1:
                            #region Configuration 1 (1/2/3/4)
                            // BUCK 1
                            this.C1P1B1VoutPresenter.Content = LoadControl(new InputControl { Name = "C1P1B1Vout" }, "READ_VOUT");
                            this.C1P1B1IoutPresenter.Content = LoadControl(new InputControl { Name = "C1P1B1Iout" }, "READ_IOUT");

                            this.C1P1B1OperationPresenter.Content = LoadControl(new ListControl { Name = "C1P1B1Operation" }, "OPERATION");
                            this.C1P1B1VoutCommandPresenter.Content = LoadControl(new InputControl { Name = "C1P1B1VoutCommand" }, "VOUT_COMMAND");
                            this.C1P1B1VoutMarginHighPresenter.Content = LoadControl(new InputControl { Name = "C1P1B1VoutMarginHigh" }, "VOUT_MARGIN_HIGH");
                            this.C1P1B1VoutMarginLowPresenter.Content = LoadControl(new InputControl { Name = "C1P1B1VoutMarginLow" }, "VOUT_MARGIN_LOW");
                            this.C1P1B1VoutTransRatePresenter.Content = LoadControl(new InputControl { Name = "C1P1B1VoutTransRate" }, "VOUT_TRANSITION_RATE");
                            this.C1P1B1VoutOVFLimitPresenter.Content = LoadControl(new InputControl { Name = "C1P1B1VoutOVFLimit" }, "VOUT_OV_FAULT_LIMIT");
                            this.C1P1B1VoutUVFLimitPresenter.Content = LoadControl(new InputControl { Name = "C1P1B1VoutUVFLimit" }, "VOUT_UV_FAULT_LIMIT");
                            this.C1P1B1IoutOCFLimitPresenter.Content = LoadControl(new InputControl { Name = "C1P1B1IoutOCFLimit" }, "IOUT_OC_FAULT_LIMIT");
                            this.C1P1B1IoutUCFPresenter.Content = LoadControl(new InputControl { Name = "C1P1B1IoutUCF" }, "IOUT_UC_FAULT_LIMIT");
                            this.C1P1B1PowerGoodOnPresenter.Content = LoadControl(new InputControl { Name = "C1P1B1PowerGoodOn" }, "POWER_GOOD_ON");
                            this.C1P1B1PowerGoodOffPresenter.Content = LoadControl(new InputControl { Name = "C1P1B1PowerGoodOff" }, "POWER_GOOD_OFF");
                            this.C1P1B1TonDelayPresenter.Content = LoadControl(new InputControl { Name = "C1P1B1TonDelay" }, "TON_DELAY");
                            this.C1P1B1TonRisePresenter.Content = LoadControl(new InputControl { Name = "C1P1B1TonRise" }, "TON_RISE");
                            this.C1P1B1ToffDelayPresenter.Content = LoadControl(new InputControl { Name = "C1P1B1ToffDelay" }, "TOFF_DELAY");
                            this.C1P1B1ToffFallPresenter.Content = LoadControl(new InputControl { Name = "C1P1B1ToffFall" }, "TOFF_FALL");
                            // Status
                            this.C1P1B1PowerStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P1B1PowerStatus", OnSetBackgroundColor = new SolidColorBrush(Color.FromRgb(0x75, 0xdf, 0x4a)), OnSetForegroundColor = new SolidColorBrush(Colors.Black) }, "STATUS_WORD|POWER_GOOD");
                            this.C1P1B1VoutStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P1B1VoutStatus" }, "STATUS_WORD|VOUT_STATUS");
                            this.C1P1B1VoutOvStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P1B1VoutOvStatus" }, "STATUS_VOUT|VOUT_OV_FAULT");
                            this.C1P1B1VoutUvStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P1B1VoutUvStatus" }, "STATUS_VOUT|VOUT_UV_FAULT");
                            this.C1P1B1IoutStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P1B1IoutStatus" }, "STATUS_WORD|IOUT_STATUS");
                            this.C1P1B1IoutOcStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P1B1IoutOcStatus" }, "STATUS_IOUT|IOUT_OC_FAULT");
                            this.C1P1B1IoutUcStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P1B1IoutUcStatus" }, "STATUS_IOUT|IOUT_UC_FAULT");

                            // BUCK 2
                            this.C1P2B2VoutPresenter.Content = LoadControl(new InputControl { Name = "C1P2B2Vout" }, "READ_VOUT");
                            this.C1P2B2IoutPresenter.Content = LoadControl(new InputControl { Name = "C1P2B2Iout" }, "READ_IOUT");

                            this.C1P2B2OperationPresenter.Content = LoadControl(new ListControl { Name = "C1P2B2Operation" }, "OPERATION");
                            this.C1P2B2VoutCommandPresenter.Content = LoadControl(new InputControl { Name = "C1P2B2VoutCommand" }, "VOUT_COMMAND");
                            this.C1P2B2VoutMarginHighPresenter.Content = LoadControl(new InputControl { Name = "C1P2B2VoutMarginHigh" }, "VOUT_MARGIN_HIGH");
                            this.C1P2B2VoutMarginLowPresenter.Content = LoadControl(new InputControl { Name = "C1P2B2VoutMarginLow" }, "VOUT_MARGIN_LOW");
                            this.C1P2B2VoutTransRatePresenter.Content = LoadControl(new InputControl { Name = "C1P2B2VoutTransRate" }, "VOUT_TRANSITION_RATE");
                            this.C1P2B2VoutOVFLimitPresenter.Content = LoadControl(new InputControl { Name = "C1P2B2VoutOVFLimit" }, "VOUT_OV_FAULT_LIMIT");
                            this.C1P2B2VoutUVFLimitPresenter.Content = LoadControl(new InputControl { Name = "C1P2B2VoutUVFLimit" }, "VOUT_UV_FAULT_LIMIT");
                            this.C1P2B2IoutOCFLimitPresenter.Content = LoadControl(new InputControl { Name = "C1P2B2IoutOCFLimit" }, "IOUT_OC_FAULT_LIMIT");
                            this.C1P2B2IoutUCFPresenter.Content = LoadControl(new InputControl { Name = "C1P2B2IoutUCF" }, "IOUT_UC_FAULT_LIMIT");
                            this.C1P2B2PowerGoodOnPresenter.Content = LoadControl(new InputControl { Name = "C1P2B2PowerGoodOn" }, "POWER_GOOD_ON");
                            this.C1P2B2PowerGoodOffPresenter.Content = LoadControl(new InputControl { Name = "C1P2B2PowerGoodOff" }, "POWER_GOOD_OFF");
                            this.C1P2B2TonDelayPresenter.Content = LoadControl(new InputControl { Name = "C1P2B2TonDelay" }, "TON_DELAY");
                            this.C1P2B2TonRisePresenter.Content = LoadControl(new InputControl { Name = "C1P2B2TonRise" }, "TON_RISE");
                            this.C1P2B2ToffDelayPresenter.Content = LoadControl(new InputControl { Name = "C1P2B2ToffDelay" }, "TOFF_DELAY");
                            this.C1P2B2ToffFallPresenter.Content = LoadControl(new InputControl { Name = "C1P2B2ToffFall" }, "TOFF_FALL");
                            // Status
                            this.C1P2B2PowerStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P2B2PowerStatus", OnSetBackgroundColor = new SolidColorBrush(Color.FromRgb(0x75, 0xdf, 0x4a)), OnSetForegroundColor = new SolidColorBrush(Colors.Black) }, "STATUS_WORD|POWER_GOOD");
                            this.C1P2B2VoutStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P2B2VoutStatus" }, "STATUS_WORD|VOUT_STATUS");
                            this.C1P2B2VoutOvStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P2B2VoutOvStatus" }, "STATUS_VOUT|VOUT_OV_FAULT");
                            this.C1P2B2VoutUvStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P2B2VoutUvStatus" }, "STATUS_VOUT|VOUT_UV_FAULT");
                            this.C1P2B2IoutStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P2B2IoutStatus" }, "STATUS_WORD|IOUT_STATUS");
                            this.C1P2B2IoutOcStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P2B2IoutOcStatus" }, "STATUS_IOUT|IOUT_OC_FAULT");
                            this.C1P2B2IoutUcStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P2B2IoutUcStatus" }, "STATUS_IOUT|IOUT_UC_FAULT");
                            // BUCK 3
                            this.C1P3B3VoutPresenter.Content = LoadControl(new InputControl { Name = "C1P3B3Vout" }, "READ_VOUT");
                            this.C1P3B3IoutPresenter.Content = LoadControl(new InputControl { Name = "C1P3B3Iout" }, "READ_IOUT");

                            this.C1P3B3OperationPresenter.Content = LoadControl(new ListControl { Name = "C1P3B3Operation" }, "OPERATION");
                            this.C1P3B3VoutCommandPresenter.Content = LoadControl(new InputControl { Name = "C1P3B3VoutCommand" }, "VOUT_COMMAND");
                            this.C1P3B3VoutMarginHighPresenter.Content = LoadControl(new InputControl { Name = "C1P3B3VoutMarginHigh" }, "VOUT_MARGIN_HIGH");
                            this.C1P3B3VoutMarginLowPresenter.Content = LoadControl(new InputControl { Name = "C1P3B3VoutMarginLow" }, "VOUT_MARGIN_LOW");
                            this.C1P3B3VoutTransRatePresenter.Content = LoadControl(new InputControl { Name = "C1P3B3VoutTransRate" }, "VOUT_TRANSITION_RATE");
                            this.C1P3B3VoutOVFLimitPresenter.Content = LoadControl(new InputControl { Name = "C1P3B3VoutOVFLimit" }, "VOUT_OV_FAULT_LIMIT");
                            this.C1P3B3VoutUVFLimitPresenter.Content = LoadControl(new InputControl { Name = "C1P3B3VoutUVFLimit" }, "VOUT_UV_FAULT_LIMIT");
                            this.C1P3B3IoutOCFLimitPresenter.Content = LoadControl(new InputControl { Name = "C1P3B3IoutOCFLimit" }, "IOUT_OC_FAULT_LIMIT");
                            this.C1P3B3IoutUCFPresenter.Content = LoadControl(new InputControl { Name = "C1P3B3IoutUCF" }, "IOUT_UC_FAULT_LIMIT");
                            this.C1P3B3PowerGoodOnPresenter.Content = LoadControl(new InputControl { Name = "C1P3B3PowerGoodOn" }, "POWER_GOOD_ON");
                            this.C1P3B3PowerGoodOffPresenter.Content = LoadControl(new InputControl { Name = "C1P3B3PowerGoodOff" }, "POWER_GOOD_OFF");
                            this.C1P3B3TonDelayPresenter.Content = LoadControl(new InputControl { Name = "C1P3B3TonDelay" }, "TON_DELAY");
                            this.C1P3B3TonRisePresenter.Content = LoadControl(new InputControl { Name = "C1P3B3TonRise" }, "TON_RISE");
                            this.C1P3B3ToffDelayPresenter.Content = LoadControl(new InputControl { Name = "C1P3B3ToffDelay" }, "TOFF_DELAY");
                            this.C1P3B3ToffFallPresenter.Content = LoadControl(new InputControl { Name = "C1P3B3ToffFall" }, "TOFF_FALL");
                            // Status
                            this.C1P3B3PowerStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P3B3PowerStatus", OnSetBackgroundColor = new SolidColorBrush(Color.FromRgb(0x75, 0xdf, 0x4a)), OnSetForegroundColor = new SolidColorBrush(Colors.Black) }, "STATUS_WORD|POWER_GOOD");
                            this.C1P3B3VoutStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P3B3VoutStatus" }, "STATUS_WORD|VOUT_STATUS");
                            this.C1P3B3VoutOvStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P3B3VoutOvStatus" }, "STATUS_VOUT|VOUT_OV_FAULT");
                            this.C1P3B3VoutUvStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P3B3VoutUvStatus" }, "STATUS_VOUT|VOUT_UV_FAULT");
                            this.C1P3B3IoutStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P3B3IoutStatus" }, "STATUS_WORD|IOUT_STATUS");
                            this.C1P3B3IoutOcStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P3B3IoutOcStatus" }, "STATUS_IOUT|IOUT_OC_FAULT");
                            this.C1P3B3IoutUcStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P3B3IoutUcStatus" }, "STATUS_IOUT|IOUT_UC_FAULT");
                            // BUCK 4
                            this.C1P4B4VoutPresenter.Content = LoadControl(new InputControl { Name = "C1P4B4Vout" }, "READ_VOUT");
                            this.C1P4B4IoutPresenter.Content = LoadControl(new InputControl { Name = "C1P4B4Iout" }, "READ_IOUT");

                            this.C1P4B4OperationPresenter.Content = LoadControl(new ListControl { Name = "C1P4B4Operation" }, "OPERATION");
                            this.C1P4B4VoutCommandPresenter.Content = LoadControl(new InputControl { Name = "C1P4B4VoutCommand" }, "VOUT_COMMAND");
                            this.C1P4B4VoutMarginHighPresenter.Content = LoadControl(new InputControl { Name = "C1P4B4VoutMarginHigh" }, "VOUT_MARGIN_HIGH");
                            this.C1P4B4VoutMarginLowPresenter.Content = LoadControl(new InputControl { Name = "C1P4B4VoutMarginLow" }, "VOUT_MARGIN_LOW");
                            this.C1P4B4VoutTransRatePresenter.Content = LoadControl(new InputControl { Name = "C1P4B4VoutTransRate" }, "VOUT_TRANSITION_RATE");
                            this.C1P4B4VoutOVFLimitPresenter.Content = LoadControl(new InputControl { Name = "C1P4B4VoutOVFLimit" }, "VOUT_OV_FAULT_LIMIT");
                            this.C1P4B4VoutUVFLimitPresenter.Content = LoadControl(new InputControl { Name = "C1P4B4VoutUVFLimit" }, "VOUT_UV_FAULT_LIMIT");
                            this.C1P4B4IoutOCFLimitPresenter.Content = LoadControl(new InputControl { Name = "C1P4B4IoutOCFLimit" }, "IOUT_OC_FAULT_LIMIT");
                            this.C1P4B4IoutUCFPresenter.Content = LoadControl(new InputControl { Name = "C1P4B4IoutUCF" }, "IOUT_UC_FAULT_LIMIT");
                            this.C1P4B4PowerGoodOnPresenter.Content = LoadControl(new InputControl { Name = "C1P4B4PowerGoodOn" }, "POWER_GOOD_ON");
                            this.C1P4B4PowerGoodOffPresenter.Content = LoadControl(new InputControl { Name = "C1P4B4PowerGoodOff" }, "POWER_GOOD_OFF");
                            this.C1P4B4TonDelayPresenter.Content = LoadControl(new InputControl { Name = "C1P4B4TonDelay" }, "TON_DELAY");
                            this.C1P4B4TonRisePresenter.Content = LoadControl(new InputControl { Name = "C1P4B4TonRise" }, "TON_RISE");
                            this.C1P4B4ToffDelayPresenter.Content = LoadControl(new InputControl { Name = "C1P4B4ToffDelay" }, "TOFF_DELAY");
                            this.C1P4B4ToffFallPresenter.Content = LoadControl(new InputControl { Name = "C1P4B4ToffFall" }, "TOFF_FALL");
                            // Status
                            this.C1P4B4PowerStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P4B4PowerStatus", OnSetBackgroundColor = new SolidColorBrush(Color.FromRgb(0x75, 0xdf, 0x4a)), OnSetForegroundColor = new SolidColorBrush(Colors.Black) }, "STATUS_WORD|POWER_GOOD");
                            this.C1P4B4VoutStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P4B4VoutStatus" }, "STATUS_WORD|VOUT_STATUS");
                            this.C1P4B4VoutOvStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P4B4VoutOvStatus" }, "STATUS_VOUT|VOUT_OV_FAULT");
                            this.C1P4B4VoutUvStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P4B4VoutUvStatus" }, "STATUS_VOUT|VOUT_UV_FAULT");
                            this.C1P4B4IoutStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P4B4IoutStatus" }, "STATUS_WORD|IOUT_STATUS");
                            this.C1P4B4IoutOcStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P4B4IoutOcStatus" }, "STATUS_IOUT|IOUT_OC_FAULT");
                            this.C1P4B4IoutUcStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P4B4IoutUcStatus" }, "STATUS_IOUT|IOUT_UC_FAULT");
                            break;
                        #endregion
                        case 2:
                            #region Configuration 2 (1/2,3/4)
                            // BUCK 1
                            this.C2P1B1VoutPresenter.Content = LoadControl(new InputControl { Name = "C2P1B1Vout" }, "READ_VOUT");
                            this.C2P1B1IoutPresenter.Content = LoadControl(new InputControl { Name = "C2P1B1Iout" }, "READ_IOUT");

                            this.C2P1B1OperationPresenter.Content = LoadControl(new ListControl { Name = "C2P1B1Operation" }, "OPERATION");
                            this.C2P1B1VoutCommandPresenter.Content = LoadControl(new InputControl { Name = "C2P1B1VoutCommand" }, "VOUT_COMMAND");
                            this.C2P1B1VoutMarginHighPresenter.Content = LoadControl(new InputControl { Name = "C2P1B1VoutMarginHigh" }, "VOUT_MARGIN_HIGH");
                            this.C2P1B1VoutMarginLowPresenter.Content = LoadControl(new InputControl { Name = "C2P1B1VoutMarginLow" }, "VOUT_MARGIN_LOW");
                            this.C2P1B1VoutTransRatePresenter.Content = LoadControl(new InputControl { Name = "C2P1B1VoutTransRate" }, "VOUT_TRANSITION_RATE");
                            this.C2P1B1VoutOVFLimitPresenter.Content = LoadControl(new InputControl { Name = "C2P1B1VoutOVFLimit" }, "VOUT_OV_FAULT_LIMIT");
                            this.C2P1B1VoutUVFLimitPresenter.Content = LoadControl(new InputControl { Name = "C2P1B1VoutUVFLimit" }, "VOUT_UV_FAULT_LIMIT");
                            this.C2P1B1IoutOCFLimitPresenter.Content = LoadControl(new InputControl { Name = "C2P1B1IoutOCFLimit" }, "IOUT_OC_FAULT_LIMIT");
                            this.C2P1B1IoutUCFPresenter.Content = LoadControl(new InputControl { Name = "C2P1B1IoutUCF" }, "IOUT_UC_FAULT_LIMIT");
                            this.C2P1B1PowerGoodOnPresenter.Content = LoadControl(new InputControl { Name = "C2P1B1PowerGoodOn" }, "POWER_GOOD_ON");
                            this.C2P1B1PowerGoodOffPresenter.Content = LoadControl(new InputControl { Name = "C2P1B1PowerGoodOff" }, "POWER_GOOD_OFF");
                            this.C2P1B1TonDelayPresenter.Content = LoadControl(new InputControl { Name = "C2P1B1TonDelay" }, "TON_DELAY");
                            this.C2P1B1TonRisePresenter.Content = LoadControl(new InputControl { Name = "C2P1B1TonRise" }, "TON_RISE");
                            this.C2P1B1ToffDelayPresenter.Content = LoadControl(new InputControl { Name = "C2P1B1ToffDelay" }, "TOFF_DELAY");
                            this.C2P1B1ToffFallPresenter.Content = LoadControl(new InputControl { Name = "C2P1B1ToffFall" }, "TOFF_FALL");
                            // Status
                            this.C2P1B1PowerStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C2P1B1PowerStatus", OnSetBackgroundColor = new SolidColorBrush(Color.FromRgb(0x75, 0xdf, 0x4a)), OnSetForegroundColor = new SolidColorBrush(Colors.Black) }, "STATUS_WORD|POWER_GOOD");
                            this.C2P1B1VoutStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C2P1B1VoutStatus" }, "STATUS_WORD|VOUT_STATUS");
                            this.C2P1B1VoutOvStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C2P1B1VoutOvStatus" }, "STATUS_VOUT|VOUT_OV_FAULT");
                            this.C2P1B1VoutUvStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C2P1B1VoutUvStatus" }, "STATUS_VOUT|VOUT_UV_FAULT");
                            this.C2P1B1IoutStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C2P1B1IoutStatus" }, "STATUS_WORD|IOUT_STATUS");
                            this.C2P1B1IoutOcStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C2P1B1IoutOcStatus" }, "STATUS_IOUT|IOUT_OC_FAULT");
                            this.C2P1B1IoutUcStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C2P1B1IoutUcStatus" }, "STATUS_IOUT|IOUT_UC_FAULT");

                            // BUCK 2/3
                            this.C2P2B2VoutPresenter.Content = LoadControl(new InputControl { Name = "C2P2B2Vout" }, "READ_VOUT");
                            this.C2P2B2IoutPresenter.Content = LoadControl(new InputControl { Name = "C2P2B2Iout" }, "READ_IOUT");

                            this.C2P2B2OperationPresenter.Content = LoadControl(new ListControl { Name = "C2P2B2Operation" }, "OPERATION");
                            this.C2P2B2VoutCommandPresenter.Content = LoadControl(new InputControl { Name = "C2P2B2VoutCommand" }, "VOUT_COMMAND");
                            this.C2P2B2VoutMarginHighPresenter.Content = LoadControl(new InputControl { Name = "C2P2B2VoutMarginHigh" }, "VOUT_MARGIN_HIGH");
                            this.C2P2B2VoutMarginLowPresenter.Content = LoadControl(new InputControl { Name = "C2P2B2VoutMarginLow" }, "VOUT_MARGIN_LOW");
                            this.C2P2B2VoutTransRatePresenter.Content = LoadControl(new InputControl { Name = "C2P2B2VoutTransRate" }, "VOUT_TRANSITION_RATE");
                            this.C2P2B2VoutOVFLimitPresenter.Content = LoadControl(new InputControl { Name = "C2P2B2VoutOVFLimit" }, "VOUT_OV_FAULT_LIMIT");
                            this.C2P2B2VoutUVFLimitPresenter.Content = LoadControl(new InputControl { Name = "C2P2B2VoutUVFLimit" }, "VOUT_UV_FAULT_LIMIT");
                            this.C2P2B2IoutOCFLimitPresenter.Content = LoadControl(new InputControl { Name = "C2P2B2IoutOCFLimit" }, "IOUT_OC_FAULT_LIMIT");
                            this.C2P2B2IoutUCFPresenter.Content = LoadControl(new InputControl { Name = "C2P2B2IoutUCF" }, "IOUT_UC_FAULT_LIMIT");
                            this.C2P2B2PowerGoodOnPresenter.Content = LoadControl(new InputControl { Name = "C2P2B2PowerGoodOn" }, "POWER_GOOD_ON");
                            this.C2P2B2PowerGoodOffPresenter.Content = LoadControl(new InputControl { Name = "C2P2B2PowerGoodOff" }, "POWER_GOOD_OFF");
                            this.C2P2B2TonDelayPresenter.Content = LoadControl(new InputControl { Name = "C2P2B2TonDelay" }, "TON_DELAY");
                            this.C2P2B2TonRisePresenter.Content = LoadControl(new InputControl { Name = "C2P2B2TonRise" }, "TON_RISE");
                            this.C2P2B2ToffDelayPresenter.Content = LoadControl(new InputControl { Name = "C2P2B2ToffDelay" }, "TOFF_DELAY");
                            this.C2P2B2ToffFallPresenter.Content = LoadControl(new InputControl { Name = "C2P2B2ToffFall" }, "TOFF_FALL");
                            // Status
                            this.C2P2B2PowerStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C2P2B2PowerStatus", OnSetBackgroundColor = new SolidColorBrush(Color.FromRgb(0x75, 0xdf, 0x4a)), OnSetForegroundColor = new SolidColorBrush(Colors.Black) }, "STATUS_WORD|POWER_GOOD");
                            this.C2P2B2VoutStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C2P2B2VoutStatus" }, "STATUS_WORD|VOUT_STATUS");
                            this.C2P2B2VoutOvStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C2P2B2VoutOvStatus" }, "STATUS_VOUT|VOUT_OV_FAULT");
                            this.C2P2B2VoutUvStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C2P2B2VoutUvStatus" }, "STATUS_VOUT|VOUT_UV_FAULT");
                            this.C2P2B2IoutStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C2P2B2IoutStatus" }, "STATUS_WORD|IOUT_STATUS");
                            this.C2P2B2IoutOcStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C2P2B2IoutOcStatus" }, "STATUS_IOUT|IOUT_OC_FAULT");
                            this.C2P2B2IoutUcStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C2P2B2IoutUcStatus" }, "STATUS_IOUT|IOUT_UC_FAULT");

                            //BUCK 3
                            Page_IOUT_Readings = LoadControl(new InputControl { Name = "C2P3B3Iout" }, "READ_IOUT");

                            //BUCK 4
                            this.C2P4B4VoutPresenter.Content = LoadControl(new InputControl { Name = "C2P4B4Vout" }, "READ_VOUT");
                            this.C2P4B4IoutPresenter.Content = LoadControl(new InputControl { Name = "C2P4B4Iout" }, "READ_IOUT");

                            this.C2P4B4OperationPresenter.Content = LoadControl(new ListControl { Name = "C2P4B4Operation" }, "OPERATION");
                            this.C2P4B4VoutCommandPresenter.Content = LoadControl(new InputControl { Name = "C2P4B4VoutCommand" }, "VOUT_COMMAND");
                            this.C2P4B4VoutMarginHighPresenter.Content = LoadControl(new InputControl { Name = "C2P4B4VoutMarginHigh" }, "VOUT_MARGIN_HIGH");
                            this.C2P4B4VoutMarginLowPresenter.Content = LoadControl(new InputControl { Name = "C2P4B4VoutMarginLow" }, "VOUT_MARGIN_LOW");
                            this.C2P4B4VoutTransRatePresenter.Content = LoadControl(new InputControl { Name = "C2P4B4VoutTransRate" }, "VOUT_TRANSITION_RATE");
                            this.C2P4B4VoutOVFLimitPresenter.Content = LoadControl(new InputControl { Name = "C2P4B4VoutOVFLimit" }, "VOUT_OV_FAULT_LIMIT");
                            this.C2P4B4VoutUVFLimitPresenter.Content = LoadControl(new InputControl { Name = "C2P4B4VoutUVFLimit" }, "VOUT_UV_FAULT_LIMIT");
                            this.C2P4B4IoutOCFLimitPresenter.Content = LoadControl(new InputControl { Name = "C2P4B4IoutOCFLimit" }, "IOUT_OC_FAULT_LIMIT");
                            this.C2P4B4IoutUCFPresenter.Content = LoadControl(new InputControl { Name = "C2P4B4IoutUCF" }, "IOUT_UC_FAULT_LIMIT");
                            this.C2P4B4PowerGoodOnPresenter.Content = LoadControl(new InputControl { Name = "C2P4B4PowerGoodOn" }, "POWER_GOOD_ON");
                            this.C2P4B4PowerGoodOffPresenter.Content = LoadControl(new InputControl { Name = "C2P4B4PowerGoodOff" }, "POWER_GOOD_OFF");
                            this.C2P4B4TonDelayPresenter.Content = LoadControl(new InputControl { Name = "C2P4B4TonDelay" }, "TON_DELAY");
                            this.C2P4B4TonRisePresenter.Content = LoadControl(new InputControl { Name = "C2P4B4TonRise" }, "TON_RISE");
                            this.C2P4B4ToffDelayPresenter.Content = LoadControl(new InputControl { Name = "C2P4B4ToffDelay" }, "TOFF_DELAY");
                            this.C2P4B4ToffFallPresenter.Content = LoadControl(new InputControl { Name = "C2P4B4ToffFall" }, "TOFF_FALL");
                            // Status
                            this.C2P4B4PowerStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C2P4B4PowerStatus" }, "STATUS_WORD|POWER_GOOD");
                            this.C2P4B4VoutStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C2P4B4VoutStatus" }, "STATUS_WORD|VOUT_STATUS");
                            this.C2P4B4VoutOvStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C2P4B4VoutOvStatus" }, "STATUS_VOUT|VOUT_OV_FAULT");
                            this.C2P4B4VoutUvStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C2P4B4VoutUvStatus" }, "STATUS_VOUT|VOUT_UV_FAULT");
                            this.C2P4B4IoutStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C2P4B4IoutStatus" }, "STATUS_WORD|IOUT_STATUS");
                            this.C2P4B4IoutOcStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C2P4B4IoutOcStatus" }, "STATUS_IOUT|IOUT_OC_FAULT");
                            this.C2P4B4IoutUcStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C2P4B4IoutUcStatus" }, "STATUS_IOUT|IOUT_UC_FAULT");
                            break;
                        #endregion
                        case 3:
                            #region Configuration 3 (1,2/3,4)
                            // BUCK 1/2
                            this.C3P1B1VoutPresenter.Content = LoadControl(new InputControl { Name = "C3P1B1Vout" }, "READ_VOUT");
                            this.C3P1B1IoutPresenter.Content = LoadControl(new InputControl { Name = "C3P1B1Iout" }, "READ_IOUT");

                            this.C3P1B1OperationPresenter.Content = LoadControl(new ListControl { Name = "C3P1B1Operation" }, "OPERATION");
                            this.C3P1B1VoutCommandPresenter.Content = LoadControl(new InputControl { Name = "C3P1B1VoutCommand" }, "VOUT_COMMAND");
                            this.C3P1B1VoutMarginHighPresenter.Content = LoadControl(new InputControl { Name = "C3P1B1VoutMarginHigh" }, "VOUT_MARGIN_HIGH");
                            this.C3P1B1VoutMarginLowPresenter.Content = LoadControl(new InputControl { Name = "C3P1B1VoutMarginLow" }, "VOUT_MARGIN_LOW");
                            this.C3P1B1VoutTransRatePresenter.Content = LoadControl(new InputControl { Name = "C3P1B1VoutTransRate" }, "VOUT_TRANSITION_RATE");
                            this.C3P1B1VoutOVFLimitPresenter.Content = LoadControl(new InputControl { Name = "C3P1B1VoutOVFLimit" }, "VOUT_OV_FAULT_LIMIT");
                            this.C3P1B1VoutUVFLimitPresenter.Content = LoadControl(new InputControl { Name = "C3P1B1VoutUVFLimit" }, "VOUT_UV_FAULT_LIMIT");
                            this.C3P1B1IoutOCFLimitPresenter.Content = LoadControl(new InputControl { Name = "C3P1B1IoutOCFLimit" }, "IOUT_OC_FAULT_LIMIT");
                            this.C3P1B1IoutUCFPresenter.Content = LoadControl(new InputControl { Name = "C3P1B1IoutUCF" }, "IOUT_UC_FAULT_LIMIT");
                            this.C3P1B1PowerGoodOnPresenter.Content = LoadControl(new InputControl { Name = "C3P1B1PowerGoodOn" }, "POWER_GOOD_ON");
                            this.C3P1B1PowerGoodOffPresenter.Content = LoadControl(new InputControl { Name = "C3P1B1PowerGoodOff" }, "POWER_GOOD_OFF");
                            this.C3P1B1TonDelayPresenter.Content = LoadControl(new InputControl { Name = "C3P1B1TonDelay" }, "TON_DELAY");
                            this.C3P1B1TonRisePresenter.Content = LoadControl(new InputControl { Name = "C3P1B1TonRise" }, "TON_RISE");
                            this.C3P1B1ToffDelayPresenter.Content = LoadControl(new InputControl { Name = "C3P1B1ToffDelay" }, "TOFF_DELAY");
                            this.C3P1B1ToffFallPresenter.Content = LoadControl(new InputControl { Name = "C3P1B1ToffFall" }, "TOFF_FALL");
                            // Status
                            this.C3P1B1PowerStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C3P1B1PowerStatus", OnSetBackgroundColor = new SolidColorBrush(Color.FromRgb(0x75, 0xdf, 0x4a)), OnSetForegroundColor = new SolidColorBrush(Colors.Black) }, "STATUS_WORD|POWER_GOOD");
                            this.C3P1B1VoutStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C3P1B1VoutStatus" }, "STATUS_WORD|VOUT_STATUS");
                            this.C3P1B1VoutOvStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C3P1B1VoutOvStatus" }, "STATUS_VOUT|VOUT_OV_FAULT");
                            this.C3P1B1VoutUvStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C3P1B1VoutUvStatus" }, "STATUS_VOUT|VOUT_UV_FAULT");
                            this.C3P1B1IoutStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C3P1B1IoutStatus" }, "STATUS_WORD|IOUT_STATUS");
                            this.C3P1B1IoutOcStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C3P1B1IoutOcStatus" }, "STATUS_IOUT|IOUT_OC_FAULT");
                            this.C3P1B1IoutUcStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C3P1B1IoutUcStatus" }, "STATUS_IOUT|IOUT_UC_FAULT");

                            //BUCK 2
                            Page_IOUT_Readings = LoadControl(new InputControl { Name = "C3P2B2Iout" }, "READ_IOUT");

                            // BUCK 3/4
                            this.C3P3B3VoutPresenter.Content = LoadControl(new InputControl { Name = "C3P3B3Vout" }, "READ_VOUT");
                            this.C3P3B3IoutPresenter.Content = LoadControl(new InputControl { Name = "C3P3B3Iout" }, "READ_IOUT");

                            this.C3P3B3OperationPresenter.Content = LoadControl(new ListControl { Name = "C3P3B3Operation" }, "OPERATION");
                            this.C3P3B3VoutCommandPresenter.Content = LoadControl(new InputControl { Name = "C3P3B3VoutCommand" }, "VOUT_COMMAND");
                            this.C3P3B3VoutMarginHighPresenter.Content = LoadControl(new InputControl { Name = "C3P3B3VoutMarginHigh" }, "VOUT_MARGIN_HIGH");
                            this.C3P3B3VoutMarginLowPresenter.Content = LoadControl(new InputControl { Name = "C3P3B3VoutMarginLow" }, "VOUT_MARGIN_LOW");
                            this.C3P3B3VoutTransRatePresenter.Content = LoadControl(new InputControl { Name = "C3P3B3VoutTransRate" }, "VOUT_TRANSITION_RATE");
                            this.C3P3B3VoutOVFLimitPresenter.Content = LoadControl(new InputControl { Name = "C3P3B3VoutOVFLimit" }, "VOUT_OV_FAULT_LIMIT");
                            this.C3P3B3VoutUVFLimitPresenter.Content = LoadControl(new InputControl { Name = "C3P3B3VoutUVFLimit" }, "VOUT_UV_FAULT_LIMIT");
                            this.C3P3B3IoutOCFLimitPresenter.Content = LoadControl(new InputControl { Name = "C3P3B3IoutOCFLimit" }, "IOUT_OC_FAULT_LIMIT");
                            this.C3P3B3IoutUCFPresenter.Content = LoadControl(new InputControl { Name = "C3P3B3IoutUCF" }, "IOUT_UC_FAULT_LIMIT");
                            this.C3P3B3PowerGoodOnPresenter.Content = LoadControl(new InputControl { Name = "C3P3B3PowerGoodOn" }, "POWER_GOOD_ON");
                            this.C3P3B3PowerGoodOffPresenter.Content = LoadControl(new InputControl { Name = "C3P3B3PowerGoodOff" }, "POWER_GOOD_OFF");
                            this.C3P3B3TonDelayPresenter.Content = LoadControl(new InputControl { Name = "C3P3B3TonDelay" }, "TON_DELAY");
                            this.C3P3B3TonRisePresenter.Content = LoadControl(new InputControl { Name = "C3P3B3TonRise" }, "TON_RISE");
                            this.C3P3B3ToffDelayPresenter.Content = LoadControl(new InputControl { Name = "C3P3B3ToffDelay" }, "TOFF_DELAY");
                            this.C3P3B3ToffFallPresenter.Content = LoadControl(new InputControl { Name = "C3P3B3ToffFall" }, "TOFF_FALL");
                            // Status
                            this.C3P3B3PowerStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C3P3B3PowerStatus", OnSetBackgroundColor = new SolidColorBrush(Color.FromRgb(0x75, 0xdf, 0x4a)), OnSetForegroundColor = new SolidColorBrush(Colors.Black) }, "STATUS_WORD|POWER_GOOD");
                            this.C3P3B3VoutStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C3P3B3VoutStatus" }, "STATUS_WORD|VOUT_STATUS");
                            this.C3P3B3VoutOvStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C3P3B3VoutOvStatus" }, "STATUS_VOUT|VOUT_OV_FAULT");
                            this.C3P3B3VoutUvStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C3P3B3VoutUvStatus" }, "STATUS_VOUT|VOUT_UV_FAULT");
                            this.C3P3B3IoutStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C3P3B3IoutStatus" }, "STATUS_WORD|IOUT_STATUS");
                            this.C3P3B3IoutOcStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C3P3B3IoutOcStatus" }, "STATUS_IOUT|IOUT_OC_FAULT");
                            this.C3P3B3IoutUcStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C3P3B3IoutUcStatus" }, "STATUS_IOUT|IOUT_UC_FAULT");

                            //Buck 4
                            Page_IOUT_Readings = LoadControl(new InputControl { Name = "C3P4B4Iout" }, "READ_IOUT");
                            break;
                        #endregion
                        case 4:
                            #region Configuration 4 (1,2,3/4)
                            // BUCK 1/2/3
                            this.C4P1B1VoutPresenter.Content = LoadControl(new InputControl { Name = "C4P1B1Vout" }, "READ_VOUT");
                            this.C4P1B1IoutPresenter.Content = LoadControl(new InputControl { Name = "C4P1B1Iout" }, "READ_IOUT");

                            this.C4P1B1OperationPresenter.Content = LoadControl(new ListControl { Name = "C4P1B1Operation" }, "OPERATION");
                            this.C4P1B1VoutCommandPresenter.Content = LoadControl(new InputControl { Name = "C4P1B1VoutCommand" }, "VOUT_COMMAND");
                            this.C4P1B1VoutMarginHighPresenter.Content = LoadControl(new InputControl { Name = "C4P1B1VoutMarginHigh" }, "VOUT_MARGIN_HIGH");
                            this.C4P1B1VoutMarginLowPresenter.Content = LoadControl(new InputControl { Name = "C4P1B1VoutMarginLow" }, "VOUT_MARGIN_LOW");
                            this.C4P1B1VoutTransRatePresenter.Content = LoadControl(new InputControl { Name = "C4P1B1VoutTransRate" }, "VOUT_TRANSITION_RATE");
                            this.C4P1B1VoutOVFLimitPresenter.Content = LoadControl(new InputControl { Name = "C4P1B1VoutOVFLimit" }, "VOUT_OV_FAULT_LIMIT");
                            this.C4P1B1VoutUVFLimitPresenter.Content = LoadControl(new InputControl { Name = "C4P1B1VoutUVFLimit" }, "VOUT_UV_FAULT_LIMIT");
                            this.C4P1B1IoutOCFLimitPresenter.Content = LoadControl(new InputControl { Name = "C4P1B1IoutOCFLimit" }, "IOUT_OC_FAULT_LIMIT");
                            this.C4P1B1IoutUCFPresenter.Content = LoadControl(new InputControl { Name = "C4P1B1IoutUCF" }, "IOUT_UC_FAULT_LIMIT");
                            this.C4P1B1PowerGoodOnPresenter.Content = LoadControl(new InputControl { Name = "C4P1B1PowerGoodOn" }, "POWER_GOOD_ON");
                            this.C4P1B1PowerGoodOffPresenter.Content = LoadControl(new InputControl { Name = "C4P1B1PowerGoodOff" }, "POWER_GOOD_OFF");
                            this.C4P1B1TonDelayPresenter.Content = LoadControl(new InputControl { Name = "C4P1B1TonDelay" }, "TON_DELAY");
                            this.C4P1B1TonRisePresenter.Content = LoadControl(new InputControl { Name = "C4P1B1TonRise" }, "TON_RISE");
                            this.C4P1B1ToffDelayPresenter.Content = LoadControl(new InputControl { Name = "C4P1B1ToffDelay" }, "TOFF_DELAY");
                            this.C4P1B1ToffFallPresenter.Content = LoadControl(new InputControl { Name = "C4P1B1ToffFall" }, "TOFF_FALL");
                            // Status
                            this.C4P1B1PowerStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C4P1B1PowerStatus", OnSetBackgroundColor = new SolidColorBrush(Color.FromRgb(0x75, 0xdf, 0x4a)), OnSetForegroundColor = new SolidColorBrush(Colors.Black) }, "STATUS_WORD|POWER_GOOD");
                            this.C4P1B1VoutStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C4P1B1VoutStatus" }, "STATUS_WORD|VOUT_STATUS");
                            this.C4P1B1VoutOvStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C4P1B1VoutOvStatus" }, "STATUS_VOUT|VOUT_OV_FAULT");
                            this.C4P1B1VoutUvStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C4P1B1VoutUvStatus" }, "STATUS_VOUT|VOUT_UV_FAULT");
                            this.C4P1B1IoutStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C4P1B1IoutStatus" }, "STATUS_WORD|IOUT_STATUS");
                            this.C4P1B1IoutOcStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C4P1B1IoutOcStatus" }, "STATUS_IOUT|IOUT_OC_FAULT");
                            this.C4P1B1IoutUcStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C4P1B1IoutUcStatus" }, "STATUS_IOUT|IOUT_UC_FAULT");

                            //BUCK 2
                            Page_IOUT_Readings = LoadControl(new InputControl { Name = "C4P2B2Iout" }, "READ_IOUT");

                            //BUCK 3
                            Page_IOUT_Readings = LoadControl(new InputControl { Name = "C4P3B3Iout" }, "READ_IOUT");

                            // BUCK 4
                            this.C4P4B4VoutPresenter.Content = LoadControl(new InputControl { Name = "C4P4B4Vout" }, "READ_VOUT");
                            this.C4P4B4IoutPresenter.Content = LoadControl(new InputControl { Name = "C4P4B4Iout" }, "READ_IOUT");

                            this.C4P4B4OperationPresenter.Content = LoadControl(new ListControl { Name = "C4P4B4Operation" }, "OPERATION");
                            this.C4P4B4VoutCommandPresenter.Content = LoadControl(new InputControl { Name = "C4P4B4VoutCommand" }, "VOUT_COMMAND");
                            this.C4P4B4VoutMarginHighPresenter.Content = LoadControl(new InputControl { Name = "C4P4B4VoutMarginHigh" }, "VOUT_MARGIN_HIGH");
                            this.C4P4B4VoutMarginLowPresenter.Content = LoadControl(new InputControl { Name = "C4P4B4VoutMarginLow" }, "VOUT_MARGIN_LOW");
                            this.C4P4B4VoutTransRatePresenter.Content = LoadControl(new InputControl { Name = "C4P4B4VoutTransRate" }, "VOUT_TRANSITION_RATE");
                            this.C4P4B4VoutOVFLimitPresenter.Content = LoadControl(new InputControl { Name = "C4P4B4VoutOVFLimit" }, "VOUT_OV_FAULT_LIMIT");
                            this.C4P4B4VoutUVFLimitPresenter.Content = LoadControl(new InputControl { Name = "C4P4B4VoutUVFLimit" }, "VOUT_UV_FAULT_LIMIT");
                            this.C4P4B4IoutOCFLimitPresenter.Content = LoadControl(new InputControl { Name = "C4P4B4IoutOCFLimit" }, "IOUT_OC_FAULT_LIMIT");
                            this.C4P4B4IoutUCFPresenter.Content = LoadControl(new InputControl { Name = "C4P4B4IoutUCF" }, "IOUT_UC_FAULT_LIMIT");
                            this.C4P4B4PowerGoodOnPresenter.Content = LoadControl(new InputControl { Name = "C4P4B4PowerGoodOn" }, "POWER_GOOD_ON");
                            this.C4P4B4PowerGoodOffPresenter.Content = LoadControl(new InputControl { Name = "C4P4B4PowerGoodOff" }, "POWER_GOOD_OFF");
                            this.C4P4B4TonDelayPresenter.Content = LoadControl(new InputControl { Name = "C4P4B4TonDelay" }, "TON_DELAY");
                            this.C4P4B4TonRisePresenter.Content = LoadControl(new InputControl { Name = "C4P4B4TonRise" }, "TON_RISE");
                            this.C4P4B4ToffDelayPresenter.Content = LoadControl(new InputControl { Name = "C4P4B4ToffDelay" }, "TOFF_DELAY");
                            this.C4P4B4ToffFallPresenter.Content = LoadControl(new InputControl { Name = "C4P4B4ToffFall" }, "TOFF_FALL");
                            // Status
                            this.C4P4B4PowerStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C4P4B4PowerStatus", OnSetBackgroundColor = new SolidColorBrush(Color.FromRgb(0x75, 0xdf, 0x4a)), OnSetForegroundColor = new SolidColorBrush(Colors.Black) }, "STATUS_WORD|POWER_GOOD");
                            this.C4P4B4VoutStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C4P4B4VoutStatus" }, "STATUS_WORD|VOUT_STATUS");
                            this.C4P4B4VoutOvStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C4P4B4VoutOvStatus" }, "STATUS_VOUT|VOUT_OV_FAULT");
                            this.C4P4B4VoutUvStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C4P4B4VoutUvStatus" }, "STATUS_VOUT|VOUT_UV_FAULT");
                            this.C4P4B4IoutStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C4P4B4IoutStatus" }, "STATUS_WORD|IOUT_STATUS");
                            this.C4P4B4IoutOcStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C4P4B4IoutOcStatus" }, "STATUS_IOUT|IOUT_OC_FAULT");
                            this.C4P4B4IoutUcStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C4P4B4IoutUcStatus" }, "STATUS_IOUT|IOUT_UC_FAULT");
                            break;
                        #endregion
                        case 5:
                            #region Configuration 5 (1,2,3,4)
                            // BUCK 1
                            this.C5P1B1VoutPresenter.Content = LoadControl(new InputControl { Name = "C5P1B1Vout" }, "READ_VOUT");
                            this.C5P1B1IoutPresenter.Content = LoadControl(new InputControl { Name = "C5P1B1Iout" }, "READ_IOUT");

                            this.C5P1B1OperationPresenter.Content = LoadControl(new ListControl { Name = "C5P1B1Operation" }, "OPERATION");
                            this.C5P1B1VoutCommandPresenter.Content = LoadControl(new InputControl { Name = "C5P1B1VoutCommand" }, "VOUT_COMMAND");
                            this.C5P1B1VoutMarginHighPresenter.Content = LoadControl(new InputControl { Name = "C5P1B1VoutMarginHigh" }, "VOUT_MARGIN_HIGH");
                            this.C5P1B1VoutMarginLowPresenter.Content = LoadControl(new InputControl { Name = "C5P1B1VoutMarginLow" }, "VOUT_MARGIN_LOW");
                            this.C5P1B1VoutTransRatePresenter.Content = LoadControl(new InputControl { Name = "C5P1B1VoutTransRate" }, "VOUT_TRANSITION_RATE");
                            this.C5P1B1VoutOVFLimitPresenter.Content = LoadControl(new InputControl { Name = "C5P1B1VoutOVFLimit" }, "VOUT_OV_FAULT_LIMIT");
                            this.C5P1B1VoutUVFLimitPresenter.Content = LoadControl(new InputControl { Name = "C5P1B1VoutUVFLimit" }, "VOUT_UV_FAULT_LIMIT");
                            this.C5P1B1IoutOCFLimitPresenter.Content = LoadControl(new InputControl { Name = "C5P1B1IoutOCFLimit" }, "IOUT_OC_FAULT_LIMIT");
                            this.C5P1B1IoutUCFPresenter.Content = LoadControl(new InputControl { Name = "C5P1B1IoutUCF" }, "IOUT_UC_FAULT_LIMIT");
                            this.C5P1B1PowerGoodOnPresenter.Content = LoadControl(new InputControl { Name = "C5P1B1PowerGoodOn" }, "POWER_GOOD_ON");
                            this.C5P1B1PowerGoodOffPresenter.Content = LoadControl(new InputControl { Name = "C5P1B1PowerGoodOff" }, "POWER_GOOD_OFF");
                            this.C5P1B1TonDelayPresenter.Content = LoadControl(new InputControl { Name = "C5P1B1TonDelay" }, "TON_DELAY");
                            this.C5P1B1TonRisePresenter.Content = LoadControl(new InputControl { Name = "C5P1B1TonRise" }, "TON_RISE");
                            this.C5P1B1ToffDelayPresenter.Content = LoadControl(new InputControl { Name = "C5P1B1ToffDelay" }, "TOFF_DELAY");
                            this.C5P1B1ToffFallPresenter.Content = LoadControl(new InputControl { Name = "C5P1B1ToffFall" }, "TOFF_FALL");
                            // Status
                            this.C5P1B1PowerStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C5P1B1PowerStatus", OnSetBackgroundColor = new SolidColorBrush(Color.FromRgb(0x75, 0xdf, 0x4a)), OnSetForegroundColor = new SolidColorBrush(Colors.Black) }, "STATUS_WORD|POWER_GOOD");
                            this.C5P1B1VoutStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C5P1B1VoutStatus" }, "STATUS_WORD|VOUT_STATUS");
                            this.C5P1B1VoutOvStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C5P1B1VoutOvStatus" }, "STATUS_VOUT|VOUT_OV_FAULT");
                            this.C5P1B1VoutUvStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C5P1B1VoutUvStatus" }, "STATUS_VOUT|VOUT_UV_FAULT");
                            this.C5P1B1IoutStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C5P1B1IoutStatus" }, "STATUS_WORD|IOUT_STATUS");
                            this.C5P1B1IoutOcStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C5P1B1IoutOcStatus" }, "STATUS_IOUT|IOUT_OC_FAULT");
                            this.C5P1B1IoutUcStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C5P1B1IoutUcStatus" }, "STATUS_IOUT|IOUT_UC_FAULT");

                            //BUCK 2
                            Page_IOUT_Readings = LoadControl(new InputControl { Name = "C5P2B2Iout" }, "READ_IOUT");

                            //BUCK 3
                            Page_IOUT_Readings = LoadControl(new InputControl { Name = "C5P3B3Iout" }, "READ_IOUT");

                            //BUCK 4
                            Page_IOUT_Readings = LoadControl(new InputControl { Name = "C5P4B4Iout" }, "READ_IOUT");
                            break;
                            #endregion
                    }
                }
            }
            else
            {
                switch (selectedConfig)
                {
                    case 1:
                        #region Configuration 1 (1/2)

                        // BUCK 1
                        this.T2C1P1B1VoutPresenter.Content = LoadControl(new InputControl { Name = "C1P1B1Vout" }, "READ_VOUT");
                        this.T2C1P1B1IoutPresenter.Content = LoadControl(new InputControl { Name = "C1P1B1Iout" }, "READ_IOUT");

                        this.T2C1P1B1OperationPresenter.Content = LoadControl(new ListControl { Name = "C1P1B1Operation" }, "OPERATION");
                        this.T2C1P1B1VoutCommandPresenter.Content = LoadControl(new InputControl { Name = "C1P1B1VoutCommand" }, "VOUT_COMMAND");
                        this.T2C1P1B1VoutMarginHighPresenter.Content = LoadControl(new InputControl { Name = "C1P1B1VoutMarginHigh" }, "VOUT_MARGIN_HIGH");
                        this.T2C1P1B1VoutMarginLowPresenter.Content = LoadControl(new InputControl { Name = "C1P1B1VoutMarginLow" }, "VOUT_MARGIN_LOW");
                        this.T2C1P1B1VoutTransRatePresenter.Content = LoadControl(new InputControl { Name = "C1P1B1VoutTransRate" }, "VOUT_TRANSITION_RATE");
                        this.T2C1P1B1VoutOVFLimitPresenter.Content = LoadControl(new InputControl { Name = "C1P1B1VoutOVFLimit" }, "VOUT_OV_FAULT_LIMIT");
                        this.T2C1P1B1VoutUVFLimitPresenter.Content = LoadControl(new InputControl { Name = "C1P1B1VoutUVFLimit" }, "VOUT_UV_FAULT_LIMIT");
                        this.T2C1P1B1IoutOCFLimitPresenter.Content = LoadControl(new InputControl { Name = "C1P1B1IoutOCFLimit" }, "IOUT_OC_FAULT_LIMIT");
                        this.T2C1P1B1IoutUCFPresenter.Content = LoadControl(new InputControl { Name = "C1P1B1IoutUCF" }, "IOUT_UC_FAULT_LIMIT");
                        this.T2C1P1B1PowerGoodOnPresenter.Content = LoadControl(new InputControl { Name = "C1P1B1PowerGoodOn" }, "POWER_GOOD_ON");
                        this.T2C1P1B1PowerGoodOffPresenter.Content = LoadControl(new InputControl { Name = "C1P1B1PowerGoodOff" }, "POWER_GOOD_OFF");
                        this.T2C1P1B1TonDelayPresenter.Content = LoadControl(new InputControl { Name = "C1P1B1TonDelay" }, "TON_DELAY");
                        this.T2C1P1B1TonRisePresenter.Content = LoadControl(new InputControl { Name = "C1P1B1TonRise" }, "TON_RISE");
                        this.T2C1P1B1ToffDelayPresenter.Content = LoadControl(new InputControl { Name = "C1P1B1ToffDelay" }, "TOFF_DELAY");
                        this.T2C1P1B1ToffFallPresenter.Content = LoadControl(new InputControl { Name = "C1P1B1ToffFall" }, "TOFF_FALL");
                        // Status
                        this.T2C1P1B1PowerStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P1B1PowerStatus", OnSetBackgroundColor = new SolidColorBrush(Color.FromRgb(0x75, 0xdf, 0x4a)), OnSetForegroundColor = new SolidColorBrush(Colors.Black) }, "STATUS_WORD|POWER_GOOD");
                        this.T2C1P1B1VoutStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P1B1VoutStatus" }, "STATUS_WORD|VOUT_STATUS");
                        this.T2C1P1B1VoutOvStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P1B1VoutOvStatus" }, "STATUS_VOUT|VOUT_OV_FAULT");
                        this.T2C1P1B1VoutUvStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P1B1VoutUvStatus" }, "STATUS_VOUT|VOUT_UV_FAULT");
                        this.T2C1P1B1IoutStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P1B1IoutStatus" }, "STATUS_WORD|IOUT_STATUS");
                        this.T2C1P1B1IoutOcStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P1B1IoutOcStatus" }, "STATUS_IOUT|IOUT_OC_FAULT");
                        this.T2C1P1B1IoutUcStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P1B1IoutUcStatus" }, "STATUS_IOUT|IOUT_UC_FAULT");

                        // BUCK 2
                        this.T2C1P2B2VoutPresenter.Content = LoadControl(new InputControl { Name = "C1P2B2Vout" }, "READ_VOUT");
                        this.T2C1P2B2IoutPresenter.Content = LoadControl(new InputControl { Name = "C1P2B2Iout" }, "READ_IOUT");

                        this.T2C1P2B2OperationPresenter.Content = LoadControl(new ListControl { Name = "C1P2B2Operation" }, "OPERATION");
                        this.T2C1P2B2VoutCommandPresenter.Content = LoadControl(new InputControl { Name = "C1P2B2VoutCommand" }, "VOUT_COMMAND");
                        this.T2C1P2B2VoutMarginHighPresenter.Content = LoadControl(new InputControl { Name = "C1P2B2VoutMarginHigh" }, "VOUT_MARGIN_HIGH");
                        this.T2C1P2B2VoutMarginLowPresenter.Content = LoadControl(new InputControl { Name = "C1P2B2VoutMarginLow" }, "VOUT_MARGIN_LOW");
                        this.T2C1P2B2VoutTransRatePresenter.Content = LoadControl(new InputControl { Name = "C1P2B2VoutTransRate" }, "VOUT_TRANSITION_RATE");
                        this.T2C1P2B2VoutOVFLimitPresenter.Content = LoadControl(new InputControl { Name = "C1P2B2VoutOVFLimit" }, "VOUT_OV_FAULT_LIMIT");
                        this.T2C1P2B2VoutUVFLimitPresenter.Content = LoadControl(new InputControl { Name = "C1P2B2VoutUVFLimit" }, "VOUT_UV_FAULT_LIMIT");
                        this.T2C1P2B2IoutOCFLimitPresenter.Content = LoadControl(new InputControl { Name = "C1P2B2IoutOCFLimit" }, "IOUT_OC_FAULT_LIMIT");
                        this.T2C1P2B2IoutUCFPresenter.Content = LoadControl(new InputControl { Name = "C1P2B2IoutUCF" }, "IOUT_UC_FAULT_LIMIT");
                        this.T2C1P2B2PowerGoodOnPresenter.Content = LoadControl(new InputControl { Name = "C1P2B2PowerGoodOn" }, "POWER_GOOD_ON");
                        this.T2C1P2B2PowerGoodOffPresenter.Content = LoadControl(new InputControl { Name = "C1P2B2PowerGoodOff" }, "POWER_GOOD_OFF");
                        this.T2C1P2B2TonDelayPresenter.Content = LoadControl(new InputControl { Name = "C1P2B2TonDelay" }, "TON_DELAY");
                        this.T2C1P2B2TonRisePresenter.Content = LoadControl(new InputControl { Name = "C1P2B2TonRise" }, "TON_RISE");
                        this.T2C1P2B2ToffDelayPresenter.Content = LoadControl(new InputControl { Name = "C1P2B2ToffDelay" }, "TOFF_DELAY");
                        this.T2C1P2B2ToffFallPresenter.Content = LoadControl(new InputControl { Name = "C1P2B2ToffFall" }, "TOFF_FALL");
                        // Status
                        this.T2C1P2B2PowerStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P2B2PowerStatus", OnSetBackgroundColor = new SolidColorBrush(Color.FromRgb(0x75, 0xdf, 0x4a)), OnSetForegroundColor = new SolidColorBrush(Colors.Black) }, "STATUS_WORD|POWER_GOOD");
                        this.T2C1P2B2VoutStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P2B2VoutStatus" }, "STATUS_WORD|VOUT_STATUS");
                        this.T2C1P2B2VoutOvStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P2B2VoutOvStatus" }, "STATUS_VOUT|VOUT_OV_FAULT");
                        this.T2C1P2B2VoutUvStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P2B2VoutUvStatus" }, "STATUS_VOUT|VOUT_UV_FAULT");
                        this.T2C1P2B2IoutStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P2B2IoutStatus" }, "STATUS_WORD|IOUT_STATUS");
                        this.T2C1P2B2IoutOcStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P2B2IoutOcStatus" }, "STATUS_IOUT|IOUT_OC_FAULT");
                        this.T2C1P2B2IoutUcStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C1P2B2IoutUcStatus" }, "STATUS_IOUT|IOUT_UC_FAULT");
                        break;

                    #endregion
                    case 2:
                        #region Configuration 2 (1/2)

                        // BUCK 1/2
                        this.T2C2P1B1VoutPresenter.Content = LoadControl(new InputControl { Name = "C2P1B1Vout" }, "READ_VOUT");
                        this.T2C2P1B1IoutPresenter.Content = LoadControl(new InputControl { Name = "C2P1B1Iout" }, "READ_IOUT");

                        this.T2C2P1B1OperationPresenter.Content = LoadControl(new ListControl { Name = "C2P1B1Operation" }, "OPERATION");
                        this.T2C2P1B1VoutCommandPresenter.Content = LoadControl(new InputControl { Name = "C2P1B1VoutCommand" }, "VOUT_COMMAND");
                        this.T2C2P1B1VoutMarginHighPresenter.Content = LoadControl(new InputControl { Name = "C2P1B1VoutMarginHigh" }, "VOUT_MARGIN_HIGH");
                        this.T2C2P1B1VoutMarginLowPresenter.Content = LoadControl(new InputControl { Name = "C2P1B1VoutMarginLow" }, "VOUT_MARGIN_LOW");
                        this.T2C2P1B1VoutTransRatePresenter.Content = LoadControl(new InputControl { Name = "C2P1B1VoutTransRate" }, "VOUT_TRANSITION_RATE");
                        this.T2C2P1B1VoutOVFLimitPresenter.Content = LoadControl(new InputControl { Name = "C2P1B1VoutOVFLimit" }, "VOUT_OV_FAULT_LIMIT");
                        this.T2C2P1B1VoutUVFLimitPresenter.Content = LoadControl(new InputControl { Name = "C2P1B1VoutUVFLimit" }, "VOUT_UV_FAULT_LIMIT");
                        this.T2C2P1B1IoutOCFLimitPresenter.Content = LoadControl(new InputControl { Name = "C2P1B1IoutOCFLimit" }, "IOUT_OC_FAULT_LIMIT");
                        this.T2C2P1B1IoutUCFPresenter.Content = LoadControl(new InputControl { Name = "C2P1B1IoutUCF" }, "IOUT_UC_FAULT_LIMIT");
                        this.T2C2P1B1PowerGoodOnPresenter.Content = LoadControl(new InputControl { Name = "C2P1B1PowerGoodOn" }, "POWER_GOOD_ON");
                        this.T2C2P1B1PowerGoodOffPresenter.Content = LoadControl(new InputControl { Name = "C2P1B1PowerGoodOff" }, "POWER_GOOD_OFF");
                        this.T2C2P1B1TonDelayPresenter.Content = LoadControl(new InputControl { Name = "C2P1B1TonDelay" }, "TON_DELAY");
                        this.T2C2P1B1TonRisePresenter.Content = LoadControl(new InputControl { Name = "C2P1B1TonRise" }, "TON_RISE");
                        this.T2C2P1B1ToffDelayPresenter.Content = LoadControl(new InputControl { Name = "C2P1B1ToffDelay" }, "TOFF_DELAY");
                        this.T2C2P1B1ToffFallPresenter.Content = LoadControl(new InputControl { Name = "C2P1B1ToffFall" }, "TOFF_FALL");
                        // Status
                        this.T2C2P1B1PowerStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C2P1B1PowerStatus", OnSetBackgroundColor = new SolidColorBrush(Color.FromRgb(0x75, 0xdf, 0x4a)), OnSetForegroundColor = new SolidColorBrush(Colors.Black) }, "STATUS_WORD|POWER_GOOD");
                        this.T2C2P1B1VoutStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C2P1B1VoutStatus" }, "STATUS_WORD|VOUT_STATUS");
                        this.T2C2P1B1VoutOvStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C2P1B1VoutOvStatus" }, "STATUS_VOUT|VOUT_OV_FAULT");
                        this.T2C2P1B1VoutUvStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C2P1B1VoutUvStatus" }, "STATUS_VOUT|VOUT_UV_FAULT");
                        this.T2C2P1B1IoutStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C2P1B1IoutStatus" }, "STATUS_WORD|IOUT_STATUS");
                        this.T2C2P1B1IoutOcStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C2P1B1IoutOcStatus" }, "STATUS_IOUT|IOUT_OC_FAULT");
                        this.T2C2P1B1IoutUcStatusPresenter.Content = LoadControl(new BitStatusLabel { Name = "C2P1B1IoutUcStatus" }, "STATUS_IOUT|IOUT_UC_FAULT");

                        //BUCK 2
                        Page_IOUT_Readings = LoadControl(new InputControl { Name = "C2P2B2Iout" }, "READ_IOUT");
                        break;

                        #endregion
                }
            }
        }

        public IMappedControl LoadControl(FrameworkElement control, string regName)
        {
            string name = control.Name;
            string[] rname = regName.Split('|');

            Register reg = _device.Registers.FirstOrDefault(r => r.DisplayName == rname[0]);
            IRegister ireg = _device as IRegister;

            if (reg == null)
            {
                var ic = control as InputControl;
                return ic;
            }

            MappedRegister mr = new MappedRegister
            {
                Config = int.Parse(name.Substring(1, 1)),
                Page = int.Parse(name.Substring(3, 1)),
                RegisterSource = reg,
                Units = reg.Unit,
                DeviceSource = _device,
                IsReadOnly = reg.ReadOnly
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

                var unit = new Binding();
                unit.Source = mr;
                unit.Path = new PropertyPath("Units");
                ic.SetBinding(InputControl.UnitsProperty, unit);
                ic.Reg = mr;
                ic.ValidCharacters = GetValidCharacters(mr);
                ic.Write += control_Write;
                var isReadOnly = new Binding();
                int pageCount = _pvm.Pages.Count;
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

                var set = new Binding();
                set.Source = mr;
                set.Path = new PropertyPath("BitValue");
                bitStatus.SetBinding(BitStatusLabel.IsSetProperty, set);

                if (_pvm.IsR2D2 && bitStatusObject.Label == "POWER_GOOD")
                {
                    bitStatus.IsSet = true;

                    if (_pvm.IsIndicatorSet != null && _pvm.IsIndicatorSet.ContainsKey(mr.Page))
                    {
                        bool isSet = false;
                        _pvm?.IsIndicatorSet.TryGetValue(mr.Page, out isSet);
                        if (isSet)
                        {
                            bitStatus.Background = new SolidColorBrush(Color.FromRgb(189, 0, 0));
                            bitStatus.OnSetForegroundColor = Brushes.White;
                        }
                    }
                }

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

        private void UpdateIoutValue(IRegister ireg, InputControl ic, int pageCount)
        {
            if (_pvm.IsR2D2)
            {
                if (_isPE24103 && !_isMYT0424)//PE24103-R01 device Output result of IOUT Readings
                {
                    for (int _pageindex = 0; _pageindex < pageCount; _pageindex++)
                    {
                        // Iterates through Registers, Page wise. 
                        for (int i = 0; i < ireg.Registers.Count; i++)
                        {
                            if (_pvm.IsR2D2 && ireg.Registers[i].Name == "READ_IOUT")
                            {
                                if (_pvm.SelectedConfig == 2)
                                {
                                    // if Selected Config is 2, Page => 2 , IOUT Reading get updates in GUI
                                    if (_pvm.Pages[_pageindex] == 2)
                                    {
                                        //ic.Reg.Value = _pvm.IOUTRegisters[1].RegisterValue + _pvm.IOUTRegisters[2].RegisterValue;
                                        ic.Reg.Value = _pvm.IOUTRegisters[_pageindex].RegisterValue;
                                        break;
                                    }

                                }
                                else if (_pvm.SelectedConfig == 3)
                                {
                                    // if Selected Config is 3, 
                                    if (_pvm.Pages[_pageindex] == 1 && ic.Reg.Page == 1)
                                    {
                                        //Page => 1 , IOUT Reading get updates in GUI
                                        ic.Reg.Value = _pvm.IOUTRegisters[_pageindex].RegisterValue;
                                        break;
                                    }
                                    else if (_pvm.Pages[_pageindex] == 3 && ic.Reg.Page == 3)
                                    {
                                        //Page => 3 , IOUT Reading get updates in GUI
                                        ic.Reg.Value = _pvm.IOUTRegisters[_pageindex].RegisterValue;
                                        break;
                                    }
                                }
                                else if (_pvm.SelectedConfig == 4)
                                {
                                    // if Selected Config is 4, Page => 1 , IOUT Reading get updates in GUI
                                    if (_pvm.Pages[_pageindex] == 1)
                                    {
                                        ic.Reg.Value = _pvm.IOUTRegisters[_pageindex].RegisterValue;
                                        break;
                                    }

                                }
                                else if (_pvm.SelectedConfig == 5)
                                {
                                    // if Selected Config is 5, Page => 1 , IOUT Reading get updates in GUI
                                    if (_pvm.Pages[_pageindex] == 1)
                                    {
                                        _pvm.IOUTRegisters[_pageindex].RegisterValue = _pvm.IOUTRegisters[_pageindex].RegisterValue;
                                        ic.Reg.Value = _pvm.IOUTRegisters[_pageindex].RegisterValue;
                                        break;
                                    }
                                }
                            }

                            continue;
                        }

                    }
                }
            }
            else //PE24104-R01 device Output result of IOUT Readings
            {
                for (int _pageindex = 0; _pageindex < pageCount; _pageindex++)
                {
                    // Iterates through Registers, Page wise.
                    for (int i = 0; i < ireg.Registers.Count; i++)
                    {
                        if (!_pvm.IsR2D2 && ireg.Registers[i].Name == "READ_IOUT")
                        {
                            if (_pvm.SelectedConfig == 2)
                            {
                                // if Selected Config is 2, Page => 1 , IOUT Reading get updates in GUI
                                if (_pvm.Pages[_pageindex] == 1)
                                {
                                    ic.Reg.Value = _pvm.IOUTRegisters[_pageindex].RegisterValue;
                                    break;
                                }
                            }
                        }
                        continue;
                    }
                }
            }
        }

        public IMappedControl UpdateIoutLabel(FrameworkElement control, string regName, int page)
        {
            string name = control.Name;
            string[] rname = regName.Split('|');

            Register reg = _device.Registers.FirstOrDefault(r => r.DisplayName == rname[0]);
            IRegister ireg = _device as IRegister;

            MappedRegister mr = new MappedRegister
            {
                Config = int.Parse(name.Substring(1, 1)),
                Page = int.Parse(name.Substring(3, 1)),
                RegisterSource = reg,
                Units = reg.Unit,
                DeviceSource = _device,
                IsReadOnly = reg.ReadOnly
            };

            var ic = control as InputControl;
            var val = new Binding();
            val.Source = mr;
            val.Path = new PropertyPath("Value");
            ic.SetBinding(InputControl.DisplayProperty, val);

            var unit = new Binding();
            unit.Source = mr;
            unit.Path = new PropertyPath("Units");
            ic.SetBinding(InputControl.UnitsProperty, unit);
            ic.Reg = mr;
            int pageCount = _pvm.Pages.Count;
            UpdateIoutValue(ireg, ic, pageCount);
            ic.ValidCharacters = GetValidCharacters(mr);
            ic.Write += control_Write;

            var isReadOnly = new Binding();
            isReadOnly.Source = mr;
            isReadOnly.Path = new PropertyPath("IsReadOnly");
            ic.SetBinding(InputControl.IsReadOnlyProperty, isReadOnly);

            return ic;
        }

        public IMappedControl UpdatePowerGoodBitStatusLabel(FrameworkElement control, string regName, int page)
        {
            string name = control.Name;
            string[] rname = regName.Split('|');

            Register reg = _device.Registers.FirstOrDefault(r => r.DisplayName == rname[0]);
            IRegister ireg = _device as IRegister;

            MappedRegister mr = new MappedRegister
            {
                Config = int.Parse(name.Substring(1, 1)),
                Page = int.Parse(name.Substring(3, 1)),
                RegisterSource = reg,
                Units = reg.Unit,
                DeviceSource = _device,
                IsReadOnly = reg.ReadOnly
            };

            var bitStatusObject = new BitStatusObject { Label = rname[1], Map = regName, Description = "" };
            var bitStatus = control as BitStatusLabel;
            if (ireg != null)
            {
                bitStatus.RegisterSource = ireg.GetRegister(regName.Split('|')[0].Replace("_", ""));
                mr.Bit = ireg.GetRegisterBit(regName.Split('|')[0].Replace("_", "") + "_" + regName.Split('|')[1].Replace("_", ""));
            }
            bitStatus.Tag = bitStatusObject;
            bitStatus.Content = regName.Split('|')[1];

            var set = new Binding();
            set.Source = mr;
            set.Path = new PropertyPath("BitValue");
            bitStatus.SetBinding(BitStatusLabel.IsSetProperty, set);

            if (_pvm.IsR2D2 && bitStatusObject.Label == "POWER_GOOD")
            {
                bitStatus.IsSet = true;

                if (_pvm.IsIndicatorSet != null && _pvm.IsIndicatorSet.ContainsKey(page))
                {
                    bool isSet = false;
                    _pvm.IsIndicatorSet.TryGetValue(page, out isSet);
                    if (isSet)
                    {
                        bitStatus.Background = new SolidColorBrush(Color.FromRgb(189, 0, 0));
                        bitStatus.OnSetForegroundColor = Brushes.White;
                    }
                }
            }

            bitStatus.HorizontalAlignment = HorizontalAlignment.Stretch;
            bitStatus.Reg = mr;
            return bitStatus;
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

            if (_pvm == null)
            {
                return;
            }

            _pvm.Write(reg, value, _pvm, ireg);
            FetchIOUTReadings(reg);

            if (reg.RegisterSource.DisplayName == "READ_VOUT" || reg.RegisterSource.DisplayName == "POWER_GOOD_ON" || reg.RegisterSource.DisplayName == "POWER_GOOD_OFF")
            {
                PowerIndicatorRegisterInfo pReg = _pvm.PowerIndicatorRegistersList.Where(a => (a.RegisterName == reg.RegisterSource.DisplayName && a.Page == reg.Page)).FirstOrDefault();

                if (pReg == null)
                {
                    _pvm.PowerIndicatorRegistersList.Add(new PowerIndicatorRegisterInfo
                    {
                        Page = reg.Page,
                        RegisterName = reg.RegisterSource.DisplayName,
                        RegisterValue = Convert.ToDouble(reg.RegisterSource.LastReadString)
                    });
                }
                else
                {
                    _pvm.PowerIndicatorRegistersList.FirstOrDefault(a => a.RegisterName == reg.RegisterSource.DisplayName && a.Page == reg.Page).RegisterValue = Convert.ToDouble(reg.RegisterSource.LastReadString);
                }

                _pvm.UpdatePowerGoodIndicatorRegs(_pvm.Page);

                CheckPowerGoodFaultIndicatorStatus();

            }
        }

        private void FetchIOUTReadings(MappedRegister reg)
        {
            if (reg.RegisterSource.DisplayName == "READ_IOUT")
            {
                IOUTRegisterInfo iReg = _pvm.IOUTRegisters.Where(a => (a.RegisterName == reg.RegisterSource.DisplayName && a.Page == reg.Page)).FirstOrDefault();
                if (iReg == null)
                {
                    _pvm.IOUTRegisters.Add(new IOUTRegisterInfo
                    {
                        Page = reg.Page,
                        RegisterName = reg.RegisterSource.DisplayName,
                        RegisterValue = Convert.ToDouble(reg.RegisterSource.LastReadValue)
                    });
                }
                else
                {
                    _pvm.IOUTRegisters.FirstOrDefault(a => a.RegisterName == reg.RegisterSource.DisplayName && a.Page == reg.Page).RegisterValue = Convert.ToDouble(reg.RegisterSource.LastReadString);
                }
                CheckIoutReadings();
            }
        }

        private void CheckPowerGoodFaultIndicatorStatus()
        {
            Application.Current.Dispatcher.Invoke((Action)delegate {

                if (_pvm.IsR2D2)
                {
                    if (_isPE24103 && !_isMYT0424)
                    {
                        if (_pvm.SelectedConfig == 1)
                        {
                            this.C1P1B1PowerStatusPresenter.Content = UpdatePowerGoodBitStatusLabel(new BitStatusLabel { Name = "C1P1B1PowerStatus", OnSetBackgroundColor = new SolidColorBrush(Color.FromRgb(0x75, 0xdf, 0x4a)), OnSetForegroundColor = new SolidColorBrush(Colors.Black) }, "STATUS_WORD|POWER_GOOD", 1);
                            this.C1P2B2PowerStatusPresenter.Content = UpdatePowerGoodBitStatusLabel(new BitStatusLabel { Name = "C1P2B2PowerStatus", OnSetBackgroundColor = new SolidColorBrush(Color.FromRgb(0x75, 0xdf, 0x4a)), OnSetForegroundColor = new SolidColorBrush(Colors.Black) }, "STATUS_WORD|POWER_GOOD", 2);
                            this.C1P3B3PowerStatusPresenter.Content = UpdatePowerGoodBitStatusLabel(new BitStatusLabel { Name = "C1P3B3PowerStatus", OnSetBackgroundColor = new SolidColorBrush(Color.FromRgb(0x75, 0xdf, 0x4a)), OnSetForegroundColor = new SolidColorBrush(Colors.Black) }, "STATUS_WORD|POWER_GOOD", 3);
                            this.C1P4B4PowerStatusPresenter.Content = UpdatePowerGoodBitStatusLabel(new BitStatusLabel { Name = "C1P4B4PowerStatus", OnSetBackgroundColor = new SolidColorBrush(Color.FromRgb(0x75, 0xdf, 0x4a)), OnSetForegroundColor = new SolidColorBrush(Colors.Black) }, "STATUS_WORD|POWER_GOOD", 4);
                        }
                        else if (_pvm.SelectedConfig == 2)
                        {
                            this.C2P1B1PowerStatusPresenter.Content = UpdatePowerGoodBitStatusLabel(new BitStatusLabel { Name = "C2P1B1PowerStatus", OnSetBackgroundColor = new SolidColorBrush(Color.FromRgb(0x75, 0xdf, 0x4a)), OnSetForegroundColor = new SolidColorBrush(Colors.Black) }, "STATUS_WORD|POWER_GOOD", 1);
                            this.C2P2B2PowerStatusPresenter.Content = UpdatePowerGoodBitStatusLabel(new BitStatusLabel { Name = "C2P2B2PowerStatus", OnSetBackgroundColor = new SolidColorBrush(Color.FromRgb(0x75, 0xdf, 0x4a)), OnSetForegroundColor = new SolidColorBrush(Colors.Black) }, "STATUS_WORD|POWER_GOOD", 2);
                            this.C2P4B4PowerStatusPresenter.Content = UpdatePowerGoodBitStatusLabel(new BitStatusLabel { Name = "C2P4B4PowerStatus", OnSetBackgroundColor = new SolidColorBrush(Color.FromRgb(0x75, 0xdf, 0x4a)), OnSetForegroundColor = new SolidColorBrush(Colors.Black) }, "STATUS_WORD|POWER_GOOD", 4);
                        }
                        else if (_pvm.SelectedConfig == 3)
                        {
                            this.C3P1B1PowerStatusPresenter.Content = UpdatePowerGoodBitStatusLabel(new BitStatusLabel { Name = "C3P1B1PowerStatus", OnSetBackgroundColor = new SolidColorBrush(Color.FromRgb(0x75, 0xdf, 0x4a)), OnSetForegroundColor = new SolidColorBrush(Colors.Black) }, "STATUS_WORD|POWER_GOOD", 1);
                            this.C3P3B3PowerStatusPresenter.Content = UpdatePowerGoodBitStatusLabel(new BitStatusLabel { Name = "C3P3B3PowerStatus", OnSetBackgroundColor = new SolidColorBrush(Color.FromRgb(0x75, 0xdf, 0x4a)), OnSetForegroundColor = new SolidColorBrush(Colors.Black) }, "STATUS_WORD|POWER_GOOD", 3);
                        }
                        else if (_pvm.SelectedConfig == 4)
                        {
                            this.C4P1B1PowerStatusPresenter.Content = UpdatePowerGoodBitStatusLabel(new BitStatusLabel { Name = "C4P1B1PowerStatus", OnSetBackgroundColor = new SolidColorBrush(Color.FromRgb(0x75, 0xdf, 0x4a)), OnSetForegroundColor = new SolidColorBrush(Colors.Black) }, "STATUS_WORD|POWER_GOOD", 1);
                            this.C4P4B4PowerStatusPresenter.Content = UpdatePowerGoodBitStatusLabel(new BitStatusLabel { Name = "C4P4B4PowerStatus", OnSetBackgroundColor = new SolidColorBrush(Color.FromRgb(0x75, 0xdf, 0x4a)), OnSetForegroundColor = new SolidColorBrush(Colors.Black) }, "STATUS_WORD|POWER_GOOD", 4);
                        }
                        else if (_pvm.SelectedConfig == 5)
                        {
                            this.C5P1B1PowerStatusPresenter.Content = UpdatePowerGoodBitStatusLabel(new BitStatusLabel { Name = "C5P1B1PowerStatus", OnSetBackgroundColor = new SolidColorBrush(Color.FromRgb(0x75, 0xdf, 0x4a)), OnSetForegroundColor = new SolidColorBrush(Colors.Black) }, "STATUS_WORD|POWER_GOOD", 1);
                        }
                    }
                }
                else
                {
                    if (_pvm.SelectedConfig == 1)
                    {
                        this.T2C1P1B1PowerStatusPresenter.Content = UpdatePowerGoodBitStatusLabel(new BitStatusLabel { Name = "C1P1B1PowerStatus", OnSetBackgroundColor = new SolidColorBrush(Color.FromRgb(0x75, 0xdf, 0x4a)), OnSetForegroundColor = new SolidColorBrush(Colors.Black) }, "STATUS_WORD|POWER_GOOD", 1);
                        this.T2C1P2B2PowerStatusPresenter.Content = UpdatePowerGoodBitStatusLabel(new BitStatusLabel { Name = "C1P2B2PowerStatus", OnSetBackgroundColor = new SolidColorBrush(Color.FromRgb(0x75, 0xdf, 0x4a)), OnSetForegroundColor = new SolidColorBrush(Colors.Black) }, "STATUS_WORD|POWER_GOOD", 2);
                    }
                    else if (_pvm.SelectedConfig == 2)
                    {
                        this.T2C2P1B1PowerStatusPresenter.Content = UpdatePowerGoodBitStatusLabel(new BitStatusLabel { Name = "C2P1B1PowerStatus", OnSetBackgroundColor = new SolidColorBrush(Color.FromRgb(0x75, 0xdf, 0x4a)), OnSetForegroundColor = new SolidColorBrush(Colors.Black) }, "STATUS_WORD|POWER_GOOD", 1);
                    }
                }
            });
        }

        private void CheckIoutReadings()
        {
            Application.Current.Dispatcher.Invoke((Action)delegate {

                if (_pvm.IsR2D2)
                {
                    if (_isPE24103 && !_isMYT0424)
                    {
                        if (_pvm.SelectedConfig == 2)
                        {
                            this.C2P2B2IoutPresenter.Content = UpdateIoutLabel(new InputControl { Name = "C2P2B2Iout" }, "READ_IOUT", 2);
                        }
                        else if (_pvm.SelectedConfig == 3)
                        {
                            this.C3P1B1IoutPresenter.Content = UpdateIoutLabel(new InputControl { Name = "C3P1B1Iout" }, "READ_IOUT", 1);
                            this.C3P3B3IoutPresenter.Content = UpdateIoutLabel(new InputControl { Name = "C3P3B3Iout" }, "READ_IOUT", 3);
                        }
                        else if (_pvm.SelectedConfig == 4)
                        {
                            this.C4P1B1IoutPresenter.Content = UpdateIoutLabel(new InputControl { Name = "C4P1B1Iout" }, "READ_IOUT", 1);
                        }
                        else if (_pvm.SelectedConfig == 5)
                        {
                            this.C5P1B1IoutPresenter.Content = UpdateIoutLabel(new InputControl { Name = "C5P1B1Iout" }, "READ_IOUT", 1);
                        }
                    }
                }
                else
                {
                    if (_pvm.SelectedConfig == 2)
                    {
                        this.T2C2P1B1IoutPresenter.Content = UpdateIoutLabel(new InputControl { Name = "C2P1B1Iout" }, "READ_IOUT", 1);
                    }
                }
            });
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

        #endregion
    }

    public interface IUIObjects
    {
        string Label { get; set; }
        string Map { get; set; }
        string Description { get; set; }
    }

    public class ListObject : IUIObjects
    {
        public string Mask { get; set; }

        public string Description { get; set; }

        public string Label { get; set; }

        public string Map { get; set; }
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
