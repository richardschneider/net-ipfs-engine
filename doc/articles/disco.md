# Peer Discovery

Various schemes are used to find other peers in the network. 

To get the discovered peers use the [Swarm API](xref:Ipfs.CoreApi.ISwarmApi)

```csharp
foreach (var peer in await ipfs.Swarm.AddressesAsync())
{
   Console.WriteLine(peer.Id);
}
```

## Multicast DNS

[MdnsNext](xref:PeerTalk.Discovery.MdnsNext) uses [RFC 6762 - Multicast DNS](https://tools.ietf.org/html/rfc6762) 
to locate peers on the local area network with zero configuration. 
Local area network peers are very useful to peer-to-peer protocols, because of their low latency links.

The [mDNS-discovery](https://github.com/libp2p/specs/blob/master/discovery/mdns.md) specification 
describes how IPFS uses mDNS to discover other peers;
[private networks](pnet.md) are also supported.

## Bootstrap

[Bootstrap](xref:PeerTalk.Discovery.Bootstrap) uses a [pre-configured list](bootstrap.md)
of addresses of highly stable (and somewhat trusted) peers to
find the rest of the network.  These is sometimes referred to as `seed nodes`.

## Randon walk

It makes random DHT (Distributed Hash Table) [queries](xref:Ipfs.CoreApi.IPeerRouting.FindPeerAsync*)
in order to discover a large number of peers quickly.
This causes the DHT to converge much faster, at the expense of a small load 
at the very beginning.

