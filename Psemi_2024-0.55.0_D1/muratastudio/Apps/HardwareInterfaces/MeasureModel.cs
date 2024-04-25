using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HardwareInterfaces
{
	public class MeasureModel : INotifyPropertyChanged
	{
		private DateTime _dateTime;
		private double _value;

		public DateTime DateTime
		{
			get { return _dateTime; }
			set
			{
				_dateTime = value;
				OnPropertyChanged("DateTime");
			}
		}

		public double Value
		{
			get { return _value; }
			set
			{
				_value = value;
				OnPropertyChanged("Value");
			}
		}

		#region INotifyPropertyChanged implementation

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(string propertyName = null)
		{
			if (PropertyChanged != null)
				PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion
	}
}
