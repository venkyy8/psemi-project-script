using System;

namespace AdapterAccess.Protocols
{
	/// <summary>
	/// Provides generic information about a specific protocol type.
	/// </summary>
	public class ProtocolInfo
	{
		#region Private Members

		private Type _protocolType;
		private string _name;
		private bool _genericAccess;
		private bool _deviceAccess; 

		#endregion

		#region Public Properties

		public Type ProtocolType
		{
			get
			{
				return _protocolType;
			}
		}

		public string Name
		{
			get
			{
				return _name;
			}
		}

		public bool GenericAccess
		{
			get
			{
				return _genericAccess;
			}
		}

		public bool DeviceAccess
		{
			get
			{
				return _deviceAccess;
			}
		} 

		#endregion

		#region Constructor

		public ProtocolInfo(Type protocolType, string name, bool genericAccess, bool deviceAccess)
		{
			_protocolType = protocolType;
			_name = name;
			_genericAccess = genericAccess;
			_deviceAccess = deviceAccess;
		} 

		#endregion
	}
}
