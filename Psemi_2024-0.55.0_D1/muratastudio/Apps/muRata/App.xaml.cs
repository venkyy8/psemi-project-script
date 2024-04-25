using muRata.Helpers;
using muRata.Views;
using GalaSoft.MvvmLight.Threading;
using Microsoft.Shell;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace muRata
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application, ISingleInstanceApp
	{
		private const string Unique = "ApplicationName";

		[STAThread]
		public static void Main()
		{
			DispatcherHelper.Initialize();

			Thread thread = new Thread(
			new System.Threading.ThreadStart(
				delegate()
				{
					SplashScreenHelper.SplashScreen = new Splash();
					SplashScreenHelper.Show();
					System.Windows.Threading.Dispatcher.Run();
				}
			));
			thread.SetApartmentState(ApartmentState.STA);
			thread.IsBackground = true;
			thread.Start();

			if (SingleInstance<App>.InitializeAsFirstInstance(Unique))
			{
				var application = new App();
				application.InitializeComponent();
				application.Run();

				// Allow single instance code to perform cleanup operations
				SingleInstance<App>.Cleanup();
			}
		}

		protected override void OnStartup(StartupEventArgs e)
		{
			// Select the text in a TextBox when it receives focus.
			EventManager.RegisterClassHandler(typeof(TextBox), TextBox.PreviewMouseLeftButtonDownEvent,
				new MouseButtonEventHandler(SelectivelyIgnoreMouseButton));
			EventManager.RegisterClassHandler(typeof(TextBox), TextBox.GotKeyboardFocusEvent,
				new RoutedEventHandler(SelectAllText));
			EventManager.RegisterClassHandler(typeof(TextBox), TextBox.MouseDoubleClickEvent,
				new RoutedEventHandler(SelectAllText));
			base.OnStartup(e);
		}

		void SelectivelyIgnoreMouseButton(object sender, MouseButtonEventArgs e)
		{
			// Find the TextBox
			DependencyObject parent = e.OriginalSource as UIElement;
			while (parent != null && !(parent is TextBox))
				parent = VisualTreeHelper.GetParent(parent);

			if (parent != null)
			{
				var textBox = (TextBox)parent;
				if (!textBox.IsKeyboardFocusWithin)
				{
					// If the text box is not yet focused, give it the focus and
					// stop further processing of this click event.
					textBox.Focus();
					e.Handled = true;
				}
			}
		}

		private static void SelectAllText(object sender, RoutedEventArgs e)
		{
			var textBox = e.OriginalSource as TextBox;
			if (textBox != null)
				textBox.SelectAll();
		}

		public bool SignalExternalCommandLineArgs(IList<string> args)
		{
			return ((MainWindow)MainWindow).ProcessCommandLineArgs(args);
		}
	}
}
