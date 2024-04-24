using DeviceAccess;
using HardwareInterfaces;
using PE24103i2cControl.Helpers;
using System;
using System.Collections;
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
    /// Interaction logic for ListControl.xaml
    /// </summary>
    public partial class ListControl : UserControl, IMappedControl
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

        public ListControl()
        {
            InitializeComponent();
            DataContext = this;
        }

        #endregion

        #region Properties

        public MappedRegister Reg { get; set; }

        #endregion

        #region Dependency Properties        

        public IEnumerable ItemsSource
        {
            get { return (IEnumerable)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(IEnumerable),
                typeof(ListControl), new PropertyMetadata(null));


        public string Value
        {
            get { return (string)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Units.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(string), typeof(ListControl), new PropertyMetadata("0"));


        public string DisplayMemberPath
        {
            get { return (string)GetValue(DisplayMemberPathProperty); }
            set { SetValue(DisplayMemberPathProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DisplayMemberPath.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DisplayMemberPathProperty =
            DependencyProperty.Register("DisplayMemberPath", typeof(string), typeof(ListControl), new PropertyMetadata(""));

        public string SelectedValuePath
        {
            get { return (string)GetValue(SelectedValuePathProperty); }
            set { SetValue(SelectedValuePathProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DisplayMemberPath.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedValuePathProperty =
            DependencyProperty.Register("SelectedValuePath", typeof(string), typeof(ListControl), new PropertyMetadata(""));


        #endregion

        private void cmbSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            OnWrite(Reg, Value);
        }

        #region Event Handlers


        #endregion

        #region Private Methods


        #endregion
    }
}
