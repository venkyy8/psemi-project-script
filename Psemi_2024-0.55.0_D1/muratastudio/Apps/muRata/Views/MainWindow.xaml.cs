using GalaSoft.MvvmLight.Ioc;
using PluginFramework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime;
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
using System.Windows.Shapes;
using muRata.ViewModel;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Practices.ServiceLocation;
using muRata.Helpers;
using System.Threading;
using System.Windows.Controls.Ribbon;
using Xceed.Wpf.AvalonDock.Layout;
using HardwareInterfaces;
using System.ComponentModel;
using System.Windows.Threading;
using System.Diagnostics;
using AdapterAccess.Protocols;

namespace muRata.Views
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : RibbonWindow, IPluginHost
	{
		#region Private Members
		
		private MainViewModel _mvm;
		IPlugin ip = null;

		private List<RibbonTab> _tabs = new List<RibbonTab>();

		#endregion

		#region Constructor

		public MainWindow()
		{
			InitializeComponent();

			SimpleIoc.Default.Register<IPluginHost>(() => this);
			Messenger.Default.Register<NotificationMessage>(this, HandleNotification);

			_mvm = ServiceLocator.Current.GetInstance<MainViewModel>();
			_mvm.Startup(Initialize);

			Loaded += (s, e) =>
				{
					// Some kind of issue with the splash screen causing the main
					// window to display behind any other open windows.
					// This is a workaround.
					this.Topmost = true;
					this.Show();
					this.Topmost = false;
				};

			Closing += (s, e) =>
				{
					try
					{
						// Turn off all gpios
						foreach (var item in _mvm.Adapters)
						{
							var protocols = item.GetAttachedProtocolObjects();
							var protocol = protocols.Find(p => p.Name.Equals("I2C"));
							if (protocol != null)
							{
								var i2c = protocol as I2cProtocol;
								i2c.SetGpio(0);
							}
						}
					}
					catch
					{
						// Protect against missing adapters. Exit gracefully!
					}

					Clear();
					SimpleIoc.Default.Unregister<IPluginHost>(this);
					ViewModelLocator.Cleanup();
				};

			if (AdapterCombo.Items.Count > 0)
			{
				AdapterCombo.SelectedIndex = 0;
			}

			if (DeviceCombo.Items.Count > 0)
			{
				DeviceCombo.SelectedIndex = 0;
			}

			AdapterCombo.SelectionChanged += AdapterCombo_SelectionChanged;
			DeviceCombo.SelectionChanged += DeviceCombo_SelectionChanged;

			if (_mvm.IsDemoMode)
			{
				this.Title += " - DEMO MODE";
				StatusAdapterLabel.Background = Brushes.YellowGreen;
			}

			Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;

		}
		#endregion

		#region Public Methods

		public void Clear()
		{
			Messenger.Default.Send(new NotificationMessage(Notifications.CleanupNotification));
		}

		public bool ProcessCommandLineArgs(IList<string> args)
		{
			return true;
		}

		public void PlacePlugin(IPlugin plugin, object dataContext, bool isAllRegRead = false)
		{
			var bootstrapper = ServiceLocator.Current.GetInstance<Bootstrapper>();
			var mvm = ServiceLocator.Current.GetInstance<MainViewModel>();

			FrameworkElement element = null;

			try
			{
				// Filter the plugin dependency object based on some known plugins
				object pluginDependency = null;

				if (plugin.GetPluginInfo.AssemblyName.Contains("AdapterControl"))
				{
					pluginDependency = mvm.Adapters;
				}
				else
				{
					pluginDependency = mvm.ActiveDevice;
				}

				element = bootstrapper.GetElement(plugin, pluginDependency, mvm.ActiveDevice, mvm.IsInternalMode, mvm.ActiveDevice.Is16BitAddressing, isAllRegRead);
			}
			catch (Exception)
			{
				MessageBox.Show("Error when creating element for plugin " + plugin.GetType());
			}

			if (element == null)
			{
				return;
			}

			// Check the main container children
			foreach (var pane in DocLayoutPanel.Children)
			{
				if (pane is LayoutDocumentPane)
				{
					var p = pane as LayoutDocumentPane;
					foreach (var item in p.Children)
					{
						if (item.Title == plugin.GetPluginInfo.ButtonName)
						{
							item.IsSelected = true;
							item.IsMaximized = true;
							return;
						}
					}
				}

				if (pane is LayoutDocumentPaneGroup)
				{
					var pg = pane as LayoutDocumentPaneGroup;
					foreach (var item in pg.Children)
					{
						if (item is LayoutDocumentPane)
						{
							var i = item as LayoutDocumentPane;
							foreach (var v in i.Children)
							{
								if (v.Title == plugin.GetPluginInfo.ButtonName)
								{
									v.IsSelected = true;
									v.IsMaximized = true;
									return;
								}
							}
						}
					}
				}
			}

			// Check if the plugin is floating
			foreach (var item in DockRoot.FloatingWindows)
			{
				foreach (LayoutDocument child in item.Children)
				{
					if (child.Title == plugin.GetPluginInfo.ButtonName)
					{
						child.IsSelected = true;
						return;
					}
				}
			}

			// Check if plugin is hidden
			foreach (var item in DockRoot.Hidden)
			{
				if (item.Title == plugin.GetPluginInfo.ButtonName)
				{
					item.IsSelected = true;
					return;
				}
			}

			LayoutDocument ld = new LayoutDocument();
			ld.IsActiveChanged += ld_IsActiveChanged;
			ld.IsSelectedChanged += ld_IsSelectedChanged;
			ld.Closing += ld_Closing;
			ld.Title = plugin.GetPluginInfo.ButtonName;
			ld.IconSource = plugin.GetPluginInfo.DockTabImage;
			ld.Content = element;

			// Fine the first available Pane to dock the plugin.
			// If there are only PaneGroups, then we will look one level deep for a Pane.
			// If we cannot find one then the user need to change the configuration.
			var main = DocLayoutPanel.Children[0];
			if (main is LayoutDocumentPane)
			{
				LayoutDocumentPane pane = DocLayoutPanel.Children[0] as LayoutDocumentPane;
				pane.Children.Add(ld);
				ld.IsSelected = true;
			}
			else if (main is LayoutDocumentPaneGroup)
			{
				LayoutDocumentPaneGroup paneGroup = main as LayoutDocumentPaneGroup;
				if (paneGroup.Children[0] is LayoutDocumentPane)
				{
					LayoutDocumentPane pane = paneGroup.Children[0] as LayoutDocumentPane;
					pane.Children.Add(ld);
					ld.IsSelected = true;
				}
				else
				{
					MessageBox.Show("Pane grouping has reached its max.",
						"Application Layout", MessageBoxButton.OK, MessageBoxImage.Exclamation);
				}
			}
		}

		#endregion

		#region Private Methods

		void Current_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			MessageBox.Show(e.Exception.Message, "Application Exception", MessageBoxButton.OK, MessageBoxImage.Warning);
			e.Handled = true;
		}

		private void Initialize()
		{
			// Load the default document pane
			LayoutDocumentPane MainDocPane = new LayoutDocumentPane();
			DocLayoutPanel.Children.Add(MainDocPane);

			foreach (var item in _mvm.ActivePlugins)
			{
				ConfigureRibbonTabs(ref _tabs, item);
			}

			//MainRibbon.Items.Clear();
			foreach (var item in _tabs)
			{
				MainRibbon.Items.Add(item);
			}

			// Read the registers on startup unless we are in silent startup mode
			if (!_mvm.IsSilentMode)
			{
				_mvm.AllRegistersRead += allRegistersReadCompleted;
				_mvm.RefreshCommand.Execute(true);
			}

			if (_mvm.ActiveDevice != null && _mvm.ActiveDevice.Is16BitAddressing)
			{
				ip = _mvm.GetActiveDeviceUserInterface();
				//ip = _mvm.GetRegisterControlUserInterface();
			}
			else
            {
				// Show the main UI
				ip = _mvm.GetActiveDeviceUserInterface();
			}

			if (ip != null)
			{
				this.PlacePlugin(ip, _mvm);
			}
		}

		private void allRegistersReadCompleted(object sender, EventArgs e)
		{
			_mvm.IsAllRegistersRead = true;
			SplashScreenHelper.Hide();
			ip?.RefreshPlugin();
		}

		private void AdapterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (AdapterCombo.Items.Count > 0)
			{
				DeviceCombo.SelectionChanged -= DeviceCombo_SelectionChanged;

				ClearAllPlugins();
				_mvm.ActivePlugins.Clear();
				_mvm.ChangeAdapterCommand.Execute(AdapterCombo.SelectedIndex);
				_mvm.GetActivePluginsCommand.Execute(_mvm.ActiveAdapter);
				_mvm.GetActivePluginsCommand.Execute(_mvm.ActiveDevice);

				Initialize();

				if (DeviceCombo.Items.Count > 0)
				{
					DeviceCombo.SelectedIndex = 0;
				}

				DeviceCombo.SelectionChanged += DeviceCombo_SelectionChanged;
			}
		}

		private void DeviceCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (DeviceCombo.Items.Count > 0)
			{
				ClearAllPlugins();
				_mvm.ActivePlugins.Clear();
				int[] indexes = new int[] { AdapterCombo.SelectedIndex, DeviceCombo.SelectedIndex };
				_mvm.ChangeDeviceCommand.Execute(indexes);
				_mvm.GetActivePluginsCommand.Execute(_mvm.ActiveAdapter);
				_mvm.GetActivePluginsCommand.Execute(_mvm.ActiveDevice);
				Initialize();
			}
		}

		private void ConfigureRibbonTabs(ref List<RibbonTab> tabs, IPlugin plugin)
		{
			RibbonTab tab = null;
			RibbonGroup group = null;
			RibbonButton button = null;
			PluginInfo p = plugin.GetPluginInfo;
			var mvm = ServiceLocator.Current.GetInstance<MainViewModel>();

			//tabs.Clear();

			// Create the tab if it does not exist
			if (!tabs.Exists(t => t.Name == p.TabName))
			{
				tab = new RibbonTab
				{
					Name = p.TabName,
					Header = p.TabName,
					KeyTip = p.TabName.Substring(0, 1)
				};
				tabs.Add(tab);
			}

			tab = tabs.Find(t => t.Name == p.TabName);

			foreach (var item in tab.Items)
			{
				if (item is RibbonGroup)
				{
					RibbonGroup g = item as RibbonGroup;
					if (g.Name == p.PanelName)
					{
						group = g;
						break;
					}
				}
			}

			// Create the group if it does not exist
			if (group == null)
			{
				group = new RibbonGroup();
				group.Name = p.PanelName;
				group.Header = p.PanelName;
				tab.Items.Add(group);
			}
			else
			{
				//group.Items.Clear();
			}

			// Create the button
			button = new RibbonButton
			{
				Command = mvm.ShowPluginCommand,
				CommandParameter = plugin,
				LargeImageSource = p.ButtonImage,
				SmallImageSource = p.ButtonSmallImage,
				Label = p.ButtonName,
				ToolTipTitle = p.ButtonName,
				ToolTipImageSource = p.ButtonSmallImage,
				ToolTipDescription = p.ButtonToolTipText,
				Margin = new System.Windows.Thickness(3, 0, 3, 0),
				KeyTip = p.ButtonName.Substring(0, 1)
			};

			group.Items.Add(button);
		}

		private void HandleNotification(NotificationMessage message)
		{
			if (message.Notification == Notifications.CleanupNotification)
			{
				Messenger.Default.Unregister(this);
			}

			if (message.Notification == Notifications.DisplayErrorsNotification)
			{
				// Show the application log on error or warning
				// Possibly and a disable option
				Application.Current.Dispatcher.BeginInvoke((Action)(() =>
				{
					if (ApplicationLog.IsAutoHidden)
					{
						ApplicationLog.ToggleAutoHide();
					}
				}));
			}
		}

		private void DoEvents()
		{
			DispatcherFrame frame = new DispatcherFrame(true);
			Dispatcher.CurrentDispatcher.Invoke
			(
			DispatcherPriority.Background,
			(SendOrPostCallback)delegate(object arg)
			{
				var f = arg as DispatcherFrame;
				f.Continue = false;
			},
			frame
			);
			Dispatcher.PushFrame(frame);
		}

		#endregion

		#region Event Handlers

		void ld_IsActiveChanged(object sender, EventArgs e)
		{
			Application.Current.Dispatcher.BeginInvoke((Action)(() =>
			{
				LayoutDocument ld = sender as LayoutDocument;
				UserControl uc = (UserControl)ld.Content;
				if (uc != null)
				{
					IPlugin plugin = (IPlugin)uc.Tag;

					if (plugin.GetPluginInfo.ButtonName == "Charting")
					{
						if (ld.IsVisible && ld.IsActive)
						{
							if (ld.IsFloating)
							{
								ld.Dock();
								ld.CanFloat = false;
							}
						}
					}
				}
			}));
		}

		void ld_IsSelectedChanged(object sender, EventArgs e)
		{
			Application.Current.Dispatcher.BeginInvoke((Action)(() =>
			{
				LayoutDocument ld = sender as LayoutDocument;
				UserControl uc = (UserControl)ld.Content;
				if (uc != null)
				{
					IPlugin plugin = (IPlugin)uc.Tag;

					if (plugin.GetPluginInfo.ButtonName == "Charting")
					{
						if (!ld.IsFloating && ld.PreviousContainerIndex == -1)
						{
							ld.FloatingTop = this.Top;
							ld.FloatingLeft = this.Left;
							ld.CanFloat = true;
							ld.Float();
						}

						if (!ld.IsSelected)
						{
							ld.PreviousContainerIndex = -1;
						}
					}
				}
			}));
		}

		private void ld_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			LayoutDocument ld = sender as LayoutDocument;

			// Clean up the plugins resources
			UserControl uc = (UserControl)ld.Content;
			IPlugin plugin = (IPlugin)uc.Tag;
			plugin.ClosePlugin();

			// Remove event reference and allow to close
			ld.Closing -= ld_Closing;
		}

		private void Reinitialize_Click(object sender, RoutedEventArgs e)
		{
			// Instanciate the splash screen
			Thread splashThread = new Thread(
				new System.Threading.ThreadStart(
					delegate()
					{
						SplashScreenHelper.SplashScreen = new Splash();
						SplashScreenHelper.Show();
						System.Windows.Threading.Dispatcher.Run();
					}
				));
			splashThread.SetApartmentState(ApartmentState.STA);
			splashThread.IsBackground = true;
			splashThread.Start();

			AdapterCombo.SelectionChanged -= AdapterCombo_SelectionChanged;
			DeviceCombo.SelectionChanged -= DeviceCombo_SelectionChanged;

			ClearAllPlugins();

			AdapterCombo.Text = string.Empty;
			DeviceCombo.Text = string.Empty;

			// Because we are forcing a single instance application the threading model is ApartmentState.STA.
			// This causes issues with threading on the main UI thread. We will simply reinitialize the connection
			// on the main thread.
			// I am using a doevents hack to clear screen
			DoEvents();

			// Restart the application
			_mvm.ReStart();

			// Load the UI elements
			Initialize();

			if (AdapterCombo.Items.Count > 0)
			{
				AdapterCombo.SelectedIndex = 0;
			}

			if (DeviceCombo.Items.Count > 0)
			{
				DeviceCombo.SelectedIndex = 0;
			}

			AdapterCombo.SelectionChanged += AdapterCombo_SelectionChanged;
			DeviceCombo.SelectionChanged += DeviceCombo_SelectionChanged;
		}

		private void ClearAllPlugins()
		{
			// Clear all object on the screen
			_tabs.Clear();
			MainRibbon.Items.Clear();
			DocLayoutPanel.Children.Clear();
			DockRoot.FloatingWindows.Clear();
		}

		private void ShowAboutBox_Click(object sender, RoutedEventArgs e)
		{
			About about = new About(this);
			about.ShowDialog();
			//if (about.Updating)
			//{
			//	this.Close();
			//}
		}

		private void hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
		{
			string uri = e.Uri.AbsoluteUri;
			Process.Start(new ProcessStartInfo(uri));

			e.Handled = true;
		}

        #endregion

        private void MainApplicationWindow_Loaded(object sender, RoutedEventArgs e)
        {
			SplashScreenHelper.Hide();
        }
    }
}
