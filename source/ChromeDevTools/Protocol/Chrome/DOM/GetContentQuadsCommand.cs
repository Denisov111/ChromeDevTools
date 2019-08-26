using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MasterDevs.ChromeDevTools.Protocol.Chrome.DOM
{
    /// <summary>
	/// Returns boxes for the currently selected nodes.
	/// </summary>
	[Command(ProtocolName.DOM.GetContentQuads)]
    [SupportedBy("Chrome")]
    public class GetContentQuadsCommand : ICommand<GetContentQuadsCommandResponse>
    {
        /// <summary>
		/// Gets or sets Identifier of the node.
		/// </summary>
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public long? NodeId { get; set; }
        /// <summary>
        /// Gets or sets Identifier of the backend node.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public long? BackendNodeId { get; set; }
        /// <summary>
        /// Gets or sets JavaScript object id of the node wrapper.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string ObjectId { get; set; }
    }
}
