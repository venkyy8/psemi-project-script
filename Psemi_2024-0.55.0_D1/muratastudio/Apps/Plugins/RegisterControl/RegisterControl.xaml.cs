using HardwareInterfaces;
using Microsoft.Win32;
using RegisterControl.ViewModel;
using Serilog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;


namespace RegisterControl
{
    /// <summary>
    /// Interaction logic for RegisterControl.xaml
    /// </summary>
    public partial class RegisterControl : UserControl
    {
        PluginViewModel pvm = null;
        bool _is16BitAddressing;
        private IEnumerable _filteredLists;
        private bool isPE26100Mode;

        public RegisterControl(object device, bool isInternalMode, bool is16BitMode = false)
        {
            InitializeComponent();
            pvm = new PluginViewModel(device, isInternalMode, is16BitMode);
            DataContext = pvm;
            _is16BitAddressing = is16BitMode;
            if (!_is16BitAddressing) 
            {
                RegistersTab8Bit.Visibility = Visibility.Visible;
                RegistersTab16Bit.Visibility = Visibility.Hidden;
                Registers16BitTabControl.Visibility = Visibility.Hidden;
            }
            else
            {
                RegistersTab8Bit.Visibility = Visibility.Hidden;
                RegistersTab16Bit.Visibility = Visibility.Visible;
                Registers16BitTabControl.Visibility = Visibility.Visible;
            }
            this.Loaded += RegisterControl_Loaded;
        }

        void RegisterControl_Loaded(object sender, RoutedEventArgs e)
        {
            pvm = this.DataContext as PluginViewModel;
            pvm._loaded = true;

            //dgErase.ItemsSource = pvm.EraseRegisters;
            //dgProgram.ItemsSource = pvm.ProgramRegisters;
        }

        private void eraseButton_Click(object sender, RoutedEventArgs e)
        {
            if (pvm.MTPRegisters == null || pvm?.MTPRegisters.Count == 0)
            {
                MessageBox.Show("Please load the MTP registers file to erase Page");
            }
            else
            {
                if (pvm?.PageRegisters.Count == 0)
                {
                    MessageBox.Show("Please load the Page to perform Erase operation");
                }
                else
                {
                    pvm?.SendClockSignal();

                    if (pvm.SendRegisterValues(pvm?.EraseRegisters))
                    {
                        var page = pageCombobox.SelectedItem.ToString();
                        MessageBox.Show(string.Format("Erase operation is completed for {0}", page));
                    }
                    else
                    {
                        MessageBox.Show("Please enter the register and data to perform Erase operation");
                    }
                }
            }
        }

        private void programButton_Click(object sender, RoutedEventArgs e)
        {
            if (pvm.MTPRegisters == null || pvm?.MTPRegisters.Count == 0)
            {
                MessageBox.Show("Please load the MTP registers to program Page");
            }
            else
            {
                if (pvm?.PageRegisters.Count == 0)
                {
                    MessageBox.Show("Please load the Page to perform program operation");
                }
                else
                {
                    pvm?.SendClockSignal();

                    if (pvm.SendRegisterValues(pvm?.ProgramRegisters))
                    {
                        var page = pageCombobox.SelectedItem.ToString();
                        MessageBox.Show(string.Format("MTP Programming for {0} is completed", page));
                    }
                    else
                    {
                        MessageBox.Show("Please enter the register address and data to program MTP");
                    }
                }
            }
        }

        private void browseButton_Click(object sender, RoutedEventArgs e)
        {
            bool isFileSelected = pvm.BrowsePath();

            if (isFileSelected)
            {
                ObservableCollection<MTPRegister> mTPRegisters = pvm.LoadMTPRegisters();

                if (pageCombobox.SelectedItem == null)
                {
                    if (pvm.Pages.Any())
                    {
                        pageCombobox.SelectedItem = pvm.Pages[0];
                    }
                }

                if (mTPRegisters != null)
                {
                    MTPGrid.ItemsSource = mTPRegisters;
                    string pageValue = (string)pageCombobox.SelectedItem;
                    pvm?.ReadPageValuesFromFile(pageValue, true, false, true);
                    pageGrid.ItemsSource = pvm.PageRegisters;
                }
                else
                {
                    MessageBox.Show("Please upload the MTP registers file to load the registers");
                }
            }
            else
                return;
        }

        private void loadButton_Click(object sender, RoutedEventArgs e)
        {
            ObservableCollection<MTPRegister> mTPRegisters = pvm.LoadMTPRegisters();
            if (pageCombobox.SelectedItem == null)
            {
                pageCombobox.SelectedItem = pvm.Pages[0];
            }

            if (mTPRegisters != null)
            {
                MTPGrid.ItemsSource = mTPRegisters;
            }
            else
            {
                MessageBox.Show("Please upload the MTP registers file to load the registers");
            }
        }

        private void readButton_Click(object sender, RoutedEventArgs e)
        {
            string pageValue = (string)pageCombobox.SelectedItem;

            if (pvm.MTPRegisters == null || pvm?.MTPRegisters.Count == 0)
            {
                MessageBox.Show("Please load the MTP registers file to read the page registers");
            }
            else
            {
                pvm?.ReadPageValuesFromFile(pageValue, false, true);

                if (pvm?.PageRegisters.Count == 0)
                {
                    MessageBox.Show("Please enter the correct page address");
                }
                else
                {
                    pageGrid.ItemsSource = pvm?.PageRegisters;
                }
            }
        }

        private void writeButton_Click(object sender, RoutedEventArgs e)
        {
            if (pvm?.PageRegisters == null || pvm?.PageRegisters.Count == 0)
            {
                MessageBox.Show("Please load the Page to perform write operation");
            }
            else
            {
                pvm?.SendClockSignal();
                var page = pageCombobox.SelectedItem.ToString();

                if (!pvm.SendRegisterValues(pvm?.EraseRegisters))
                {
                    MessageBox.Show("Please enter the register and data to perform Erase operation");
                }

                bool isSuccess = pvm.WriteRWRegistersFromMTP();
                if (!isSuccess)
                {
                    MessageBox.Show(string.Format("RW Registers cannot be empty for page - {0} ", page));
                }

                pvm?.SendClockSignal();

                if (!pvm.SendRegisterValues(pvm?.ProgramRegisters))
                {
                    MessageBox.Show("Please enter the register address and data to program MTP");
                }
                else
                {
                    MessageBox.Show(string.Format("Write operation completed for page - {0}", page));
                }

                //pvm?.WriteMTPRegisterValues();
                //pvm?.SendClockSignal();
                //pvm?.SendRegisterValues(pvm?.ProgramRegisters);
            }
        }

        private void offButton_Checked(object sender, RoutedEventArgs e)
        {
            pvm?.TurnOffMasterTestRegister();
        }

        private void onButton_Checked(object sender, RoutedEventArgs e)
        {
            pvm?.TurnOnMasterTestRegister();
        }

        private void TabItem_GotFocus(object sender, RoutedEventArgs e)
        {
            if (_is16BitAddressing)
            {
                pvm.CheckMasterTestStatus();
            }
        }

        private void pageCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string pageValue = (string)pageCombobox.SelectedItem;

            pvm.UpdateEraseAndProgamRegisters(pageValue);

            if (pvm.MTPRegisters == null || pvm?.MTPRegisters.Count == 0)
            {
                MessageBox.Show("Please load the MTP registers file to read the page registers");
            }
            else
            {
                pvm?.ReadPageValuesFromFile(pageValue, true);

                if (pvm?.PageRegisters.Count == 0)
                {
                    MessageBox.Show("Please enter the correct page address");
                }
                else
                {
                    pageGrid.ItemsSource = pvm?.PageRegisters;
                }
            }
        }

        private void exportButton_Click(object sender, RoutedEventArgs e)
        {
            MTPGrid.SelectAllCells();
            MTPGrid.ClipboardCopyMode = DataGridClipboardCopyMode.IncludeHeader;
            ApplicationCommands.Copy.Execute(null, MTPGrid);
            MTPGrid.UnselectAllCells();
            String result = (string)Clipboard.GetData(DataFormats.CommaSeparatedValue);

            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.InitialDirectory = @"C:\";
            saveFileDialog1.Title = "Save text Files";
            saveFileDialog1.CheckFileExists = false;
            saveFileDialog1.CheckPathExists = true;
            saveFileDialog1.Filter = "CSV files(*.csv)| *.csv | All files(*.*) | *.* ";
            saveFileDialog1.FilterIndex = 1;
            saveFileDialog1.DefaultExt = ".csv";
            saveFileDialog1.RestoreDirectory = true;
            if (saveFileDialog1.ShowDialog() == true)
            {
                File.AppendAllText(saveFileDialog1.FileName, result, UnicodeEncoding.UTF8);
            }


        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var input = sender as TextBox;

            if (pvm == null || !pvm.Registers.Any())
            {
                Log.Error("Plugin view model is null or registers are empty.");
                return;
            }

            if (String.IsNullOrWhiteSpace(input.Text))
            {
                try
                {
                    if (_is16BitAddressing)
                    {
                        dataGrid2.ItemsSource = pvm?.Registers;
                        lblSearchResult2.Visibility = Visibility.Hidden;
                        
                    }
                    else
                    {
                        dataGrid.ItemsSource = pvm?.Registers;
                        lblSearchResult1.Visibility = Visibility.Hidden;
                    }
                }
                catch (Exception ex3)
                {
                    Log.Error(" Error thrown on Search implementation On clearing the text input ", ex3);
                }
            }
            else
            {
                try
                {
                    List<Register> filteredRegisters = new List<Register>();
                    pvm.RegistersList = new ObservableCollection<Register>();

                    filteredRegisters.AddRange(CheckMatchInRegisterName(input.Text));
                    filteredRegisters.AddRange(CheckMatchInRegisterUnit(input.Text));
                    filteredRegisters.AddRange(CheckMatchInRegisterAddress(input.Text));
                    filteredRegisters.AddRange(CheckMatchInRegisterValue(input.Text));
                    filteredRegisters.AddRange(CheckMatchInRegisterData(input.Text));

                    foreach (Register register in filteredRegisters)
                    {
                        IEnumerable<Register> vmRegisterList = pvm.RegistersList.Where(reg => reg.Address == register.Address);
                        //To avoid duplicates
                        if (!vmRegisterList.Any())
                        {
                            pvm.RegistersList.Add(register);
                        }
                    }
                    

                    if (pvm.RegistersList.Any())
                    {
                        try
                        {
                            if (_is16BitAddressing)
                            {
                                dataGrid2.ItemsSource = null;
                                dataGrid2.ItemsSource = pvm.RegistersList;
                                lblSearchResult2.Foreground = new SolidColorBrush(Color.FromRgb(66, 0, 0));
                                lblSearchResult2.Visibility = Visibility.Visible;
                                lblSearchResult2.Content = string.Format("Matching rows found : " + pvm.RegistersList.Count);
                            }
                            else
                            {
                                dataGrid.ItemsSource = null;
                                dataGrid.ItemsSource = pvm.RegistersList;
                                lblSearchResult1.Foreground = new SolidColorBrush(Color.FromRgb(66, 0, 0));
                                lblSearchResult1.Visibility = Visibility.Visible;
                                lblSearchResult1.Content = string.Format("Matching rows found : " + pvm.RegistersList.Count);
                            }
                        }
                        catch (Exception ex2)
                        {
                            Log.Error(" Error thrown on Search implementation with input:" + input.Text + " and matching Registers count : " + pvm.Registers.Count, ex2);
                        }
                    }
                    else
                    {
                        if (_is16BitAddressing)
                        {
                            lblSearchResult2.Content = "No matching rows found !";
                            lblSearchResult2.Foreground = new SolidColorBrush(Color.FromRgb(255, 0, 0));
                            lblSearchResult2.Visibility = Visibility.Visible;
                            dataGrid2.ItemsSource = pvm.Registers;
                        }
                        else
                        {
                            lblSearchResult1.Content = "No matching rows found !";
                            lblSearchResult1.Foreground = new SolidColorBrush(Color.FromRgb(255, 0, 0));
                            lblSearchResult1.Visibility = Visibility.Visible;
                            dataGrid.ItemsSource = pvm.Registers;
                        }
                    }
                }

                catch (Exception ex1)
                {
                    Log.Error(" Error thrown on Search implementation Execution: ", ex1);
                    throw new Exception(" Error thrown on Search implementation Execution", ex1);
                }
            }
        }

        private IEnumerable<Register> CheckMatchInRegisterName(string inputText)
        {
            IEnumerable<Register> regs = pvm.Registers.Where(reg =>

                   (reg.DisplayName.ToUpper() == inputText.ToUpper()) ||
                   (reg.DisplayName.ToUpper().StartsWith(inputText.ToUpper())) ||
                   (reg.DisplayName.ToUpper().EndsWith(inputText.ToUpper()))
                    );

            return regs;
        }

        private IEnumerable<Register> CheckMatchInRegisterUnit(string inputText)
        {
            IEnumerable<Register> regs = pvm.Registers.Where(reg =>

                   (reg.Unit.ToUpper().StartsWith(inputText.ToUpper())) ||
                   (reg.Unit.ToUpper().EndsWith(inputText.ToUpper()))
                    );

            return regs;
        }

        private IEnumerable<Register> CheckMatchInRegisterAddress(string inputText)
        {
            IEnumerable<Register> regs = null;

            if (inputText.ToUpper() == "0" || inputText.ToUpper() == "0X")
            {
                regs = pvm.Registers;
            }
            else if (inputText.ToUpper().StartsWith("0X"))
            {
                //To handle input text referring to address starting with '0x'
                if (!_is16BitAddressing)
                {
                    regs = pvm.Registers.Where(regi => (("0X" + regi.Address.ToString("X2").ToUpper()) == (inputText.ToUpper())));

                    if (!regs.ToList().Any())
                    {
                        regs = pvm.Registers.Where(regi => ("0X" + regi.Address.ToString("X2").ToUpper()).StartsWith(inputText.ToUpper()));
                    }
                }
                else
                {
                    regs = pvm.Registers.Where(regi => (("0X" + regi.Address.ToString("X3").ToUpper()) == (inputText.ToUpper())));

                    if (!regs.ToList().Any())
                    {
                        regs = pvm.Registers.Where(regi => ("0X" + regi.Address.ToString("X3").ToUpper()).StartsWith(inputText.ToUpper()));
                    }
                }
            }
            else
            {
                //To handle input text referring to address starting without '0x'

                if (!_is16BitAddressing)
                {
                    regs = pvm.Registers.Where(regi => ((regi.Address.ToString("X2").ToUpper()) == (inputText.ToUpper())));

                    if (!regs.ToList().Any())
                    {
                        regs = pvm.Registers.Where(regi => ((regi.Address.ToString("X2").ToUpper()).StartsWith(inputText.ToUpper())));
                    }
                }
                else
                {
                    regs = pvm.Registers.Where(regi => (regi.Address.ToString("X3").ToUpper()) == (inputText.ToUpper()));

                    if (!regs.ToList().Any())
                    {
                        regs = pvm.Registers.Where(regi => (regi.Address.ToString("X3").ToUpper()).StartsWith(inputText.ToUpper()));
                    }
                }

            }

            return regs;
        }

        private IEnumerable<Register> CheckMatchInRegisterValue(string inputText)
        {

            IEnumerable<Register> regs = pvm.Registers.Where(reg => (reg.LastReadString!="")&&(
                   reg.LastReadString.ToUpper().StartsWith(inputText.ToUpper()) ||reg.LastReadString.ToUpper().EndsWith(inputText.ToUpper()))
                    );

            return regs;
        }

        private IEnumerable<Register> CheckMatchInRegisterData(string inputText)
        {

            IEnumerable<Register> regs = pvm.Registers.Where(reg => ((reg.LastReadValueWithoutFormula != 0 )&&(((int)reg.LastReadValueWithoutFormula).ToString("X") == inputText.ToUpper() || 
            ((int)reg.LastReadValueWithoutFormula).ToString("X").ToUpper().StartsWith(inputText.ToUpper()) || ((int)reg.LastReadValueWithoutFormula).ToString("X").EndsWith(inputText.ToUpper()))));

            return regs;
        }

    }
}
