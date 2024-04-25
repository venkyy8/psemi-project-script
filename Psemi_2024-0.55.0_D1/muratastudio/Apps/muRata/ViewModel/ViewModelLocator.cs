using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;
using Microsoft.Practices.ServiceLocation;
using PluginFramework;
using muRata.Helpers;
using GalaSoft.MvvmLight.Messaging;

namespace muRata.ViewModel
{
	/// <summary>
	/// This class contains static references to all the view models in the
	/// application and provides an entry point for the bindings.
	/// </summary>
	public class ViewModelLocator
	{
		/// <summary>
		/// Initializes a new instance of the ViewModelLocator class.
		/// </summary>
		public ViewModelLocator()
		{
			ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);
			SimpleIoc.Default.Register<SplashViewModel>();
			SimpleIoc.Default.Register<MainViewModel>();
			SimpleIoc.Default.Register<Bootstrapper>();
		}

		/// <summary>
		/// Gets the Main property.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
			"CA1822:MarkMembersAsStatic",
			Justification = "This non-static member is needed for data binding purposes.")]
		public MainViewModel Main
		{
			get
			{
				return ServiceLocator.Current.GetInstance<MainViewModel>();
			}
		}

		/// <summary>
		/// Gets the Main property.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
			"CA1822:MarkMembersAsStatic",
			Justification = "This non-static member is needed for data binding purposes.")]
		public SplashViewModel Splash
		{
			get
			{
				return ServiceLocator.Current.GetInstance<SplashViewModel>();
			}
		}
		
		public static void Cleanup()
		{
			Messenger.Default.Send(new NotificationMessage(Notifications.CleanupNotification));
		}
	}
}
