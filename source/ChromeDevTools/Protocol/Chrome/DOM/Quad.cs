using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MasterDevs.ChromeDevTools.Protocol.Chrome.DOM
{
    /// <summary>
	/// Box model.
	/// </summary>
	[SupportedBy("Chrome")]
    public class Quad
    {
        /// <summary>
		/// Gets or sets Content box
		/// </summary>
		public double[] Content { get; set; }
    }
}
