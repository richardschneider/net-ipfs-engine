using Ipfs;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeerTalk.Routing
{
    // From https://github.com/libp2p/js-libp2p-kad-dht/blob/master/src/message/dht.proto.js
    [ProtoContract]
    class DhtRecordMessage
    {
        [ProtoMember(1)]
        public byte[] Key { get; set; }

        [ProtoMember(2)]
        public byte[] Value { get; set; }

        [ProtoMember(3)]
        public byte[] Author { get; set; }

        [ProtoMember(4)]
        public byte[] Signature { get; set; }

        [ProtoMember(5)]
        public string TimeReceived { get; set; }
    }

    enum MessageType
    {
        PutValue = 0,
        GetValue = 1,
        AddProvider = 2,
        GetProviders = 3,
        FindNode = 4,
        Ping = 5
    }

    enum ConnectionType
    {
        // sender does not have a connection to peer, and no extra information (default)
        NotConnected = 0,

        // sender has a live connection to peer
        Connected = 1,

        // sender recently connected to peer
        CanConnect = 2,

        // sender recently tried to connect to peer repeatedly but failed to connect
        // ("try" here is loose, but this should signal "made strong effort, failed")
        CannotConnect = 3
    }

    [ProtoContract]
    class DhtPeerMessage
    {
        // ID of a given peer.
        [ProtoMember(1)]
        public byte[] Id { get; set; }

        // multiaddrs for a given peer
        [ProtoMember(2)]
        public byte[][] Addresses { get; set; }

        // used to signal the sender's connection capabilities to the peer
        [ProtoMember(3)]
        ConnectionType Connection { get; set; }

        public Peer ToPeer()
        {
            var id = new MultiHash(Id);
            var x = new MultiAddress($"/ipfs/{id}");
            return new Peer
            {
                Id = id,
                Addresses = Addresses
                    .Select(bytes =>
                    {
                        try
                        {
                            var ma = new MultiAddress(bytes);
                            ma.Protocols.AddRange(x.Protocols);
                            return ma;
                        }
                        catch
                        {
                            return null;
                        }
                    })
                    .Where(a => a != null)
                    .ToArray()
            };
        }
    }

    [ProtoContract]
    class DhtMessage
    {
        // defines what type of message it is.
        [ProtoMember(1)]
        public MessageType Type { get; set; }

        // defines what coral cluster level this query/response belongs to.
        // in case we want to implement coral's cluster rings in the future.
        [ProtoMember(10)]
        public int ClusterLevelRaw { get; set; }

        // Used to specify the key associated with this message.
        // PUT_VALUE, GET_VALUE, ADD_PROVIDER, GET_PROVIDERS
        // adjusted for javascript
        [ProtoMember(2)]
        public byte[] Key { get; set; }

        // Used to return a value
        // PUT_VALUE, GET_VALUE
        // adjusted Record to bytes for js
        [ProtoMember(3)]
        public DhtRecordMessage Record { get; set; }

        // Used to return peers closer to a key in a query
        // GET_VALUE, GET_PROVIDERS, FIND_NODE
        [ProtoMember(8)]
        public DhtPeerMessage[] CloserPeers { get; set; }

        // Used to return Providers
        // GET_VALUE, ADD_PROVIDER, GET_PROVIDERS
        [ProtoMember(9)]
        public DhtPeerMessage[] ProviderPeers { get; set; }
    }
}
