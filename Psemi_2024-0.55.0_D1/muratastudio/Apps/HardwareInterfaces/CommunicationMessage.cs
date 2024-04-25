using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HardwareInterfaces
{
	public class CommunicationMessage : IMessageBase
	{
		public string MessageTime { get; set; }

		public Exception TopLevel { get; set; }

		public Exception BaseLevel { get; set; }

		public Object Sender { get; set; }

		public string MessageType { get; set; }

		public bool SupressWarning { get; set; }

		public CommunicationMessage(object sender, Exception e)
		{
			MessageTime = DateTime.Now.ToString("hh:mm:ss:fff");
			Sender = sender;
			TopLevel = e;
			BaseLevel = e.InnerException;
			MessageType = "Error";
		}

		public CommunicationMessage()
		{
			MessageTime = DateTime.Now.ToString("hh:mm:ss:fff");
		}
	}
}
