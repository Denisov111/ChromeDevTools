using MasterDevs.ChromeDevTools;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace MasterDevs.ChromeDevTools.Protocol.Chrome.Fetch
{
    /// <summary>
    /// Provides response to the request.
    /// </summary>
    [Command(ProtocolName.Fetch.FulfillRequest)]
    [SupportedBy("Chrome")]
    public class FulfillRequestCommand : ICommand<FulfillRequestCommandResponse>
    {
        /// <summary>
        /// Gets or sets An id the client received in requestPaused event.
        /// </summary>
        public string RequestId { get; set; }
        /// <summary>
        /// Gets or sets An HTTP response code.
        /// </summary>
        public long ResponseCode { get; set; }
        /// <summary>
        /// Gets or sets Response headers.
        /// </summary>
        public HeaderEntry[] ResponseHeaders { get; set; }
        /// <summary>
        /// Gets or sets A response body.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Body { get; set; }
        /// <summary>
        /// Gets or sets A textual representation of responseCode.
        ///If absent, a standard phrase mathcing responseCode is used.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string ResponsePhrase { get; set; }
    }
}
