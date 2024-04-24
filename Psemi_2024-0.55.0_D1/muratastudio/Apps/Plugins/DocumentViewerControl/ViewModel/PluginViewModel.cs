using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DocumentViewerControl.ViewModel
{
	public class PluginViewModel : DependencyObject, ICleanup
	{
		public PluginViewModel(object device, bool isInternalMode)
		{

		}

		public void Cleanup()
		{
			// Nothing to cleanup
		}
	}
}
