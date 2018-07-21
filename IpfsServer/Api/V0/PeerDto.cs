using Ipfs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ipfs.Server.Api.V0
{
    public class PeerDto
    {
        public string ID;
        public string PublicKey;
        public IEnumerable<string> Addresses;
        public string AgentVersion;
        public string ProtocolVersion;

        public PeerDto(Peer peer)
        {
            ID = peer.Id.ToBase58();
            PublicKey = peer.PublicKey;
            Addresses = peer.Addresses.Select(a => a.ToString());
            AgentVersion = peer.AgentVersion;
            ProtocolVersion = peer.ProtocolVersion;
        }
    }
}
