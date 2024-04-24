using System;
using System.Windows;
using System.Windows.Controls;
using log4net;
using log4net.Config;
namespace Calc
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static ILog Log = LogManager.GetLogger(typeof(MainWindow));
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Log.Info("Window_Loaded event execution started");
            try
            {
                
                Button b = (Button)sender;
                tb.Text += b.Content.ToString();
                Log.Info("try block executed");
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }
        
        private void Result_click(object sender, RoutedEventArgs e)
        {
            
            try
            {
                result();
                Log.Info("try block executed");
            }
            catch (Exception ex)
            {
                tb.Text = "Error!";
                Log.Error(ex);
            }
        }
        

        private void result()
        {
            try
            {
                String op;
                int iOp = 0;
                if (tb.Text.Contains("+"))
                {
                    iOp = tb.Text.IndexOf("+");
                }
                else if (tb.Text.Contains("-"))
                {
                    iOp = tb.Text.IndexOf("-");
                }
                else if (tb.Text.Contains("*"))
                {
                    iOp = tb.Text.IndexOf("*");
                }
                else if (tb.Text.Contains("/"))
                {
                    iOp = tb.Text.IndexOf("/");
                }
                else
                {
                    //error    
                }

                op = tb.Text.Substring(iOp, 1);
                double op1 = Convert.ToDouble(tb.Text.Substring(0, iOp));
                double op2 = Convert.ToDouble(tb.Text.Substring(iOp + 1, tb.Text.Length - iOp - 1));

                if (op == "+")
                {
                    tb.Text += "=" + (op1 + op2);
                }
                else if (op == "-")
                {
                    tb.Text += "=" + (op1 - op2);
                }
                else if (op == "*")
                {
                    tb.Text += "=" + (op1 * op2);
                }
                else
                {
                    tb.Text += "=" + (op1 / op2);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }
    

        private void Off_Click_1(object sender, RoutedEventArgs e)
        {
            try 
            { 
                Application.Current.Shutdown();
                Log.Info("try block executed");
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        private void Del_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                tb.Text = "";
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        private void R_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (tb.Text.Length > 0)
                {
                    tb.Text = tb.Text.Substring(0, tb.Text.Length - 1);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }
    }
}
