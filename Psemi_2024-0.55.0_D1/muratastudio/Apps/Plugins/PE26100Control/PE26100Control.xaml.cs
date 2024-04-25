using PE26100Control.ViewModel;
using System.Windows;
using System.Text.RegularExpressions;
using System.Windows.Input;
using System.Windows.Controls;

namespace PE26100Control
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class PE26100Control : UserControl
	{
		PluginViewModel pvm = null;
        public PE26100Control(object device, bool isInternalMode)
        {
            InitializeComponent();
            pvm = new PluginViewModel(device, isInternalMode);
            DataContext = pvm;
            this.Loaded += PE26100Control_Loaded;

            //if condition should be added for /devmode
            if (isInternalMode)
            {
                pvm.TurnOnPrivateModeRegister();
            }
        }     
        void PE26100Control_Loaded(object sender, RoutedEventArgs e)
		{
			pvm = this.DataContext as PluginViewModel;
			pvm._loaded = true;
		}        
        private void txtInput_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            string newtext = DecimalInRegEx(sender, e);
            pvm.SenseRegValueChanged("txtInput", newtext);
        }
        private void txtOutput_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            string newtext = DecimalInRegEx(sender, e);
            pvm.SenseRegValueChanged("txtOutput", newtext);
        }
        private static string DecimalInRegEx(object sender, TextCompositionEventArgs e)
        {
            string sendertext = (sender as TextBox).Text;
            string eventargText = e.Text;
            string newtext = string.Empty;
            string pattern = @"^[.][0-9]+$|^[0-9]*[.]{0,2}[0-9]*$"; // regular expression
            Regex regex = new Regex(pattern);
            e.Handled = !regex.IsMatch((sender as TextBox).Text.Insert((sender as TextBox).SelectionStart, e.Text));
            if (!e.Handled)
            {
                int selectionStart = (sender as TextBox).SelectionStart;   // beginning of current selection
                if ((sender as TextBox).Text.Length < selectionStart)
                    selectionStart = (sender as TextBox).Text.Length;

                int selectionLength = (sender as TextBox).SelectionLength;      // length of current selection
                if ((sender as TextBox).Text.Length < selectionStart + selectionLength)
                    selectionLength = (sender as TextBox).Text.Length - selectionStart;

                var realtext = (sender as TextBox).Text.Remove(selectionStart, selectionLength);

                int newvalIndex = (sender as TextBox).CaretIndex;       //current insertion position index 
                if (realtext.Length < newvalIndex)
                    newvalIndex = realtext.Length;

                newtext = realtext.Insert(newvalIndex, e.Text);
            }
            return newtext;
        }

    }
}
