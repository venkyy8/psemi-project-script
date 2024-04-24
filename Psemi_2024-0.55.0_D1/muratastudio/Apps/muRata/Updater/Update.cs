using HardwareInterfaces;
using ShareFileV3;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace muRata.Updater
{
	public class Update
	{
		public static OAuth2Token AuthenticateUser(UpdaterSettings settings)
		{
			return Downloader.AuthenticateUser(settings);
		}

		public static Dictionary<string, string> GetFolders(OAuth2Token token, string id)
		{
			return ShareFileV3Sample.GetFolderWithQueryParameters(token, id);
		}

		public static UpdateInfo GetUpdateInfo(OAuth2Token token, string url)
		{
			// Let's try and download update information from the web
			byte[] updateData = Downloader.DownloadFromServer(token, url, "ASUpdate.xml");

			if (updateData == null)
			{
				return null;
			}

			// If the download of the file was successful
			if (updateData.Length > 0)
			{
				// Get information out of download info file
				//return DeserializeUpdateInfo(file, targetFolder);
				return DeserializeUpdateStream(new MemoryStream(updateData));
			}
			// There is a chance that the download of the file was not successful
			else
			{
				return null;
			}
		}

		public static void InstallUpdateRestart(UpdaterSettings settings, UpdateInfo ui, string folderId)
		{
			// Build the command line for the updater application
			StringBuilder sb = new StringBuilder();
			sb.Append(string.Format("appName|{0},", System.Diagnostics.Process.GetCurrentProcess().ProcessName));
			sb.Append(string.Format("serverFolderId|{0},", folderId));
			sb.Append(string.Format("serverFileName|{0},", ui.Application.File));
			sb.Append(string.Format("serverFileMd5|{0},", ui.Application.MD5));
			sb.Append(string.Format("serverFileLength|{0},", ui.Application.Length));
			sb.Append(string.Format("serverHost|{0},", settings.H));
			sb.Append(string.Format("appId|{0},", settings.Id));
			sb.Append(string.Format("appKey|{0},", settings.Key));
			sb.Append(string.Format("hash1|{0},", settings.U));
			sb.Append(string.Format("hash2|{0},", settings.L));
			sb.Append(string.Format("hash3|{0},", settings.GU));
			sb.Append(string.Format("hash4|{0},", settings.GL));
			sb.Append(string.Format("install|{0},", ui.Application.RequiresInstall));

			ProcessStartInfo startInfo = new ProcessStartInfo();
			startInfo.FileName = FileUtilities.UpdaterPath;
			startInfo.Arguments = sb.ToString();
			startInfo.Verb = "runas";
			Process.Start(startInfo);
		}

		/// <summary>Updates the update application by renaming prefixed files</summary>
		/// <param name="updaterPrefix">Prefix of files to be renamed</param>
		/// <param name="containingFolder">Folder on the local machine where the prefixed files exist</param>
		/// <returns>Void</returns>
		public static void UpdateUtility(string updaterPrefix, string containingFolder)
		{
			try
			{
				DirectoryInfo dInfo = new DirectoryInfo(containingFolder);
				FileInfo[] updaterFiles = dInfo.GetFiles(updaterPrefix + "*");
				int fileCount = updaterFiles.Length;

				foreach (FileInfo file in updaterFiles)
				{
					string newFile = containingFolder + @"\" + file.Name;
					string origFile = containingFolder + @"\" + file.Name.Substring(updaterPrefix.Length, file.Name.Length - updaterPrefix.Length);

					if (File.Exists(origFile))
					{
						File.Delete(origFile);
					}

					File.Move(newFile, origFile);
				}
			}
			catch (Exception)
			{
				throw;
			}
		}

		public static UpdateInfo DeserializeUpdateInfo(string file, string targetFolder)
		{
			UpdateInfo updateInfo = null;
			FileStream fs = new FileStream(Path.Combine(targetFolder, file), FileMode.Open);
			XmlSerializer formatter = new XmlSerializer(typeof(UpdateInfo), new Type[] { typeof(ApplicationData) });
			try
			{
				updateInfo = (UpdateInfo)formatter.Deserialize(fs);
			}
			catch (InvalidOperationException e)
			{
				updateInfo = null;
				throw new InvalidOperationException("Failed to deserialize update information.\r\nReason: " + e.Message);
			}
			finally
			{
				fs.Close();
			}

			return updateInfo;
		}

		public static UpdateInfo DeserializeUpdateStream(Stream stream)
		{
			UpdateInfo updateInfo = null;
			XmlSerializer formatter = new XmlSerializer(typeof(UpdateInfo), new Type[] { typeof(ApplicationData) });
			try
			{
				updateInfo = (UpdateInfo)formatter.Deserialize(stream);
			}
			catch (InvalidOperationException e)
			{
				updateInfo = null;
				throw new InvalidOperationException("Failed to deserialize update information.\r\nReason: " + e.Message);
			}
			finally
			{
				stream.Close();
			}

			return updateInfo;
		}

		public static void SerializeUpdateInfo(string fileName, UpdateInfo updateInfo)
		{
			FileStream fs = new FileStream(fileName, FileMode.Create);
			XmlSerializer formatter = new XmlSerializer(typeof(UpdateInfo), new Type[] { typeof(ApplicationData) });
			try
			{
				formatter.Serialize(fs, updateInfo);
			}
			catch (InvalidOperationException e)
			{
				throw new InvalidOperationException("Failed to serialize register map.\r\nReason: " + e.Message);
			}
			finally
			{
				fs.Close();
			}
		}
	}
}
