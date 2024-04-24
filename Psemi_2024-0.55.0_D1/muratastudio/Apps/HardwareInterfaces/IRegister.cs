using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace HardwareInterfaces
{
	public interface IRegister
	{
		#region Register Access

		/// <summary>
		/// Structure Containing the Register Information
		/// </summary>
		List<Register> Registers { get; }

		/// <summary>
		/// Returns a complete Register object specified by the Register ID if it is valid
		/// </summary>
		/// <param name="registerID">Register Identifier</param>
		/// <returns></returns>
		Register GetRegister(string registerID);

		/// <summary>
		/// Returns a complete Register Bit object specified by the Bit ID if it is valid
		/// </summary>
		/// <param name="bitID">Register Bit Identifier</param>
		/// <returns></returns>
		Register.Bit GetRegisterBit(string bitID);

		/// <summary>
		/// Returns a Boolean Indicating Whether the Specified Register ID is Valid in the Current Context
		/// </summary>
		/// <param name="registerID">Register Identifier</param>
		/// <returns></returns>
		bool isRegisterIDValid(string registerID);

		/// <summary>
		/// Returns a string Containing the Display Name Associated with a Specific Register
		/// </summary>
		/// <param name="registerID">Register Identifier</param>
		/// <returns></returns>
		string GetRegisterDisplayName(string registerID);

		/// <summary>
		/// Returns an Integer Value Containing the Address Associated with a Specific Register
		/// </summary>
		/// <param name="registerID">Register Identifier</param>
		/// <returns></returns>
		uint GetRegisterAddress(string registerID);

		/// <summary>
		/// Returns an string Containing the Data Type Associated with a Specific Register
		/// </summary>
		/// <param name="registerID">Register Identifier</param>
		/// <returns></returns>
		string GetRegisterDataType(string registerID);

		/// <summary>
		/// Returns an int Containing the position of the signed bit Associated with a Specific Register
		/// </summary>
		/// <param name="registerID">Register Identifier</param>
		/// <returns></returns>
		int GetRegisterSignedBit(string registerID);

		/// <summary>
		/// Returns an string Containing the read write access type Associated with a Specific Register
		/// </summary>
		/// <param name="registerID">Register Identifier</param>
		/// <returns></returns>
		string GetRegisterAccess(string registerID);

		/// <summary>
		/// Returns an Integer Value Containing the Size Associated with a Specific Register
		/// </summary>
		/// <param name="registerID">Register Identifier</param>
		/// <returns></returns>
		uint GetRegisterSize(string registerID);

		/// <summary>
		/// Returns a string Containing the Description Associated with a Specific Register
		/// </summary>
		/// <param name="registerID">Register Identifier</param>
		/// <returns></returns>
		string GetRegisterDescription(string registerID);

		/// <summary>
		/// Returns a string Containing the Format Associated with a Specific Register
		/// </summary>
		/// <param name="registerID">Register Identifier</param>
		/// <returns></returns>
		string GetRegisterFormat(string registerID);

		/// <summary>
		/// Returns a string Containing the Unit Associated with a Specific Register
		/// </summary>
		/// <param name="registerID">Register Identifier</param>
		/// <returns></returns>
		string GetRegisterUnit(string registerID);

		/// <summary>
		/// Returns a string Containing the Load Formula Associated with a Specific Register
		/// </summary>
		/// <param name="registerID">Register Identifier</param>
		/// <returns></returns>
		string GetRegisterLoadFormula(string registerID);

		/// <summary>
		/// Returns a string Containing the Store Formula Associated with a Specific Register
		/// </summary>
		/// <param name="registerID">Register Identifier</param>
		/// <returns></returns>
		string GetRegisterStoreFormula(string registerID);

		/// <summary>
		/// Populates a specified object with a raw value from a Register and returns an indication of success
		/// </summary>
		/// <param name="register">Register structure corresponding to the Register to be accessed</param>
		/// <param name="value">Reference to the object that receives the raw data</param>
		/// <returns>TRUE on successful read, otherwise FALSE</returns>
		bool ReadRegister(Register register, ref object value);

		/// <summary>
		/// Populates a specified object with a raw value from a Register and returns an indication of success
		/// </summary>
		/// <param name="registerID">Register Identifier</param>
		/// <param name="value">Reference to the object that receives the raw data</param>
		/// <returns>TRUE on successful read, otherwise FALSE</returns>
		bool ReadRegister(string registerID, ref object value);

		/// <summary>
		/// Populates a specified object with a raw value from a Register and returns an indication of success
		/// </summary>
		/// <param name="address">Address of register</param>
		/// <param name="value">Reference to the object that receives the raw data</param>
		/// <returns>TRUE on successful read, otherwise FALSE</returns>
		bool ReadRegister(uint address, ref object value);

		/// <summary>
		/// Returns a raw value from a Register specified by the Register object
		/// </summary>
		/// <param name="register">Register structure corresponding to the Register to be accessed</param>
		/// <returns></returns>
		object ReadRegister(Register register);

		/// <summary>
		/// Returns a raw value from a Register specified by the Register ID bit
		/// </summary>
		/// <param name="registerID">Register Identifier</param>
		/// <returns></returns>
		object ReadRegister(string registerID);

		/// <summary>
		/// Returns a raw value from a Register specified by the Register ID bit
		/// </summary>
		/// <param name="address">Register address</param>
		/// <returns></returns>
		object ReadRegister(uint address);

		/// <summary>
		/// Writes a raw value to a Register specified by the Register object
		/// </summary>
		/// <param name="register">Register structure corresponding to the Register to be accessed</param>
		/// <param name="value">Value to be written to the Register</param>
		/// <returns>TRUE on successful write, otherwise FALSE</returns>
		bool WriteRegister(Register register, object value);

		bool WriteRegister(Register register, string protocolType, string readCount, byte[] data);
		/// <summary>
		/// Writes a raw value to a Register specified by the Register ID bit
		/// </summary>
		/// <param name="registerID">Register Identifier</param>
		/// <param name="value">Value to be written to the Register</param>
		/// <returns>TRUE on successful write, otherwise FALSE</returns>
		bool WriteRegister(string registerID, object value);

		/// <summary>
		/// Writes a raw value to a Register specified by the Register object
		/// </summary>
		/// <param name="address">The address of a register</param>
		/// <param name="value">Value to be written to the Register</param>
		/// <returns>TRUE on successful write, otherwise FALSE</returns>
		bool WriteRegister(uint address, object value);

		/// <summary>
		/// Populates a specified value with a Register value from a Register and returns an indication of success
		/// </summary>
		/// <param name="register">Register structure corresponding to the Register to be accessed</param>
		/// <param name="value">Reference to the value that receives the data</param>
		/// <returns>TRUE on successful read, otherwise FALSE</returns>
		bool ReadRegisterValue(Register register, ref double value);

		bool ReadRegisterValue(Register register, ref double value, int readCount, bool isAdapterControl = false);

		/// <summary>
		/// Populates a specified value with a Register value from a Register and returns an indication of success
		/// </summary>
		/// <param name="registerID">Register Identifier</param>
		/// <param name="value">Reference to the value that receives the data</param>
		/// <returns>TRUE on successful read, otherwise FALSE</returns>
		bool ReadRegisterValue(string registerID, ref double value);

		/// <summary>
		/// Returns a Register value from a Register specified by the Register object
		/// </summary>
		/// <param name="register">Register structure corresponding to the Register to be accessed</param>
		/// <returns></returns>
		double ReadRegisterValue(Register register);

		double ReadRegisterValue(Register register, int readCount, bool isAdapterControl = false);
		/// <summary>
		/// Returns a Register value from a Register specified by the Register ID bit
		/// </summary>
		/// <param name="registerID">Register Identifier</param>
		/// <returns></returns>
		double ReadRegisterValue(string registerID);

		/// <summary>
		/// Writes a Register value to a Register specified by the Register object and returns an indication of success
		/// </summary>
		/// <param name="register">Register structure corresponding to the Register to be accessed</param>
		/// <param name="value">Value to be written to the Register</param>
		/// <returns>TRUE on successful write, otherwise FALSE</returns>
		bool WriteRegisterValue(Register register, double value);

		/// <summary>
		/// Writes a Register value to a Register specified by the Register ID bit and returns an indication of success
		/// </summary>
		/// <param name="registerID">Register Identifier</param>
		/// <param name="value">Value to be written to the Register</param>
		/// <returns>TRUE on successful write, otherwise FALSE</returns>
		bool WriteRegisterValue(string registerID, double value);

		/// <summary>
		/// Populates a specified value with a 32-bit value read from the Bit within a Register Specified by the Bit object and returns an indication of success (Masking and Shifting Handled Automatically) 
		/// </summary>
		/// <param name="bit">Register Bit structure corresponding to the Bit to be accessed</param>
		/// <param name="value">Reference to the 32-bit value that receives the data</param>
		/// <returns>TRUE on successful read, otherwise FALSE</returns>
		bool ReadRegisterBit(Register.Bit bit, ref uint value);

		/// <summary>
		/// Populates a specified value with a 32-bit value read from the Bit within a Register Bit ID bit and returns an indication of success (Masking and Shifting Handled Automatically) 
		/// </summary>
		/// <param name="bitID">Register Bit Identifier</param>
		/// <param name="value">Reference to the 32-bit value that receives the data</param>
		/// <returns>TRUE on successful read, otherwise FALSE</returns>
		bool ReadRegisterBit(string bitID, ref uint value);

		/// <summary>
		/// Returns a 32-bit value read from the Bit within a Register Specified by the Bit object (Masking and Shifting Handled Automatically)
		/// </summary>
		/// <param name="bit">Register Bit structure corresponding to the Bit to be accessed</param>
		/// <returns></returns>
		uint ReadRegisterBit(Register.Bit bit);

		/// <summary>
		/// Returns a 32-bit value read from the Bit within a Register Specified by the Bit ID bit (Masking and Shifting Handled Automatically)
		/// </summary>
		/// <param name="bitID">Register Bit Identifier</param>
		/// <returns></returns>
		uint ReadRegisterBit(string bitID);

		/// <summary>
		/// Writes a 32-bit value to the Bit within a Register Specified by the Bit object and returns an indication of success (Masking and Shifting Handled Automatically)
		/// </summary>
		/// <param name="bit">Register Bit structure corresponding to the Bit to be accessed</param>
		/// <param name="value">Value to be written to the Register Bit</param>
		/// <returns>TRUE on successful write, otherwise FALSE</returns>
		bool WriteRegisterBit(Register.Bit bit, uint value);

		/// <summary>
		/// Writes a 32-bit value to the Bit within a Register Specified by the Bit ID bit and returns an indication of success (Masking and Shifting Handled Automatically)
		/// </summary>
		/// <param name="bitID">Register Bit Identifier</param>
		/// <param name="value">Value to be written to the Register Bit</param>
		/// <returns>TRUE on successful write, otherwise FALSE</returns>
		bool WriteRegisterBit(string bitID, uint value);

		/// <summary>
		/// Loads register map.
		/// </summary>
		/// <param name="map">object containing the addresses and values to write.</param>
		/// <returns>Status, true is successful.</returns>
		Task<bool> LoadRegisters(object map);

		/// <summary>
		/// Reads and creates an object that can be saved.
		/// </summary>
		/// <returns>Object containing the mapped data.</returns>
		Task<object> CreateRegisterMap();
        Register GetRegisters(string regId);

        #endregion
    }
}
