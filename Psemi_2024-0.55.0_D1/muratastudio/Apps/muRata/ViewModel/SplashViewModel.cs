using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace muRata.ViewModel
{
	public class SplashViewModel : ViewModelBase
	{
		/// <summary>
		/// The <see cref="SplashScreenText" /> property's name.
		/// </summary>
		public const string SplashScreenTextPropertyName = "SplashScreenText";

		private string _splashScreenText = "Initializing...";

		/// <summary>
		/// Sets and gets the SplashScreenText property.
		/// Changes to that property's value raise the PropertyChanged event.
		/// </summary>
		public string SplashScreenText
		{
			get
			{
				return _splashScreenText;
			}

			set
			{
				if (_splashScreenText == value)
				{
					return;
				}

				_splashScreenText = value;
				RaisePropertyChanged(SplashScreenTextPropertyName);
			}
		}
	}
}
