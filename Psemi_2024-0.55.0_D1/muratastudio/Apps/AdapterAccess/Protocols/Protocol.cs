using AdapterAccess.Adapters;
using System;

namespace AdapterAccess.Protocols
{
	public abstract class Protocol : IDisposable
	{
		#region Member Variables

		protected string _name = "Generic Protocol";
		protected Adapter _adapter;
		protected bool _opened = false;
		private bool _disposed = false;

		#endregion Member Variables

		#region Constructor and Destructor

		public Protocol( Adapter adapter, string name )
		{
			_adapter = adapter;
			_name = name;
		}

		~Protocol()
		{
			Dispose(false);
		}

		#endregion Public Properties

		#region Public Properties

		public virtual string Name
		{
			get{ return _name; }
			set{ _name = value; }
		}

		public Adapter AttachedAdapter
		{
			get { return _adapter; }
		}

		public abstract Type GetInterfaceType();

		#endregion

		#region Access Members

		public virtual bool ResetAdapter()
		{
			return _adapter.Reset();
		}

		public abstract void SendByte(byte data);

		public abstract void WriteByte(uint address, byte data);

		public virtual void WriteWord(uint address, ushort data)
		{
			// Separate the WORD into byte array
			byte[] dataBytes = new byte[2];
			dataBytes[0] = (byte)data;
			dataBytes[1] = (byte)(data >> 8);

			WriteBlock(address, 2, ref dataBytes);
		}
		
		public abstract void WriteBlock(uint address, int length, ref byte[] data);

		public abstract byte ReadByte(uint address);

		public virtual ushort ReadWord(uint address)
		{
			// Read a 2 byte block
			byte[] dataBytes = new byte[2];
			ReadBlock(address, 2, ref dataBytes);

			return (ushort)(dataBytes[0] + ((UInt16)dataBytes[1] << 8));
		}

		public virtual ushort Read16Bit(uint address)
		{
			// Read a 2 byte block
			byte[] dataBytes = new byte[2];
			ReadBlock(address, 2, ref dataBytes);

			return (ushort)(dataBytes[1] + ((UInt16)dataBytes[0] << 8));
		}

		public abstract void ReadBlock(uint address, int length, ref byte[] data, bool isAdapterControl = false);

		#endregion Access Members

		#region Exclusive Access Methods

		public void GetExclusiveAccess(int msTimeout)
		{
			_adapter.GetExclusiveAccess(msTimeout);
		}

		public bool TryGetExclusiveAccess(int msTimeout)
		{
			return _adapter.TryGetExclusiveAccess(msTimeout);
		}

		public void ReleaseExclusiveAccess()
		{
			_adapter.ReleaseExclusiveAccess();
		}

		#endregion

		#region Close

		public void Close()
		{
			Dispose();
		}

		protected virtual void Dispose(bool disposing)
		{
			// Has the device already been disposed?
			if (!_disposed)
			{
				if (disposing)
				{
					// Dispose "managed" resources as well
					_adapter.RemoveProtocol(this);
				}

				// Release unmanaged resources
			}

			_disposed = true;
		}

		#endregion Close

		#region IDisposable Members

		public void Dispose()
		{
			Dispose(true);
			// Remove the object from the Finalization queue
			// to prevent finalization code for this object
			// from executing a second time
			GC.SuppressFinalize(this);
		}

		#endregion
	}
}
