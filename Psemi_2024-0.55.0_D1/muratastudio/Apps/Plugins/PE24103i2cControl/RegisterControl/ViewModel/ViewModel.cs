using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using HardwareInterfaces;
using PluginFramework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime;
using System.ComponentModel;
using GalaSoft.MvvmLight.Command;
using System.Diagnostics;
using System.Timers;
using System.Collections;
using DeviceAccess;

namespace PE24103i2cControl.RegisterControl
{
	public class ViewModel : ViewModelBase
	{
		#region Private Members

		private SlaveDevice _d;
		private IDevice _device;
		private IRegister _register;
		private bool _isInternalMode;
		private ObservableCollection<Register> _registers;
		private RelayCommand<Register.Bit> _toggleBitCommand;
		private RelayCommand<object> _writeRegisterCommand;
		public bool _loaded;
		private CustomizeBase _deviceBase;

		#endregion

		#region Properties

		public ObservableCollection<Register> Registers
		{
			get { return _registers; }
		}

		#endregion

		#region Constructors

		public ViewModel()
		{

		}

		public ViewModel(object device, bool isInternalMode)
		{
			_d = device as SlaveDevice;
			_device = device as IDevice;
			_register = device as IRegister;
			_isInternalMode = isInternalMode;
			_registers = new ObservableCollection<Register>();
			_register.Registers.ForEach(r => _registers.Add(r));

			// Get the name of the device without any containing dashes
			string deviceName = _device.DeviceInfoName.Replace("-", "");

			// Get a reference to an available type with the same name.
			var customizer = Type.GetType("DeviceAccess." + deviceName + ",DeviceAccess");

			// If found, create an instance of the customizing class and perform its base actions
			if (customizer != null)
			{
				try
				{
					// TODO - Return the actual part number and not the masters PN
					//_deviceBase = Activator.CreateInstance(customizer, device) as CustomizeBase;
					_deviceBase = null;
				}
				catch (Exception)
				{
					throw;
				}
			}

			Messenger.Default.Register<NotificationMessage>(this, HandleNotification);
		}

		#endregion

		#region Commands

		/// <summary>
		/// Gets the ToggleBitCommand.
		/// </summary>
		public RelayCommand<Register.Bit> ToggleBitCommand
		{
			get
			{
				return _toggleBitCommand
					?? (_toggleBitCommand = new RelayCommand<Register.Bit>(ExecuteToggleBitCommand));
			}
		}

		public RelayCommand<object> WriteRegisterCommand
		{
			get
			{
				return _writeRegisterCommand
					?? (_writeRegisterCommand = new RelayCommand<object>(ExecuteWriteRegisterCommand));
			}
		} 

		#endregion

		#region Public Methods

		public override void Cleanup()
		{
			base.Cleanup();
		} 

		#endregion

		#region Private Methods

		private void ExecuteWriteRegisterCommand(object value)
		{
			ArrayList info = value as ArrayList;
			string id = (string)info[0];
			int val = (int)info[1];

			Register reg = _register.GetRegister(id);

			if (_loaded)
			{
				try
				{
					if (reg.LastReadValue != (double)val)
					{
						reg.LastReadValue = val; // Small hack to change the binding so if the write fails the drop down selection will change back.
						_register.WriteRegister(reg, val);
						_register.ReadRegisterValue(reg);

						if (_deviceBase != null)
						{
							_deviceBase.CheckDevice(null);
						}
					}
				}
				catch (Exception ex)
				{
					MessengerSend(ex);
				}
			}
		}

		private void ExecuteToggleBitCommand(Register.Bit bit)
		{
			Register reg = _register.GetRegister(bit.RegisterID);
			int x = (((int)reg.LastReadValue & (int)bit.Mask) == (int)bit.Mask) ? 0 : 1;

			try
			{
				_register.WriteRegisterBit(bit, (uint)x);
				
				_register.ReadRegisterValue(reg);

				// Check the device for errors
				if (_deviceBase != null)
				{
					_deviceBase.CheckDevice(null);
				}
			}
			catch (Exception ex)
			{
				MessengerSend(ex);
			}
		}

		private void MessengerSend(Exception ex)
		{
			if (ex.InnerException != null)
				Debug.Print("Error: " + ex.InnerException.Message + " " + ex.Message);
			else
				Debug.Print("Error: " + ex.Message);
			Messenger.Default.Send(new CommunicationMessage(this, ex));
		}

		#endregion

		#region Event Handling
		
		private void HandleNotification(NotificationMessage message)
		{
			if (message.Notification == Notifications.CleanupNotification)
			{
				Cleanup();
				Messenger.Default.Unregister(this);
			}
		}

		#endregion
	}
}
