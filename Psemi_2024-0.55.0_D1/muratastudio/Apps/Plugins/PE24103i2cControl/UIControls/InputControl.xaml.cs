//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
using DeviceAccess;
using HardwareInterfaces;
using PE24103i2cControl.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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

namespace PE24103i2cControl.UIControls
{
    /// <summary>
    /// Interaction logic for InputControl.xaml
    /// </summary>
    public partial class InputControl : UserControl, IMappedControl
    {
        #region Events

        public delegate void OnWriteEventHandler(MappedRegister reg, string value);

        public event OnWriteEventHandler Write;

        protected virtual void OnWrite(MappedRegister reg, string value)
        {
            if (Write != null)
                Write.Invoke(reg, value);
        }

        #endregion

        #region Constructor

        public InputControl()
        {
            InitializeComponent();
            DataContext = this;
        }

        #endregion

        #region Properties

        public MappedRegister Reg { get; set; }

        public string ValidCharacters { get; set; }

        #endregion

        #region Dependency Properties        

        public string Units
        {
            get { return (string)GetValue(UnitsProperty); }
            set { SetValue(UnitsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Units.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty UnitsProperty =
            DependencyProperty.Register("Units", typeof(string), typeof(InputControl), new PropertyMetadata(""));


        public string Display
        {
            get { return (string)GetValue(DisplayProperty); }
            set { SetValue(DisplayProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Display.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DisplayProperty =
            DependencyProperty.Register("Display", typeof(string), typeof(InputControl), new PropertyMetadata(""));


        public bool IsReadOnly
        {
            get { return (bool)GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsReadOnly.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(InputControl), new PropertyMetadata(false));

        #endregion

        #region Event Handlers

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            txtEdit.Text = txtDisplay.Text;
            txtEdit.Visibility = System.Windows.Visibility.Visible;
            txtEdit.SelectAll();
            txtEdit.Focus();
        }

        private void txtEdit_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // Validate the data
                if (CanParse(txtEdit.Text))
                {
                    txtEdit.Visibility = System.Windows.Visibility.Hidden;
                    OnWrite(Reg, txtEdit.Text);
                }
            }
            else if (e.Key == Key.Escape)
            {
                txtEdit.Visibility = System.Windows.Visibility.Hidden;
            }
        }

        private void txtEdit_LostFocus(object sender, RoutedEventArgs e)
        {
            txtEdit.Visibility = System.Windows.Visibility.Hidden;
        }

        private void txtEdit_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }

        #endregion

        #region Private Methods

        private bool CanParse(string text)
        {
            bool canParse = false;
            switch (Reg.RegisterSource.DataType)
            {
                case "L11":
                case "S":
                case "F":
                    float s;
                    canParse = float.TryParse(text, out s);
                    break;
                case "H":
                    short i;
                    canParse = short.TryParse(text, System.Globalization.NumberStyles.HexNumber, null, out i);
                    break;
                case "U":
                default:
                    ushort u;
                    canParse = ushort.TryParse(text, out u);
                    break;
            }

            return canParse;
        }

        private bool IsTextAllowed(string text)
        {
            foreach (char c in text)
            {
                if (!ValidCharacters.Contains(c))
                    return false;
            }
            return true;
        }

        #endregion
    }
}

