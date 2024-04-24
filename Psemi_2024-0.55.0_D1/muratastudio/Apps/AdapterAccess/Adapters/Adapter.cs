using AdapterAccess.Protocols;
using HardwareInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AdapterAccess.Adapters
{
	/// <summary>
	/// An abstract class that represents a generic adapter object.
	/// </summary>
	public abstract class Adapter : IDisposable
	{
		#region Member Variables

		/// <summary>
		/// Used to enforce exclusive access across multiple threads.
		/// </summary>
		private volatile Mutex _exclusiveAccess;

		/// <summary>
		/// Helps the "garbage collection" process know when to close the
		/// "unmanaged resource" associated with this object.
		/// </summary>
		private bool _disposed = false;

		/// <summary>
		/// Allows only one "attach/remove" activity to proceed at a time.
		/// </summary>
		private object _attachRemoveLock = new object();

		/// <summary>
		/// Indicates if the adapter has been opened in "shared" or "exclusive" mode.
		/// </summary>
		protected AdapterMode _accessMode;

		/// <summary>
		/// List of protocols that are supported by this adapter.
		/// </summary>
		protected List<ProtocolInfo> _supportedProtocols = new List<ProtocolInfo>();

		/// <summary>
		/// List of attached protocol objects.
		/// </summary>
		protected List<Protocol> _attachedProtocolObjects = new List<Protocol>();

		/// <summary>
		/// Used to know when it is safe to close the driver interface.
		/// </summary>
		private int _referenceCount = 0;

		/// <summary>
		/// Identifies the "state" of the adapter.
		/// </summary>
		protected AdapterState _state = AdapterState.NotAvailable;

		protected bool _isOpened = false;

		#endregion Member Variables

		#region Constructor and Destructor

		/// <summary>
		/// Default constructor.
		/// </summary>
		public Adapter()
		{
			_exclusiveAccess = new Mutex();
		}

		/// <summary>
		/// Destructor.
		/// </summary>
		~Adapter()
		{
			Dispose( false );
		}

		#endregion

		#region Public Properties

		public List<ProtocolInfo> SupportedProtocols
		{
			get { return _supportedProtocols; }
		}

		public List<Protocol> GetAttachedProtocolObjects()
		{
			return _attachedProtocolObjects;
		}

		public AdapterState State
		{
			get { return _state; }
			set { _state = value; }
		}

		public AdapterMode AccessMode
		{
			get { return _accessMode; }
		}

		public bool Is16Bit
		{
			get { return _is16Bit; }
			set { _is16Bit = value; }
		}
		public bool IsOpened
		{
			get { return _isOpened; }
			set { _isOpened = value; }
		}

		/// <summary>
		/// Unique index number for enumerated adapters.
		/// </summary>
		public int Index = 0;
        private bool _is16Bit;

        ///// <summary>
        ///// A name that represents the adapter.
        /////  Should be overridden in the derived class.
        ///// </summary>
        //public virtual string Name
        //{
        //	get { return "Interface Adapter"; }
        //}

        ///// <summary>
        ///// Returns a string that represents the adapter version.
        ///// Should be overridden in the derived class.
        ///// </summary>
        ///// <returns>Version string.</returns>
        //public virtual string Version
        //{
        //	get { return "Unknown version"; }
        //}

        ///// <summary>
        ///// Returns a string that represents the adapter serial number.
        ///// Should be overridden in the derived class.
        ///// </summary>
        ///// <returns>Serial number string.</returns>
        //public virtual string SerialNumber
        //{
        //	get { return "Unknown serial number"; }
        //}

        #endregion

        #region Exclusive Access Methods

        public bool Reset()
		{
			return ReconfigureInterface();
		}

		/// <summary>
		/// Tries to get exclusive access to the adapter. Throws an exception
		/// on a failure.
		/// </summary>
		/// <param name="msTimeout">Timeout value.</param>
		/// <exception cref="AdapterException">Thrown if unable to get exclusive access before the timeout.</exception>
		public void GetExclusiveAccess(int msTimeout)
		{
			if (!_exclusiveAccess.WaitOne(msTimeout, true))
				throw new AdapterException("Timeout waiting to acquire access to the interface.");
		}

		/// <summary>
		/// Tries to get exclusive access without throwing an exception on a
		/// failure.
		/// </summary>
		/// <param name="msTimeout">Timeout value.</param>
		/// <returns>True or false.</returns>
		public bool TryGetExclusiveAccess(int msTimeout)
		{
			return _exclusiveAccess.WaitOne(msTimeout, true);
		}

		/// <summary>
		/// Releases the exclusive access lock. The function may be called even if
		/// the thread didn't actually have exclusive access.
		/// </summary>
		public void ReleaseExclusiveAccess()
		{
			try
			{
				_exclusiveAccess.ReleaseMutex();
			}
			catch (ApplicationException)
			{
				// An ApplicationException indicates that the current thread didn't
				// actually have the Mutex. This isn't really a problem with the 
				// current architecture and may normally occur. This function will
				// be called as part of the normal exception handling which may have
				// called due to a failed attempt to get the mutex.

				// Don't do anything
			}
		}

		#endregion

		#region Open and Close

		/// <summary>
		/// Calls the Dispose function that is part of the IDisposable 
		/// interface.
		/// </summary>
		public void Close()
		{
			Dispose();
		}

		/// <summary>
		/// Releases the unmanaged resources when the object is 
		/// "garbage collected". Can be overridden in the derived
		/// class if necessary, but this version should also be called
		/// from that overridden function.
		/// </summary>
		/// <param name="disposing"></param>
		protected virtual void Dispose(bool disposing)
		{
			// Has the device already been disposed?
			if (!_disposed)
			{
				if (disposing)
				{
					// Dispose "managed" resources as well
				}

				// Release unmanaged resources
				CloseDriverInterface();
			}

			_disposed = true;
		}

		/// <summary>
		/// Opens the underlying driver interface for the adapter object. 
		/// Should be overridden in the derived class.
		/// </summary>
		/// <returns>True or false.</returns>
		protected virtual bool OpenDriverInterface()
		{
			return true;
		}

		/// <summary>
		/// Closes the underlying driver interface for the adapter object.
		/// Should be overridden in the derived class.
		/// </summary>
		protected virtual void CloseDriverInterface()
		{
			return;
		}

		protected virtual bool ReconfigureInterface()
		{
			return true;
		}

		#endregion

		#region Protocol and Interface Methods

		/// <summary>
		/// Attaches a new protocol object to the adapter and opens the driver
		/// interface if necessary.
		/// </summary>
		/// <param name="protocolObject">New protocol object.</param>
		/// <param name="accessMode">Shared or Reserved.</param>
		/// <returns>True or false.</returns>
		public bool AttachProtocol(Protocol protocolObject, AdapterMode accessMode)
		{
			bool result = false;

			lock (_attachRemoveLock)
			{
				if (!IsInterfaceSupported(protocolObject.GetInterfaceType()))
				{
					result = false;
				}
				else
				{
					switch (_state)
					{
						case AdapterState.NotAvailable:
							result = false;
							break;
						case AdapterState.Available:
							// Open the actual adapter
							result = OpenDriverInterface();
							break;
						case AdapterState.OpenShared:
							if (accessMode == AdapterMode.Shared)
								result = true;
							else
								result = false;
							break;
						case AdapterState.OpenReserved:
							result = false;
							break;
						case AdapterState.Removed:
							result = false;
							break;
					}
				}

				if (result == true)
				{
					_referenceCount++;

					_attachedProtocolObjects.Add(protocolObject);

					_accessMode = accessMode;
					if (accessMode == AdapterMode.Exclusive)
						_state = AdapterState.OpenReserved;
					else
						_state = AdapterState.OpenShared;
				}

			} // lock

			return result;
		}

		/// <summary>
		/// Removes the protocol object from the adapter and closes the
		/// driver interface if there aren't any other attached protocols.
		/// </summary>
		/// <param name="protocolObject">Protocol to remove.</param>
		public void RemoveProtocol(Protocol protocolObject)
		{
			lock (_attachRemoveLock)
			{
				_attachedProtocolObjects.Remove(protocolObject);

				if (_referenceCount > 0)
					_referenceCount--;

				if (_referenceCount == 0)
				{
					// Release driver interface
					CloseDriverInterface();

					_state = AdapterState.Available;
				}
			} // lock
		}

		/// <summary>
		/// Used to determine if a particulare interface type is supported.
		/// Must be overridden by the derived class.
		/// </summary>
		/// <param name="interfaceType">Interface type.</param>
		/// <returns>True or false.</returns>
		public abstract bool IsInterfaceSupported(Type interfaceType);

		#endregion Protocol and Interface Methods

		#region IDisposable Members
		//////////////////////////////////////////////////////////////////
		// IDisposable Members
		//////////////////////////////////////////////////////////////////

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

	/// <summary>
	/// Identifies the state of an Adapter object.
	/// </summary>
	public enum AdapterState
	{
		/// <summary>
		/// Adapter was not in use and has been removed.
		/// </summary>
		NotPresent,
		/// <summary>
		/// Adapter is in use by another application.
		/// </summary>
		NotAvailable,
		/// <summary>
		/// Adapter is available for use by this application.
		/// </summary>
		Available,
		/// <summary>
		/// Adapter is in use by this application in a "shared" mode.
		/// </summary>
		OpenShared,
		/// <summary>
		/// Adapter is in use by this application in a "reserved" mode.
		/// </summary>
		OpenReserved,
		/// <summary>
		/// Adapter was in use by this application but has been removed.
		/// </summary>
		Removed,
		/// <summary>
		/// Adapter was in use by this application, but after been reattached,
		/// it is now being used by another application.
		/// </summary>
		ReattachedNotAvailable,
		/// <summary>
		/// Adapter has been reattached and is waiting to be "resynced" with
		/// this application.
		/// </summary>
		Reattached
	}

	/// <summary>
	/// Shared or exclusive access.
	/// </summary>
	public enum AdapterMode
	{
		/// <summary>
		/// More than one application to use the adapter.
		/// </summary>
		Shared,
		/// <summary>
		/// Only one application can use the adapter.
		/// </summary>
		Exclusive
	}
}
