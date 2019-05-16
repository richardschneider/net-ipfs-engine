# Private Network

A private network is a group of [peers](peer.md) that share the same 256-bit 
[secret key](xref:PeerTalk.Cryptography.PreSharedKey).  All communication between the
peers is encryped with the [XSalsa20 cipher](https://en.wikipedia.org/wiki/Salsa20).  The
specification is at [PSK v1](https://github.com/libp2p/specs/blob/master/pnet/Private-Networks-PSK-V1.md)
and is implemented by the [Psk1Protector](xref:PeerTalk.SecureCommunication.Psk1Protector) class.

The private network is defined by the symmetric secret key, which is known by all members. 
All traffic leaving the peer is encrypted and there is no characteristic 
handshake. The secret key is just a random number; public/private 
keys and certificates are not needed.

## Joining

The [local peer](local-peer.md) becomes a member of the private network by setting the
secret key.  This can be done by

- setting [SwarmOptions.PrivateNetworkKey](xref:Ipfs.Engine.SwarmOptions.PrivateNetworkKey)
- creating the `swarm.key` file in the [repository](repository.md)

## Example

Two ways of specifying the secret key for a private network.

#### A `swarm.key` file

Copy the following file to the repository

```
/key/swarm/psk/1.0.0/
/base16/
e8d6d31e8e02000010d6d31e8e020000f0d1fc609400000078f0d31e8e020000
```
#### Setting the key

```csharp
ipfs.Options.Swarm.PrivateNetworkKey = new PreSharedKey
{
	Value = "e8d6 ... 20000".ToHexBuffer();
};
```
