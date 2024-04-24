using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using muRata.ViewModel;
using muRata.Views;

namespace muRata.Helpers
{
	internal class SplashScreenHelper
	{
		public static Splash SplashScreen { get; set; }

		public static void Show()
		{
            if (SplashScreen != null)
                SplashScreen.Show();
        }

		public static void Hide()
		{
			if (SplashScreen == null) return;

			if (!SplashScreen.Dispatcher.CheckAccess())
			{
				Thread thread = new Thread(
					new System.Threading.ThreadStart(
						delegate()
						{
							SplashScreen.Dispatcher.Invoke(
								DispatcherPriority.Normal,
								new Action(delegate()
								{
									SplashScreen.Hide();
								}
							));
						}
				));
				thread.SetApartmentState(ApartmentState.STA);
				thread.Start();
			}
			else
				SplashScreen.Hide();
		}

		public static void ShowText(string text)
		{
			if (SplashScreen == null) return;

			if (!SplashScreen.Dispatcher.CheckAccess())
			{
				Thread thread = new Thread(
					new System.Threading.ThreadStart(
						delegate()
						{
							SplashScreen.Dispatcher.Invoke(
								DispatcherPriority.Normal,

								new Action(delegate()
								{
									((SplashViewModel)SplashScreen.DataContext).SplashScreenText = text;
								}
							));
							SplashScreen.Dispatcher.Invoke(DispatcherPriority.ApplicationIdle, new Action(() => { }));
						}
				));
				thread.SetApartmentState(ApartmentState.STA);
				thread.Start();
			}
			else
				((SplashViewModel)SplashScreen.DataContext).SplashScreenText = text;
		}
	}
}
