using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HardwareInterfaces
{
	public interface IMessageBase
	{
		string MessageTime { get; set; }

		Exception TopLevel { get; set; }

		Exception BaseLevel { get; set; }

		Object Sender { get; set; }

		string MessageType { get; set; }

		bool SupressWarning { get; set; }
	}
	
	public static class MessageType
	{
		public const string Error = "Error";
		public const string Warning = "Warning";
		public const string Ok = "Ok";
	}
}
