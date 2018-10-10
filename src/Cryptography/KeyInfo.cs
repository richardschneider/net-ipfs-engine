using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ipfs.Engine.Cryptography
{
    class KeyInfo : IKey
    {
        public string Name { get; set; }
        public MultiHash Id { get; set; }
    }
}
