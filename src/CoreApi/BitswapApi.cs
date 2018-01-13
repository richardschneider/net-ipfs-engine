using System;
using System.Collections.Generic;
using System.Text;
using Ipfs.CoreApi;

namespace Ipfs.Engine.CoreApi
{
    class BitswapApi : IBitswapApi
    {
        IpfsEngine ipfs;

        public BitswapApi(IpfsEngine ipfs)
        {
            this.ipfs = ipfs;
        }
    }
}
