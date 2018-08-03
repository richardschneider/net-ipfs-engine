using Ipfs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ipfs.Server.HttpApi.V0
{
    /// <summary>
    ///   Statistics on a block.
    /// </summary>
    public class BlockStatsDto
    {
        /// <summary>
        ///   Something like "QmYNQJoKGNHTpPxCBPh9KkDpaExgd2duMa3aF6ytMpHdao".
        /// </summary>
        public string Key;

        /// <summary>
        ///   The size, in bytes, of the block.
        /// </summary>
        public long Size;
    }
}
