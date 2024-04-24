using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace AdapterControl
{
	public class LogEntry : PropertyChangedBase
	{
		public string DateTime { get; set; }

		public string ProtocolName { get; set; }

		public string Direction { get; set; }

		public string Address { get; set; }

		public string Register { get; set; }

		public string Length { get; set; }

		public string Data { get; set; }

		public Brush ForeColor { get; set; }
	}

	public class PropertyChangedBase : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(string propertyName)
		{
			Application.Current.Dispatcher.BeginInvoke((Action)(() =>
			{
				PropertyChangedEventHandler handler = PropertyChanged;
				if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
			}));
		}
	}
}
