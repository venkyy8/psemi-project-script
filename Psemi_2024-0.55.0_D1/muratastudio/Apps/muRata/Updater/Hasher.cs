using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace muRata.Updater
{
	/// <summary>
	/// The type of hash to create
	/// </summary>
	internal enum HashType
	{
		MD5,
		SHA1,
		SHA512
	}

	/// <summary>
	/// Class used to generate hash sums of files
	/// </summary>
	internal static class Hasher
	{
		/// <summary>
		/// Generate a hash sum of a file
		/// </summary>
		/// <param name="filePath">The file to hash</param>
		/// <param name="algo">The Type of hash</param>
		/// <returns>The computed hash</returns>
		internal static string HashFile(string filePath, HashType algo)
		{
			switch (algo)
			{
				case HashType.MD5:
					return MakeHashString(MD5.Create().ComputeHash(new FileStream(filePath, FileMode.Open)));
				case HashType.SHA1:
					return MakeHashString(SHA1.Create().ComputeHash(new FileStream(filePath, FileMode.Open)));
				case HashType.SHA512:
					return MakeHashString(SHA512.Create().ComputeHash(new FileStream(filePath, FileMode.Open)));
				default:
					return "";
			}
		}

		/// <summary>
		/// Generate a hash sum of a file bytes.
		/// </summary>
		/// <param name="filePath">The file to hash</param>
		/// <param name="algo">The Type of hash</param>
		/// <returns>The computed hash</returns>
		internal static string HashFileBytes(byte[] fileStream, HashType algo)
		{
			switch (algo)
			{
				case HashType.MD5:
					return MakeHashString(MD5.Create().ComputeHash(fileStream));
				case HashType.SHA1:
					return MakeHashString(SHA1.Create().ComputeHash(fileStream));
				case HashType.SHA512:
					return MakeHashString(SHA512.Create().ComputeHash(fileStream));
				default:
					return "";
			}
		}

		/// <summary>
		/// Converts byte[] to string
		/// </summary>
		/// <param name="hash">The hash to convert</param>
		/// <returns>Hash as string</returns>
		private static string MakeHashString(byte[] hash)
		{
			StringBuilder s = new StringBuilder();

			foreach (byte b in hash)
				s.Append(b.ToString("x2").ToLower());

			return s.ToString();
		}

		internal static string Base64Encode(string plainText)
		{
			var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
			return System.Convert.ToBase64String(plainTextBytes);
		}

		internal static string Base64Decode(string base64EncodedData)
		{
			var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
			return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
		}
	}
}