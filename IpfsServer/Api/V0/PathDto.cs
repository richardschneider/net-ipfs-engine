using Ipfs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ipfs.Server.Api.V0
{
    public class PathDto
    {
        public string Path;

        public PathDto(String path)
        {
            Path = path;
        }
    }
}
