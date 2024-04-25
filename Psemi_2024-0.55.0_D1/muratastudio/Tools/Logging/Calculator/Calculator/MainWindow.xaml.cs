using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Calculator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {        
        public MainWindow()
        {
           Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(@"C:\Logs\myCalclogs.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();
            
            InitializeComponent();
        }
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            try
            {
                Button b = (Button)sender;
                tb.Text += b.Content.ToString();
                Log.Debug("Button is Pressed", b);
            }
            catch(Exception ex)
            {
                Log.Error(ex, "Something went wrong");
            }
        }
        private void Result_click(object sender, RoutedEventArgs e)
        {
            try
            {
                result();
                Log.Debug("Completed ");
            }
            catch (Exception ex)
            {
                tb.Text = "Error!";
                Log.Error(ex, "Something went wrong");
            }
        }
        private void result()
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
        private void Off_Click_1(object sender, RoutedEventArgs e)
        {
            try
            {
                Application.Current.Shutdown();
                Log.Debug("Off ");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Something went wrong");
            }
        }
        private void Del_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                tb.Text = "";
                Log.Debug("Deleted ");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Something went wrong");
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
                Log.Debug("Completed ");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Something went wrong");
            }
            
        }
    }
}