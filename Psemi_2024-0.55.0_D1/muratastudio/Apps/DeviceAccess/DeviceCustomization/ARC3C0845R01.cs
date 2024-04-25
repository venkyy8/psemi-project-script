using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Xml;
using System.Xml.Linq;

namespace DeviceAccess
{
    internal class ARC3C0845R01 : CustomizeBase
    {

        private const byte ACCESS_REG = 0x41;
        private const byte UNLOCK = 0x37;
        private const byte LOCK = 0xFF;
        private const byte FILTER_SETTINGS = 0x09;
        private const byte DITHER_ENABLE = 0x20;
        private const byte WLEDISETLSB = 0x05;
        private const byte WLEDISETMSB = 0x06;

        public ARC3C0845R01(Device device)
            : base(device)
        {
        }

        public ARC3C0845R01(Device device, bool isDemo)
            : base(device, isDemo)
        {
        }

        #region Base Overrides

        public override void CustomizeDevice(bool isDevMode = false)
        {
        }

        public override void CheckDevice(object criteria)
        {
            if (criteria != null)
            {
                if (criteria is List<FrameworkElement>)
                {
                    // Check to see if the device has dithering enabled. If so call modify device
                    var controls = criteria as List<FrameworkElement>;
                    var reg = Device.Registers.First(r => r.Address == FILTER_SETTINGS);
                    Device.ReadRegister(reg);
                    ModifyDeviceConfig("Dither Control", reg, controls);
                }
            }
        }

        public override void Unlock()
        {
            // Unlock master device
            this.Device.WriteByte(ACCESS_REG, UNLOCK);
        }

        public override void ModifyDeviceConfig(string controlName, HardwareInterfaces.Register reg, List<FrameworkElement> controls)
        {
            // Check for dither enabled
            if (controlName == "Dither Control")
            {
                // Write the MSB and re-read the LSB
                var regMsb = Device.Registers.First(r => r.Address == WLEDISETMSB);
                //Device.WriteRegister(regMsb, regMsb.LastReadValue);
                //Thread.Sleep(200);

                var stackPanel = controls.FirstOrDefault(c => c.Name == "LEDIntStackPanel") as StackPanel;
                Slider uiControl = null;
                TextBlock txtLEDBrightness = null;
                TextBox txtLEDIntensity = null;

                foreach (FrameworkElement panel in stackPanel.Children)
                {
                    if (panel is DockPanel)
                    {
                        var dp = panel as DockPanel;
                        foreach (var control in dp.Children)
                        {
                            if (control is Slider)
                                uiControl = control as Slider;
                            if (control is TextBlock)
                                txtLEDBrightness = control as TextBlock;
                            if (control is TextBox)
                                txtLEDIntensity = control as TextBox;
                        }
                    }
                }

                Device.ReadRegister(reg);

                //Fix for value shift to another while entering in slidebar textbox due to mask value updation failure
                //if (((int)reg.LastReadValue & (int)DITHER_ENABLE) == (int)DITHER_ENABLE)
                //{
                //    uiControl.Tag = "0xFE|0xFF";
                //}
                //else
                //{
                //    uiControl.Tag = "0xF0|0xFF";
                //}

                if (((int)reg.LastReadValue & (int)DITHER_ENABLE) == (int)DITHER_ENABLE)
                {
                    uiControl.Tag = "0xFE|0xFF";
                    object tag = "0xFE|0xFF";
                    txtLEDBrightness.Tag = tag.ToString().Split('|');
                    txtLEDIntensity.Tag = tag.ToString().Split('|');
                }
                else
                {
                    uiControl.Tag = "0xF0|0xFF";
                    object tag = "0xF0|0xFF";
                    txtLEDBrightness.Tag = tag.ToString().Split('|');
                    txtLEDIntensity.Tag = tag.ToString().Split('|');
                }

                // Reconfigure slider
                var masks = uiControl.Tag.ToString().Split('|');

                int lsb = ConvertHexToInt(masks[0]);
                int msb = ConvertHexToInt(masks[1]);

                int numberOfBits = CountSetBits(lsb);
                numberOfBits += CountSetBits(msb);
                int sliderMax = (int)Math.Pow(2d, numberOfBits);
                double sliderTick = (double)sliderMax / 256;

                uiControl.Maximum = sliderMax - 1;
                uiControl.TickFrequency = sliderTick;
                uiControl.Tag = masks;

                var binding = txtLEDBrightness.GetBindingExpression(TextBlock.TextProperty).ParentBinding;
                var newBinding = new Binding()
                {
                    Source = binding.Source,
                    Path = new PropertyPath("Text"),
                    Converter = binding.Converter,
                    ConverterParameter = sliderMax - 1,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };

                txtLEDBrightness.SetBinding(TextBlock.TextProperty, newBinding);

                var regLsb = Device.Registers.First(r => r.Address == WLEDISETLSB);

                var multiBinding = BindingOperations.GetMultiBindingExpression(uiControl, Slider.ValueProperty);
                var multi = new MultiBinding();
                multi.Converter = multiBinding.ParentMultiBinding.Converter;
                multi.ConverterParameter = masks;
                multi.UpdateSourceTrigger = UpdateSourceTrigger.Explicit;
                multi.Bindings.Add(new Binding()
                {
                    Source = regLsb,
                    Path = new PropertyPath("LastReadValue"),
                });

                multi.Bindings.Add(new Binding()
                {
                    Source = regMsb,
                    Path = new PropertyPath("LastReadValue"),
                });

                uiControl.SetBinding(Slider.ValueProperty, multi);
            }
        }

        #endregion

        #region Private Methods

        private int CountSetBits(int value)
        {
            return Convert.ToString(value, 2).ToCharArray().Count(c => c == '1');
        }

        private int ConvertHexToInt(string hexValue)
        {
            string hex = (hexValue.ToUpper().Contains("0X")) ? hexValue.Substring(2) : hexValue;
            return Convert.ToInt32(hex, 16);
        }

        #endregion
    }
}
