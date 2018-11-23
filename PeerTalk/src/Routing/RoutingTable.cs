using Ipfs;
using Makaretu.Collections;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeerTalk.Routing
{
    class RoutingPeer : IContact
    {
        public Peer Peer;

        public RoutingPeer(Peer peer)
        {
            Peer = peer;
        }

        public byte[] Id => Peer.Id.Digest;
    }

    public class RoutingTable
    {

        KBucket<RoutingPeer> Peers = new KBucket<RoutingPeer>();
        
        public RoutingTable(Peer localPeer)
        {
            Peers.LocalContactId = localPeer.Id.Digest;
        }

        public void Add(Peer peer)
        {
            Peers.Add(new RoutingPeer(peer));
        }

        public void Remove(Peer peer)
        {
            Peers.Remove(new RoutingPeer(peer));
        }

        public bool Contains(Peer peer)
        {
            return Peers.Contains(new RoutingPeer(peer));
        }

        public IEnumerable<Peer> NearestPeers(MultiHash id)
        {
            return Peers
                .Closest(id.Digest)
                .Select(r => r.Peer);
        }

    }
}
