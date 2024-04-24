using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace muRata.Updater
{
	[Serializable]
	public class UpdateInfo
	{
		public DateTime ReleaseDate { get; set; }

		public ApplicationData Application { get; set; }
	}

	[Serializable]
	public class ApplicationData
	{
		/// <summary>
		/// Name of the target application to update.
		/// </summary>
		[XmlAttribute]
		public string Name;

		/// <summary>
		/// Version of the updater application.
		/// </summary>
		[XmlAttribute]
		public string Version;

		/// <summary>
		/// Patch number for this release.
		/// </summary>
		[XmlAttribute]
		public string Patch;

		/// <summary>
		/// This update requires a full installation.
		/// </summary>
		[XmlAttribute]
		public bool RequiresInstall;

		/// <summary>
		/// Name of the update file package.
		/// </summary>
		public string File;

		/// <summary>
		/// The computed MD5 hash of the file.
		/// </summary>
		public string MD5;

		/// <summary>
		/// The computed length of the file.
		/// </summary>
		public string Length;
	}
}
