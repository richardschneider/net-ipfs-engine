using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ipfs.Engine
{
    /// <summary>
    ///   Configuration options for a <see cref="Ipfs.CoreApi.IBlockApi">block service</see>.
    /// </summary>
    /// <seealso cref="IpfsEngineOptions"/>
    public class BlockOptions
    {
        /// <summary>
        ///   Determines if an inline CID can be created.
        /// </summary>
        /// <value>
        ///   Defaults to <b>false</b>.
        /// </value>
        /// <remarks>
        ///   An "inline CID" places the content in the CID not in a seperate block.
        ///   It is used to speed up access to content that is small.
        /// </remarks>
        public bool AllowInlineCid { get; set; } = false;

        /// <summary>
        ///   Used to determine if the content is small enough to be inlined.
        /// </summary>
        /// <value>
        ///   The maximum number of bytes for content that will be inlined.
        ///   Defaults to 64.
        /// </value>
        public int InlineCidLimit { get; set; } = 64;

        /// <summary>
        ///   The maximun length of data block.
        /// </summary>
        /// <value>
        /// </value>
        ///   1MB (1024 * 1024)
        public int MaxBlockSize { get; } = 1024 * 1024;
    }
}
