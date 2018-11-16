using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/* From https://github.com/libp2p/js-libp2p-secio/blob/master/src/handshake/secio.proto.js
 
module.exports = `message Propose
{
    optional bytes rand = 1;
    optional bytes pubkey = 2;
    optional string exchanges = 3;
    optional string ciphers = 4;
    optional string hashes = 5;
}
message Exchange
{
    optional bytes epubkey = 1;
    optional bytes signature = 2;
}
*/

namespace PeerTalk.SecureCommunication
{
    [ProtoContract]
    class Secio1Propose
    {
        [ProtoMember(1)]
        public byte[] Nonce;

        [ProtoMember(2)]
        public byte[] PublicKey;

        [ProtoMember(3)]
        public string Exchanges;

        [ProtoMember(4)]
        public string Ciphers;

        [ProtoMember(5)]
        public string Hashes;
    }

    [ProtoContract]
    class Secio1Exchange
    {
        [ProtoMember(1)]
        public byte[] EPublicKey;

        [ProtoMember(2)]
        public byte[] Signature;

    }
}
