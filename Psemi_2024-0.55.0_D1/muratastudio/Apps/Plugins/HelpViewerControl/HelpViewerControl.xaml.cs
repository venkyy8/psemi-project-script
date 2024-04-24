using HelpViewerControl.ViewModel;
using HardwareInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;

namespace HelpViewerControl
{
	/// <summary>
	/// Interaction logic for HelpViewerControl.xaml
	/// </summary>
	public partial class HelpViewerControl : UserControl
	{
		#region Private Members

		private PluginViewModel pvm;
		private const string hostUri = "http://localhost:8088/PsuedoWebHost/";
		private HttpListener _httpListener;

		#endregion

		#region Constructors

		public HelpViewerControl()
		{
			InitializeComponent();
		}

		public HelpViewerControl(object device, bool isInternalMode)
		{
			InitializeComponent();
			pvm = new PluginViewModel(device, isInternalMode);

			IDevice iDevice = device as IDevice;

			if (iDevice.HelpSheet != null)
			{
				CreatePdfServer(iDevice.HelpSheet);

				// Cleanup after the browser finishes navigating
				DocumentView.Navigated += BrowserOnNavigated;
				DocumentView.Navigate(hostUri);
			}
			else
			{
				// TODO create a no datasheet available html resource.
			}

			DataContext = pvm;
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Creates an HTTP server that will return the PDF.
		/// </summary>
		/// <param name="pdfBytes"></param>
		private void CreatePdfServer(byte[] pdfBytes)
		{
			_httpListener = new HttpListener();
			_httpListener.Prefixes.Add(hostUri);
			_httpListener.Start();
			_httpListener.BeginGetContext((ar) =>
			{
				HttpListenerContext context = _httpListener.EndGetContext(ar);

				// Obtain a response object.
				HttpListenerResponse response = context.Response;
				response.StatusCode = (int)HttpStatusCode.OK;
				response.ContentType = "application/pdf";

				// Construct a response.
				if (pdfBytes != null)
				{
					response.ContentLength64 = pdfBytes.Length;

					// Get a response stream and write the PDF to it.
					Stream oStream = response.OutputStream;
					oStream.Write(pdfBytes, 0, pdfBytes.Length);
					oStream.Flush();
				}

				response.Close();
			}, null);

		}

		/// <summary>
		/// Stops the http listener after the browser has finished loading the document.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="navigationEventArgs"></param>
		private void BrowserOnNavigated(object sender, NavigationEventArgs navigationEventArgs)
		{
			_httpListener.Stop();
			DocumentView.Navigated -= BrowserOnNavigated;
		} 

		#endregion
	}
}
