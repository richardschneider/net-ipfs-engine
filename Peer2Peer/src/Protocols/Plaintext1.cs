using Semver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Peer2Peer.Protocols
{
    /// <summary>
    ///   TODO
    /// </summary>
    public class Plaintext1 : IPeerProtocol
    {
        /// <inheritdoc />
        public string Name { get; } = "plaintext";

        /// <inheritdoc />
        public SemVersion Version { get; } = new SemVersion(1, 0);

        /// <inheritdoc />
        public override string ToString()
        {
            return $"/{Name}/{Version}";
        }
    }
}
