using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceAccess.Interfaces
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
		Boolean isRegisterIDValid(string registerID);

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
		Boolean ReadRegister(Register register, ref object value);

		/// <summary>
		/// Populates a specified object with a raw value from a Register and returns an indication of success
		/// </summary>
		/// <param name="registerID">Register Identifier</param>
		/// <param name="value">Reference to the object that receives the raw data</param>
		/// <returns>TRUE on successful read, otherwise FALSE</returns>
		Boolean ReadRegister(string registerID, ref object value);

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
		/// Writes a raw value to a Register specified by the Register object
		/// </summary>
		/// <param name="register">Register structure corresponding to the Register to be accessed</param>
		/// <param name="value">Value to be written to the Register</param>
		/// <returns>TRUE on successful write, otherwise FALSE</returns>
		Boolean WriteRegister(Register register, object value);

		/// <summary>
		/// Writes a raw value to a Register specified by the Register ID bit
		/// </summary>
		/// <param name="registerID">Register Identifier</param>
		/// <param name="value">Value to be written to the Register</param>
		/// <returns>TRUE on successful write, otherwise FALSE</returns>
		Boolean WriteRegister(string registerID, object value);

		/// <summary>
		/// Populates a specified value with a Register value from a Register and returns an indication of success
		/// </summary>
		/// <param name="register">Register structure corresponding to the Register to be accessed</param>
		/// <param name="value">Reference to the value that receives the data</param>
		/// <returns>TRUE on successful read, otherwise FALSE</returns>
		Boolean ReadRegisterValue(Register register, ref Double value);

		/// <summary>
		/// Populates a specified value with a Register value from a Register and returns an indication of success
		/// </summary>
		/// <param name="registerID">Register Identifier</param>
		/// <param name="value">Reference to the value that receives the data</param>
		/// <returns>TRUE on successful read, otherwise FALSE</returns>
		Boolean ReadRegisterValue(string registerID, ref Double value);

		/// <summary>
		/// Returns a Register value from a Register specified by the Register object
		/// </summary>
		/// <param name="register">Register structure corresponding to the Register to be accessed</param>
		/// <returns></returns>
		Double ReadRegisterValue(Register register);

		/// <summary>
		/// Returns a Register value from a Register specified by the Register ID bit
		/// </summary>
		/// <param name="registerID">Register Identifier</param>
		/// <returns></returns>
		Double ReadRegisterValue(string registerID);

		/// <summary>
		/// Writes a Register value to a Register specified by the Register object and returns an indication of success
		/// </summary>
		/// <param name="register">Register structure corresponding to the Register to be accessed</param>
		/// <param name="value">Value to be written to the Register</param>
		/// <returns>TRUE on successful write, otherwise FALSE</returns>
		Boolean WriteRegisterValue(Register register, Double value);

		/// <summary>
		/// Writes a Register value to a Register specified by the Register ID bit and returns an indication of success
		/// </summary>
		/// <param name="registerID">Register Identifier</param>
		/// <param name="value">Value to be written to the Register</param>
		/// <returns>TRUE on successful write, otherwise FALSE</returns>
		Boolean WriteRegisterValue(string registerID, Double value);

		/// <summary>
		/// Populates a specified value with a 32-bit value read from the Bit within a Register Specified by the Bit object and returns an indication of success (Masking and Shifting Handled Automatically) 
		/// </summary>
		/// <param name="bit">Register Bit structure corresponding to the Bit to be accessed</param>
		/// <param name="value">Reference to the 32-bit value that receives the data</param>
		/// <returns>TRUE on successful read, otherwise FALSE</returns>
		Boolean ReadRegisterBit(Register.Bit bit, ref uint value);

		/// <summary>
		/// Populates a specified value with a 32-bit value read from the Bit within a Register Bit ID bit and returns an indication of success (Masking and Shifting Handled Automatically) 
		/// </summary>
		/// <param name="bitID">Register Bit Identifier</param>
		/// <param name="value">Reference to the 32-bit value that receives the data</param>
		/// <returns>TRUE on successful read, otherwise FALSE</returns>
		Boolean ReadRegisterBit(string bitID, ref uint value);

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
		Boolean WriteRegisterBit(Register.Bit bit, uint value);

		/// <summary>
		/// Writes a 32-bit value to the Bit within a Register Specified by the Bit ID bit and returns an indication of success (Masking and Shifting Handled Automatically)
		/// </summary>
		/// <param name="bitID">Register Bit Identifier</param>
		/// <param name="value">Value to be written to the Register Bit</param>
		/// <returns>TRUE on successful write, otherwise FALSE</returns>
		Boolean WriteRegisterBit(string bitID, uint value);

		#endregion
	}
}
