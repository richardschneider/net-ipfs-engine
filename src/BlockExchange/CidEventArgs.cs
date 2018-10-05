using Ipfs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ipfs.Engine.BlockExchange
{
    /// <summary>
    ///   The content addressable ID related to an event. 
    /// </summary>
    /// <see cref="Cid"/>
    /// <see cref="Bitswap.BlockNeeded"/>
    public class CidEventArgs : EventArgs
    {
        /// <summary>
        ///   The content addressable ID. 
        /// </summary>
        /// <value>
        ///   The unique ID of the block.
        /// </value>
        public Cid Id { get; set; }
    }
}
