using Ipfs;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeerTalk.Routing
{
    public class RoutingTable
    {
        public ConcurrentBag<Peer> Peers { get; set; } = new ConcurrentBag<Peer>();
        
        public IEnumerable<Peer> NearestPeers(MultiHash id)
        {
            return Peers;
        }
    }
}
