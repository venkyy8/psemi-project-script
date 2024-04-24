using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.IO;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Diagnostics;

/**
 * Copyright (c) 2014 Citrix Systems, Inc.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a
 * copy of this software and associated documentation files (the "Software"),
 * to deal in the Software without restriction, including without limitation
 * the rights to use, copy, modify, merge, publish, distribute, sublicense,
 * and/or sell copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
 * IN THE SOFTWARE.
 */

/**
 * The methods in this file will make use of the ShareFile API v3 to show some of the basic
 * operations using GET, POST, PATCH, DELETE HTTP verbs. See api.sharefile.com for more information.
 *
 * Requirements:
 *
 * Json.NET library. see http://json.codeplex.com
 *
 * JSON Deserialization:
 * 
 * The sample methods here simply use the JObject data accessors, rather than deserializing to a ShareFile Class representation.
 *
 * Authentication:
 *
 * OAuth2 password grant is used for authentication. After the token is acquired it is sent an an
 * authorization header with subsequent API requests.
 *
 * Exception / Error Checking:
 *
 * For simplicity, exception handling has not been added.  Code should not be used in a production environment.
 */
namespace ShareFileV3
{
	public class OAuth2Token
	{
		public string AccessToken { get; set; }
		public string RefreshToken { get; set; }
		public string TokenType { get; set; }
		public string Appcp { get; set; }
		public string Apicp { get; set; }
		public string Subdomain { get; set; }
		public int ExpiresIn { get; set; }

		public OAuth2Token(JObject json)
		{
			if (json != null)
			{
				AccessToken = (string)json["access_token"];
				RefreshToken = (string)json["refresh_token"];
				TokenType = (string)json["token_type"];
				Appcp = (string)json["appcp"];
				Apicp = (string)json["apicp"];
				Subdomain = (string)json["subdomain"];
				ExpiresIn = (int)json["expires_in"];
			}
			else
			{
				AccessToken = "";
				RefreshToken = "";
				TokenType = "";
				Appcp = "";
				Apicp = "";
				Subdomain = "";
				ExpiresIn = 0;
			}
		}
	}

	public class ShareFileV3Sample
	{

		/// <summary>
		/// Authenticate via username/password
		/// </summary>
		/// <param name="hostname">hostname like "myaccount.sharefile.com"</param>
		/// <param name="clientId">my client id</param>
		/// <param name="clientSecret">my client secret</param>
		/// <param name="username">my@user.name</param>
		/// <param name="password">mypassword</param>
		/// <returns></returns>
		public static OAuth2Token Authenticate(string hostname, string clientId, string clientSecret, string username, string password)
		{
			String uri = string.Format("https://{0}/oauth/token", hostname);
			Debug.WriteLine(uri);

			Dictionary<string, string> parameters = new Dictionary<string, string>();
			parameters.Add("grant_type", "password");
			parameters.Add("client_id", clientId);
			parameters.Add("client_secret", clientSecret);
			parameters.Add("username", username);
			parameters.Add("password", password);

			ArrayList bodyParameters = new ArrayList();
			foreach (KeyValuePair<string, string> kv in parameters)
			{
				bodyParameters.Add(string.Format("{0}={1}", HttpUtility.UrlEncode(kv.Key), HttpUtility.UrlEncode(kv.Value.ToString())));
			}
			string requestBody = String.Join("&", bodyParameters.ToArray());

			HttpWebRequest request = WebRequest.CreateHttp(uri);
			request.Method = "POST";
			request.ContentType = "application/x-www-form-urlencoded";
			request.Timeout = 30000;
			using (var writer = new StreamWriter(request.GetRequestStream()))
			{
				writer.Write(requestBody);
			}

			HttpWebResponse response = (HttpWebResponse)request.GetResponse();
			Debug.WriteLine(response.StatusCode);
			JObject token = null;
			using (var reader = new StreamReader(response.GetResponseStream()))
			{
				String body = reader.ReadToEnd();
				token = JObject.Parse(body);
			}

			return new OAuth2Token(token);
		}

		public static void addAuthorizationHeader(HttpWebRequest request, OAuth2Token token)
		{
			request.Headers.Add(string.Format("Authorization: Bearer {0}", token.AccessToken));
		}

		public static string GetHostname(OAuth2Token token)
		{
			return string.Format("{0}.sf-api.com", token.Subdomain);
		}

		/// <summary>
		/// Get the root level Item for the provided user. To retrieve Children the $expand=Children
		/// parameter can be added.
		/// </summary>
		/// <param name="token">the OAuth2Token returned from Authenticate</param>
		/// <param name="getChildren">retrieve Children Items if true, default is false</param>
		public static void GetRoot(OAuth2Token token, bool getChildren = false)
		{
			String uri = string.Format("https://{0}/sf/v3/Items", ShareFileV3Sample.GetHostname(token));
			if (getChildren)
			{
				uri += "?$expand=Children";
			}
			Debug.WriteLine(uri);

			HttpWebRequest request = WebRequest.CreateHttp(uri);
			ShareFileV3Sample.addAuthorizationHeader(request, token);

			HttpWebResponse response = (HttpWebResponse)request.GetResponse();
			Debug.WriteLine(response.StatusCode);
			using (var reader = new StreamReader(response.GetResponseStream()))
			{
				String body = reader.ReadToEnd();

				JObject root = JObject.Parse(body);

				// just print Id, CreationDate, Name of each element
				Debug.WriteLine(root["Id"] + " " + root["CreationDate"] + " " + root["Name"]);
				JArray children = (JArray)root["Children"];
				if (children != null)
				{
					foreach (JObject child in children)
					{
						Debug.WriteLine(child["Id"] + " " + child["CreationDate"] + " " + child["Name"]);
					}
				}
			}
		}

		/// <summary>
		/// Get a single Item by Id.
		/// </summary>
		/// <param name="token">the OAuth2Token returned from Authenticate</param>
		/// <param name="id">an item id</param>
		public static void GetItemById(OAuth2Token token, string id)
		{
			String uri = string.Format("https://{0}/sf/v3/Items({1})", ShareFileV3Sample.GetHostname(token), id);
			Debug.WriteLine(uri);

			HttpWebRequest request = WebRequest.CreateHttp(uri);
			ShareFileV3Sample.addAuthorizationHeader(request, token);

			HttpWebResponse response = (HttpWebResponse)request.GetResponse();
			Debug.WriteLine(response.StatusCode);
			using (var reader = new StreamReader(response.GetResponseStream()))
			{
				String body = reader.ReadToEnd();

				JObject item = JObject.Parse(body);
				Debug.WriteLine(item["Id"] + " " + item["CreationDate"] + " " + item["Name"]);
			}
		}

		/// <summary>
		/// Get a folder using some of the common query parameters that are available. This will
		/// add the expand, select parameters. The following are used:
		/// expand=Children to get any Children of the folder
		/// select=Id,Name,Children/Id,Children/Name,Children/CreationDate to get the Id, Name of the folder and the Id, Name, CreationDate of any Children
		/// </summary>
		/// <param name="token">the OAuth2Token returned from Authenticate</param>
		/// <param name="id">a folder id</param>
		public static Dictionary<string, string> GetFolderWithQueryParameters(OAuth2Token token, string id)
		{
			var ret = new Dictionary<string, string>();
			String uri = string.Format("https://{0}/sf/v3/Items({1})?$expand=Children&$select=Id,Name,Children/Id,Children/Name,Children/CreationDate", ShareFileV3Sample.GetHostname(token), id);
			Debug.WriteLine(uri);

			HttpWebRequest request = WebRequest.CreateHttp(uri);
			request.Timeout = 30000;
			ShareFileV3Sample.addAuthorizationHeader(request, token);

			HttpWebResponse response = (HttpWebResponse)request.GetResponse();
			Debug.WriteLine(response.StatusCode);
			using (var reader = new StreamReader(response.GetResponseStream()))
			{
				String body = reader.ReadToEnd();

				JObject folder = JObject.Parse(body);
				// only Id and Name are available because we specifically selected only those two Properties
				Debug.WriteLine(folder["Id"] + " " + folder["Name"]);
				JArray children = (JArray)folder["Children"];
				if (children != null)
				{
					foreach (JObject child in children)
					{
						// CreationDate is also available on Children because we specifically selected that property in addition to Id, Name
						Debug.WriteLine(child["Id"] + " " + child["CreationDate"] + " " + child["Name"]);
						ret.Add(child["Id"].ToString(), child["Name"].ToString());
					}
				}
			}

			return ret;
		}

		public static string GetFileIdFromFolderByFileName(OAuth2Token token, string folderId, string fileName)
		{
			string returnFileId = string.Empty;
			String uri = string.Format("https://{0}/sf/v3/Items({1})?$expand=Children&$select=Id,Name,Children/Id,Children/Name,Children/CreationDate",
				ShareFileV3Sample.GetHostname(token), folderId);
			Debug.WriteLine(uri);

			HttpWebRequest request = WebRequest.CreateHttp(uri);
			request.Timeout = 30000;
			ShareFileV3Sample.addAuthorizationHeader(request, token);

			HttpWebResponse response = (HttpWebResponse)request.GetResponse();
			Debug.WriteLine(response.StatusCode);
			using (var reader = new StreamReader(response.GetResponseStream()))
			{
				String body = reader.ReadToEnd();

				JObject folder = JObject.Parse(body);
				// only Id and Name are available because we specifically selected only those two Properties
				Debug.WriteLine(folder["Id"] + " " + folder["Name"]);
				JArray children = (JArray)folder["Children"];
				if (children != null)
				{
					var updateFile = children.FirstOrDefault(f => f["Name"].ToString().ToLower() == fileName.ToLower());
					if (updateFile == null)
					{
						throw new Exception("File missing or permissions for this account may not be sufficent to download this file.");
					}
					Debug.WriteLine(updateFile["Id"] + " " + updateFile["CreationDate"] + " " + updateFile["Name"]);
					returnFileId = updateFile["Id"].ToString();
				}
			}

			return returnFileId;
		}

		/// <summary>
		/// Create a new folder in the given parent folder.
		/// </summary>
		/// <param name="token">the OAuth2Token returned from Authenticate</param>
		/// <param name="parentId">the parent folder in which to create the new folder</param>
		/// <param name="name">the folder name</param>
		/// <param name="description">the folder description</param>
		public static void CreateFolder(OAuth2Token token, string parentId, string name, string description)
		{
			String uri = string.Format("https://{0}/sf/v3/Items({1})/Folder", ShareFileV3Sample.GetHostname(token), parentId);
			Debug.WriteLine(uri);

			HttpWebRequest request = WebRequest.CreateHttp(uri);
			ShareFileV3Sample.addAuthorizationHeader(request, token);

			Dictionary<string, object> folder = new Dictionary<string, object>();
			folder.Add("Name", name);
			folder.Add("Description", description);
			string json = JsonConvert.SerializeObject(folder);

			Debug.WriteLine(json);

			request.Method = "POST";
			request.ContentType = "application/json";
			using (var writer = new StreamWriter(request.GetRequestStream()))
			{
				writer.Write(json);
			}

			HttpWebResponse response = (HttpWebResponse)request.GetResponse();
			Debug.WriteLine(response.StatusCode);
			using (var reader = new StreamReader(response.GetResponseStream()))
			{
				String body = reader.ReadToEnd();
				JObject newFolder = JObject.Parse(body);
				Debug.WriteLine("Created Folder: " + newFolder["Id"]);
			}
		}

		/// <summary>
		/// Update the name and description of an Item.
		/// </summary>
		/// <param name="token">the OAuth2Token returned from Authenticate</param>
		/// <param name="itemId">the id of the item to update</param>
		/// <param name="name">the item name</param>
		/// <param name="description">the item description</param>
		public static void UpdateItem(OAuth2Token token, string itemId, string name, string description)
		{
			String uri = string.Format("https://{0}/sf/v3/Items({1})", ShareFileV3Sample.GetHostname(token), itemId);
			Debug.WriteLine(uri);

			HttpWebRequest request = WebRequest.CreateHttp(uri);
			ShareFileV3Sample.addAuthorizationHeader(request, token);

			Dictionary<string, object> folder = new Dictionary<string, object>();
			folder.Add("Name", name);
			folder.Add("Description", description);
			string json = JsonConvert.SerializeObject(folder);

			Debug.WriteLine(json);

			request.Method = "PATCH";
			request.ContentType = "application/json";
			using (var writer = new StreamWriter(request.GetRequestStream()))
			{
				writer.Write(json);
			}

			HttpWebResponse response = (HttpWebResponse)request.GetResponse();
			Debug.WriteLine(response.StatusCode);
			using (var reader = new StreamReader(response.GetResponseStream()))
			{
				String body = reader.ReadToEnd();
				JObject newFolder = JObject.Parse(body);
				Debug.WriteLine("Updated Folder: " + newFolder["Id"]);
			}
		}

		/// <summary>
		/// Delete an Item by Id.
		/// </summary>
		/// <param name="token">the OAuth2Token returned from Authenticate</param>
		/// <param name="itemId">the id of the item to delete</param>
		public static void DeleteItem(OAuth2Token token, string itemId)
		{
			String uri = string.Format("https://{0}/sf/v3/Items({1})", ShareFileV3Sample.GetHostname(token), itemId);
			Debug.WriteLine(uri);

			HttpWebRequest request = WebRequest.CreateHttp(uri);
			ShareFileV3Sample.addAuthorizationHeader(request, token);

			request.Method = "DELETE";

			HttpWebResponse response = (HttpWebResponse)request.GetResponse();
			Debug.WriteLine(response.StatusCode);
		}

		/// <summary>
		/// Downloads a single Item.
		/// </summary>
		/// <param name="token">the OAuth2Token returned from Authenticate</param>
		/// <param name="itemId">the id of the item to download</param>
		/// <param name="localPath">where to download the item to, like "c:\\path\\to\\the.file". If downloading a folder the localPath name should end in .zip.</param>
		public static void DownloadItem(OAuth2Token token, string itemId, string localPath)
		{
			String uri = string.Format("https://{0}/sf/v3/Items({1})/Download", ShareFileV3Sample.GetHostname(token), itemId);
			Debug.WriteLine(uri);

			HttpWebRequest request = WebRequest.CreateHttp(uri);
			ShareFileV3Sample.addAuthorizationHeader(request, token);
			request.AllowAutoRedirect = true;

			HttpWebResponse response = (HttpWebResponse)request.GetResponse();
			using (var source = new BufferedStream(response.GetResponseStream()))
			{
				using (var target = new FileStream(localPath, FileMode.Create))
				{
					int chunkSize = 1024 * 8;
					byte[] chunk = new byte[chunkSize];
					int len = 0;
					while ((len = source.Read(chunk, 0, chunkSize)) > 0)
					{
						target.Write(chunk, 0, len);
					}
					Debug.WriteLine("Download complete");
				}
			}
			Debug.WriteLine(response.StatusCode);
		}

		public static byte[] DownloadItem(OAuth2Token token, string itemId)
		{
			byte[] ret = null;
			MemoryStream ms = null;
			String uri = string.Format("https://{0}/sf/v3/Items({1})/Download", ShareFileV3Sample.GetHostname(token), itemId);
			Debug.WriteLine(uri);

			HttpWebRequest request = WebRequest.CreateHttp(uri);
			ShareFileV3Sample.addAuthorizationHeader(request, token);
			request.AllowAutoRedirect = true;

			HttpWebResponse response = (HttpWebResponse)request.GetResponse();
			using (var source = new BufferedStream(response.GetResponseStream()))
			{
				using (ms = new MemoryStream())
				{
					int chunkSize = 1024 * 8;
					byte[] chunk = new byte[chunkSize];
					int len = 0;
					while ((len = source.Read(chunk, 0, chunkSize)) > 0)
					{
						ms.Write(chunk, 0, len);
					}

					ms.Position = 0;
					ret = ms.GetBuffer();
				}
				Debug.WriteLine("Download complete");
			}
			Debug.WriteLine(response.StatusCode);
			return ret;
		}

		/// <summary>
		/// Uploads a File using the Standard upload method with a multipart/form mime encoded POST.
		/// </summary>
		/// <param name="token">the OAuth2Token returned from Authenticate</param>
		/// <param name="parentId">where to upload the file</param>
		/// <param name="localPath">the full path of the file to upload, like "c:\\path\\to\\file.name"</param>
		public static void UploadFile(OAuth2Token token, string parentId, string localPath)
		{
			String uri = string.Format("https://{0}/sf/v3/Items({1})/Upload", ShareFileV3Sample.GetHostname(token), parentId);
			Debug.WriteLine(uri);

			HttpWebRequest request = WebRequest.CreateHttp(uri);
			ShareFileV3Sample.addAuthorizationHeader(request, token);

			HttpWebResponse response = (HttpWebResponse)request.GetResponse();
			using (var reader = new StreamReader(response.GetResponseStream()))
			{
				String body = reader.ReadToEnd();

				JObject uploadConfig = JObject.Parse(body);
				string chunkUri = (string)uploadConfig["ChunkUri"];
				if (chunkUri != null)
				{
					Debug.WriteLine("Starting Upload");
					UploadMultiPartFile("File1", new FileInfo(localPath), chunkUri);
				}
			}
		}

		public static void UploadFile(OAuth2Token token, string parentId, Stream stream)
		{
			String uri = string.Format("https://{0}/sf/v3/Items({1})/Upload", ShareFileV3Sample.GetHostname(token), parentId);
			Debug.WriteLine(uri);

			HttpWebRequest request = WebRequest.CreateHttp(uri);
			ShareFileV3Sample.addAuthorizationHeader(request, token);

			HttpWebResponse response = (HttpWebResponse)request.GetResponse();
			using (var reader = new StreamReader(response.GetResponseStream()))
			{
				String body = reader.ReadToEnd();

				JObject uploadConfig = JObject.Parse(body);
				string chunkUri = (string)uploadConfig["ChunkUri"];
				if (chunkUri != null)
				{
					Debug.WriteLine("Starting Upload");
					UploadMultiPartFile("File1", "Update.log", stream, chunkUri);
				}
			}
		}

		public static void UploadMultiPartFile(string parameterName, string fileName, Stream file, string uploadUrl)
		{
			string boundaryGuid = "upload-" + Guid.NewGuid().ToString("n");
			string contentType = "multipart/form-data; boundary=" + boundaryGuid;

			MemoryStream ms = new MemoryStream();
			byte[] boundaryBytes = System.Text.Encoding.UTF8.GetBytes("\r\n--" + boundaryGuid + "\r\n");

			// Write MIME header
			ms.Write(boundaryBytes, 2, boundaryBytes.Length - 2);
			string header = String.Format(@"Content-Disposition: form-data; name=""{0}""; filename=""{1}""" +
				"\r\nContent-Type: application/octet-stream\r\n\r\n", parameterName, fileName);
			byte[] headerBytes = System.Text.Encoding.UTF8.GetBytes(header);
			ms.Write(headerBytes, 0, headerBytes.Length);

			// Load the file into the byte array

			byte[] buffer = new byte[1024 * 1024];
			int bytesRead;

			while ((bytesRead = file.Read(buffer, 0, buffer.Length)) > 0)
			{
				ms.Write(buffer, 0, bytesRead);
			}

			// Write MIME footer
			boundaryBytes = System.Text.Encoding.UTF8.GetBytes("\r\n--" + boundaryGuid + "--\r\n");
			ms.Write(boundaryBytes, 0, boundaryBytes.Length);

			byte[] postBytes = ms.ToArray();
			ms.Close();

			HttpWebRequest request = WebRequest.CreateHttp(uploadUrl);
			request.Timeout = 1000 * 60; // 60 seconds
			request.Method = "POST";
			request.ContentType = contentType;
			request.ContentLength = postBytes.Length;
			request.Credentials = CredentialCache.DefaultCredentials;

			using (Stream postStream = request.GetRequestStream())
			{
				int chunkSize = 48 * 1024;
				int remaining = postBytes.Length;
				int offset = 0;

				do
				{
					if (chunkSize > remaining) { chunkSize = remaining; }
					postStream.Write(postBytes, offset, chunkSize);

					remaining -= chunkSize;
					offset += chunkSize;

				} while (remaining > 0);

				postStream.Close();
			}

			HttpWebResponse response = (HttpWebResponse)request.GetResponse();
			Debug.WriteLine("Upload Status: " + response.StatusCode);
			response.Close();
		}

		/// <summary>
		/// Does a multipart form post upload of a file to a url.
		/// </summary>
		/// <param name="parameterName">multipart parameter name. File1 for a standard upload.</param>
		/// <param name="file">the FileInfo to upload</param>
		/// <param name="uploadUrl">the url of the server to upload to</param>
		public static void UploadMultiPartFile(string parameterName, FileInfo file, string uploadUrl)
		{
			string boundaryGuid = "upload-" + Guid.NewGuid().ToString("n");
			string contentType = "multipart/form-data; boundary=" + boundaryGuid;

			MemoryStream ms = new MemoryStream();
			byte[] boundaryBytes = System.Text.Encoding.UTF8.GetBytes("\r\n--" + boundaryGuid + "\r\n");

			// Write MIME header
			ms.Write(boundaryBytes, 2, boundaryBytes.Length - 2);
			string header = String.Format(@"Content-Disposition: form-data; name=""{0}""; filename=""{1}""" +
				"\r\nContent-Type: application/octet-stream\r\n\r\n", parameterName, file.Name);
			byte[] headerBytes = System.Text.Encoding.UTF8.GetBytes(header);
			ms.Write(headerBytes, 0, headerBytes.Length);

			// Load the file into the byte array
			using (FileStream source = file.OpenRead())
			{
				byte[] buffer = new byte[1024 * 1024];
				int bytesRead;

				while ((bytesRead = source.Read(buffer, 0, buffer.Length)) > 0)
				{
					ms.Write(buffer, 0, bytesRead);
				}
			}

			// Write MIME footer
			boundaryBytes = System.Text.Encoding.UTF8.GetBytes("\r\n--" + boundaryGuid + "--\r\n");
			ms.Write(boundaryBytes, 0, boundaryBytes.Length);

			byte[] postBytes = ms.ToArray();
			ms.Close();

			HttpWebRequest request = WebRequest.CreateHttp(uploadUrl);
			request.Timeout = 1000 * 60; // 60 seconds
			request.Method = "POST";
			request.ContentType = contentType;
			request.ContentLength = postBytes.Length;
			request.Credentials = CredentialCache.DefaultCredentials;

			using (Stream postStream = request.GetRequestStream())
			{
				int chunkSize = 48 * 1024;
				int remaining = postBytes.Length;
				int offset = 0;

				do
				{
					if (chunkSize > remaining) { chunkSize = remaining; }
					postStream.Write(postBytes, offset, chunkSize);

					remaining -= chunkSize;
					offset += chunkSize;

				} while (remaining > 0);

				postStream.Close();
			}

			HttpWebResponse response = (HttpWebResponse)request.GetResponse();
			Debug.WriteLine("Upload Status: " + response.StatusCode);
			response.Close();
		}
	}
}