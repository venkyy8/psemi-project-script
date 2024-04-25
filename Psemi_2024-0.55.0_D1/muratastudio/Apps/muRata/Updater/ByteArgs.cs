using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace muRata.Updater
{
	public delegate void BytesDownloadedEventHandler(ByteArgs e);

	public class ByteArgs : EventArgs
	{
		private int _downloaded;
		private int _total;

		public int downloaded
		{
			get
			{
				return _downloaded;
			}
			set
			{
				_downloaded = value;
			}
		}

		public int total
		{
			get
			{
				return _total;
			}
			set
			{
				_total = value;
			}
		}
	}
}
