using Ipfs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ipfs.Server.Api.V0
{
    /// <summary>
    ///  A key to some data.
    /// </summary>
    public class KeyDto
    {
        /// <summary>
        ///   Typically a CID.
        /// </summary>
        public string Key;
    }
}
