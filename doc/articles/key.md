# Key

An asymmetric key is used to prove identity (sign) and protect data (encrypt/decrypt). Keys are managed 
by the [Key API](xref:Ipfs.CoreApi.IKeyApi). A key is never stored in plain text, see 
[private key storage](repo/key.md) for the details.

A key has a local name and a global ID.  The ID is the SHA-256 multihash 
of its public key. The public key is a protobuf encoding containing a type and 
the DER encoding of the PKCS SubjectPublicKeyInfo.

## Types

- `rsa`, Rivest–Shamir–Adleman [crypto system](https://en.wikipedia.org/wiki/RSA_(cryptosystem))
- `secp256k1`, Bitcoin's [elliptic cuve](https://en.bitcoin.it/wiki/Secp256k1)

## Self

The key named `self` is special in that it uniquely identifies the 
[local peer](xref:Ipfs.Engine.IpfsEngine.LocalPeer) to the IPFS network.  It is 
automatically created with the [repository](repository.md) and is controlled 
by the [keychain options](xref:Ipfs.Engine.IpfsEngine.Options).