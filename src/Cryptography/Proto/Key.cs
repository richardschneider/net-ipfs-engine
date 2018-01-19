using ProtoBuf;

namespace Ipfs.Engine.Cryptography.Proto
{
    enum KeyType
    {
        RSA = 0,
        Ed25519 = 1,
        Secp256k1 = 2
    }

    [ProtoContract]
    class PublicKey
    {
        [ProtoMember(1, IsRequired = true)]
        public KeyType Type;
        [ProtoMember(2, IsRequired = true)]
        public byte[] Data;
    }

    // PrivateKey message is not currently used.  Hopefully it never will be
    // because it could introduce a huge security hole.
#if false
    [ProtoContract]
    class PrivateKey
    {
        [ProtoMember(1, IsRequired = true)]
        public KeyType Type;
        [ProtoMember(2, IsRequired = true)]
        public byte[] Data;
    }
#endif
}
