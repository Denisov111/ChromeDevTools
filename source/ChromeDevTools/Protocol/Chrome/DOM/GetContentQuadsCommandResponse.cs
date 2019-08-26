using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MasterDevs.ChromeDevTools.Protocol.Chrome.DOM
{
    /// <summary>
	/// Returns boxes for the currently selected nodes.
	/// </summary>
	[CommandResponse(ProtocolName.DOM.GetContentQuads)]
    [SupportedBy("Chrome")]
    public class GetContentQuadsCommandResponse
    {
        /// <summary>
        /// Gets or sets Box model for the node.
        /// </summary>
        public Quad Quad { get; set; }
    }
}
