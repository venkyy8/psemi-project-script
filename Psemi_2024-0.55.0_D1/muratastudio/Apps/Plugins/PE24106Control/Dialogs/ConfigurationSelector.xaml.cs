﻿using System;
using System.Collections.Generic;
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

namespace PE24106Control.Dialogs
{
    /// <summary>
    /// Interaction logic for ConfigurationSelector.xaml
    /// </summary>
    public partial class ConfigurationSelector : Window
    {
        public int Configuration { get; set; }

        public ConfigurationSelector()
        {
            InitializeComponent();
            Configuration = 1;
        }

        private void Config1Button_Click(object sender, RoutedEventArgs e)
        {
            Configuration = 1;
            Close();
        }

        private void Config2Button_Click(object sender, RoutedEventArgs e)
        {
            Configuration = 2;
            Close();
        }

        private void Config3Button_Click(object sender, RoutedEventArgs e)
        {
            Configuration = 3;
            Close();
        }
    }
}
