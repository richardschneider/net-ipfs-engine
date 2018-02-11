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
    public class Multistream1 : IPeerProtocol
    {
        /// <inheritdoc />
        public string Name { get; } = "multistream";

        /// <inheritdoc />
        public SemVersion Version { get; } = new SemVersion(1, 0);

        /// <inheritdoc />
        public override string ToString()
        {
            return $"/{Name}/{Version}";
        }

    }
}
