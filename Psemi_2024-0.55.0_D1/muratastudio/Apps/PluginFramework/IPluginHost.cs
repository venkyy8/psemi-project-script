using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginFramework
{
	public interface IPluginHost
	{
		void Clear();

		void PlacePlugin(IPlugin plugin, object dataContext, bool isAllRegRead = false);

		//bool ContainsChild(IPlugin plugin);

	}
}
