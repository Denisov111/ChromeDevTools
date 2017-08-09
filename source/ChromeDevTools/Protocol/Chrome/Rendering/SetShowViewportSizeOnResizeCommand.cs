using MasterDevs.ChromeDevTools;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace MasterDevs.ChromeDevTools.Protocol.Chrome.Rendering
{
	/// <summary>
	/// Paints viewport size upon main frame resize.
	/// </summary>
	[Command(ProtocolName.Rendering.SetShowViewportSizeOnResize)]
	[SupportedBy("Chrome")]
	public class SetShowViewportSizeOnResizeCommand
	{
		/// <summary>
		/// Gets or sets Whether to paint size or not.
		/// </summary>
		public bool Show { get; set; }
	}
}
