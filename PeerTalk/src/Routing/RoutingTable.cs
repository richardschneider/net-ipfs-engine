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

        public byte[] Id => RoutingTable.Key(Peer.Id);
    }

    /// <summary>
    ///   A wrapper around k-bucket, to provide easy store and retrival for peers.
    /// </summary>
    public class RoutingTable
    {
        KBucket<RoutingPeer> Peers = new KBucket<RoutingPeer>();

        /// <summary>
        ///   Creates a new instance of the <see cref="RoutingTable"/> for
        ///   the specified <see cref="Peer"/>.
        /// </summary>
        /// <param name="localPeer"></param>
        public RoutingTable(Peer localPeer)
        {
            Peers.LocalContactId = Key(localPeer.Id);
            Peers.ContactsToPing = 1;
            Peers.Ping += Peers_Ping;
        }

        /// <summary>
        ///   A k-bucket is full!
        /// </summary>
        /// <remarks>
        ///  Currently this just removes the oldest contact from the list, 
        ///  without acutally pinging the individual peers.
        /// 
        ///  This is the same as go does, but should probably
        ///  be upgraded to actually ping the individual peers.
        /// </remarks>
        void Peers_Ping(object sender, PingEventArgs<RoutingPeer> e)
        {
            if (Peers.Remove(e.Oldest.First()))
            {
                Peers.Add(e.Newest);
            }
        }

        /// <summary>
        ///   Add some information about the peer.
        /// </summary>
        public void Add(Peer peer)
        {
            Peers.Add(new RoutingPeer(peer));
        }

        /// <summary>
        ///   Remove the information about the peer.
        /// </summary>
        public void Remove(Peer peer)
        {
            Peers.Remove(new RoutingPeer(peer));
        }

        /// <summary>
        ///   Determines in the peer exists in the routing table.
        /// </summary>
        public bool Contains(Peer peer)
        {
            return Peers.Contains(new RoutingPeer(peer));
        }

        /// <summary>
        ///   Find the closest peers to the peer ID.
        /// </summary>
        public IEnumerable<Peer> NearestPeers(MultiHash id)
        {
            return Peers
                .Closest(Key(id))
                .Select(r => r.Peer);
        }

        /// <summary>
        ///   Converts the peer ID to a routing table key.
        /// </summary>
        /// <param name="id">A multihash</param>
        /// <returns>
        ///   The routing table key.
        /// </returns>
        /// <remarks>
        ///   The peer ID is actually a multihash, it always starts with the same characters 
        ///   (ie, Qm for rsa). This causes the distribution of hashes to be 
        ///   non-equally distributed across all possible hash buckets. So the re-hash 
        ///   into a non-multihash is to evenly distribute the potential keys and 
        ///   hash buckets.
        /// </remarks>
        /// <seealso href="https://github.com/libp2p/js-libp2p-kad-dht/issues/56#issuecomment-441378802"/>
        static public byte[] Key(MultiHash id)
        {
            return MultiHash.ComputeHash(id.ToArray(), "sha2-256").Digest;
        }
    }
}
