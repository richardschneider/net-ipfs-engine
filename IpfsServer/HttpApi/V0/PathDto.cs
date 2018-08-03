using Ipfs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ipfs.Server.HttpApi.V0
{
    /// <summary>
    ///  A path to some data.
    /// </summary>
    public class PathDto
    {
        /// <summary>
        ///   Something like "/ipfs/QmYNQJoKGNHTpPxCBPh9KkDpaExgd2duMa3aF6ytMpHdao".
        /// </summary>
        public string Path;

        /// <summary>
        ///   Create a new path.
        /// </summary>
        /// <param name="path"></param>
        public PathDto(String path)
        {
            Path = path;
        }
    }
}
