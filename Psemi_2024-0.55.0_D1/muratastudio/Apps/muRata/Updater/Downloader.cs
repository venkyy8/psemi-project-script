using ShareFileV3;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace muRata.Updater
{
	public class Downloader
	{
		public delegate void MessageEventHandler(string message);
		public static event MessageEventHandler OnMessage;
		public static event BytesDownloadedEventHandler OnBytesDownloaded;

		public static OAuth2Token AuthenticateUser(UpdaterSettings settings)
		{
			var un = Hasher.Base64Decode(settings.U);
			var pw = Hasher.Base64Decode(settings.L);
			return ShareFileV3Sample.Authenticate(settings.H, settings.Id, settings.Key, un, pw);
		}

		public static byte[] DownloadFromWeb(string url)//, string file, string targetFolder)
		{
			try
			{
				byte[] downloadedData;

				downloadedData = new byte[0];

				// Open a data stream from the supplied URL
				//Uri uri = new Uri(new Uri(url), file);

				WebRequest webReq = WebRequest.Create(new Uri(url));
				//WebRequest webReq = WebRequest.Create(url);
				WebResponse webResponse = webReq.GetResponse();

				SendMessage("...Established connection to server!");
				
				Stream dataStream = webResponse.GetResponseStream();

				// Download the data in chuncks
				byte[] dataBuffer = new byte[1024];

				//G et the total size of the download
				int dataLength = (int)webResponse.ContentLength;

				// Lets declare our downloaded bytes event args
				ByteArgs byteArgs = new ByteArgs();

				byteArgs.downloaded = 0;
				byteArgs.total = dataLength;

				// We need to test for a null as if an event is not consumed we will get an exception
				if (OnBytesDownloaded != null)
				{
					OnBytesDownloaded(byteArgs);
				}

				// Download the data
				MemoryStream memoryStream = new MemoryStream();
				SendMessage("...Downloading update information");

				while (true)
				{
					// Let's try and read the data
					int bytesFromStream = dataStream.Read(dataBuffer, 0, dataBuffer.Length);

					if (bytesFromStream == 0)
					{

						byteArgs.downloaded = dataLength;
						byteArgs.total = dataLength;
						if (OnBytesDownloaded != null)
						{
							OnBytesDownloaded(byteArgs);
						}

						// Download complete
						SendMessage("...Download complete!");
						break;
					}
					else
					{
						// Write the downloaded data
						memoryStream.Write(dataBuffer, 0, bytesFromStream);

						byteArgs.downloaded = bytesFromStream;
						byteArgs.total = dataLength;
						if (OnBytesDownloaded != null)
						{
							OnBytesDownloaded(byteArgs);
						}
					}
				}

				// Convert the downloaded stream to a byte array
				downloadedData = memoryStream.ToArray();

				// Release resources
				dataStream.Close();
				memoryStream.Close();

				SendMessage("Connection closed!");

				// Write bytes to the specified file
				//FileStream newFile = new FileStream(Path.Combine(targetFolder, file), FileMode.Create);
				//newFile.Write(downloadedData, 0, downloadedData.Length);
				//newFile.Close();

				return downloadedData;
			}

			catch (Exception ex)
			{
				// We may not be connected to the internet
				// Or the URL may be incorrect
				SendMessage(ex.Message);
				throw ex;
			}
		}

		//public static byte[] DownloadFromServer(string folderId)
		//{
		//	string hostname = "arcticsand.sharefile.com";
		//	string username = "gui_update@arcticsand.com";
		//	string password = "!Updater1";
		//	string clientId = "YjN9adjGvMBnFl589ZObDbI4lf9AbnVx";
		//	string clientSecret = "QtdSd6bUFCuTFrYUlrPgRsEBQU9vDQPxAmthharQgDlFsHGp";

		//	SendMessage("...Established connection to server!");
		//	OAuth2Token token = ShareFileV3Sample.Authenticate(hostname, clientId, clientSecret, username, password);
		//	if (token != null)
		//	{
		//		SendMessage("...Downloading update information");
		//		var updateFileId = ShareFileV3Sample.GetFileIdFromFolderByFileName(token, folderId, "ASUpdate.xml");
		//		var bytes = ShareFileV3Sample.DownloadItem(token, updateFileId);
		//		Debug.Print(ASCIIEncoding.ASCII.GetString(bytes));
		//		return bytes;
		//	}
		//	else
		//	{
		//		SendMessage("Connection to server failed!");
		//		return null;
		//	}
		//}

		public static byte[] DownloadFromServer(OAuth2Token token, string folderId, string fileName, string hash = "", int length = 0)
		{
			byte[] bytes = null;
			var updateFileId = ShareFileV3Sample.GetFileIdFromFolderByFileName(token, folderId, fileName);
			if (updateFileId != null)
			{
				bytes = ShareFileV3Sample.DownloadItem(token, updateFileId);
				Debug.Print(ASCIIEncoding.ASCII.GetString(bytes));

				// If there is a provied hash then validate the data
				if (!string.IsNullOrEmpty(hash) && length != 0)
				{
					// Since the response stream is padded, we need to copy only the actual bytes in the file.
					byte[] b = new byte[length];
					Array.Copy(bytes, b, length);

					if (Hasher.HashFileBytes(b, HashType.MD5).ToUpper() != hash)
						throw new Exception("The downloaded file cannot be validated.");
				}
			}
			else
			{
				throw new Exception("File missing or permissions for this account may not be sufficent to download this file.");
			}
			return bytes;
		}

		private static void SendMessage(string message)
		{
			if (OnMessage != null)
			{
				OnMessage(message);
				Thread.Sleep(500);
			}
		}
	}
}
