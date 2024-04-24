using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdapterAccess.Adapters
{
	/// <summary>
	/// Used to indicate errors associated with the adapter itself, but
	/// not with communication protocol specific errors.
	/// </summary>
	public class AdapterException : Exception
	{
		public AdapterException()
		{
			// Add implementation.
		}
		public AdapterException(string message)
			: base(message)
		{
		}

		public AdapterException(string message, Exception inner)
			: base(message, inner)
		{
		}
	}
}
