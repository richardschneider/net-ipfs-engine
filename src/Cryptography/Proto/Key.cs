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

    [ProtoContract]
    class PrivateKey
    {
        [ProtoMember(1, IsRequired = true)]
        public KeyType Type;
        [ProtoMember(2, IsRequired = true)]
        public byte[] Data;
    }
}
