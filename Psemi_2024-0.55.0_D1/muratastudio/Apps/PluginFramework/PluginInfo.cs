using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace PluginFramework
{
	/// <summary>
	/// A Class Containing Information About a Plugin Launch Button in the Ribbon
	/// </summary>
	public class PluginInfo
	{
		#region Public Properties

		/// <summary>
		/// A string Containing the DLL assembly name
		/// </summary>
		public string AssemblyName { get; set; }

		/// <summary>
		/// A string Containing the DLL assembly version
		/// </summary>
		public string AssemblyVersion { get; set; }

		/// <summary>
		/// A string Containing the Label of a Tab that Should Contain a Launch Button for the Plugin
		/// </summary>
		public string TabName { get; set; }

		/// <summary>
		/// A string Containing the Label of a Panel that Should Contain a Launch Button for the Plugin
		/// </summary>
		public string PanelName { get; set; }

		/// <summary>
		/// A string Containing the Label of a Button that Should Launch the Plugin
		/// </summary>
		public string ButtonName { get; set; }

		/// <summary>
		/// An Image that Should be Displayed in the Button that Should Launch the Plugin
		/// </summary>
		public ImageSource ButtonImage { get; set; }

		/// <summary>
		/// A Small Image that Should be Displayed in the Button that Should Launch the Plugin
		/// </summary>
		public ImageSource ButtonSmallImage { get; set; }

		/// <summary>
		/// A Small Image that Should be Displayed in the Button that Should Launch the Plugin
		/// </summary>
		public ImageSource DockTabImage { get; set; }

		/// <summary>
		/// ToolTip Text that Should be Displayed When the Mouse Hovers Over the Button that Should Launch the Plugin
		/// </summary>
		public string ButtonToolTipText { get; set; } 

		#endregion

		#region Constructor

		public PluginInfo()
		{
			AssemblyName = string.Empty;
			AssemblyVersion = string.Empty;
			TabName = string.Empty;
			PanelName = string.Empty;
			ButtonName = string.Empty;
			ButtonImage = null;
			ButtonSmallImage = null;
			DockTabImage = null;
			ButtonToolTipText = string.Empty;
		} 

		#endregion
	}
}
