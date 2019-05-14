# Core API

The [Core API](xref:Ipfs.CoreApi.ICoreApi) is a set of interfaces to the IPFS features and is implemented by the 
[engine](xref:Ipfs.Engine.IpfsEngine).  The 
[FileSystem](filesystem.md) and [PubSub](pubsub.md) features are most often used.

```csharp
const string filename = "QmS4ustL54uo8FzR9455qaxZwuMiUhyvMcX9Ba8nUH4uVv/about";
string text = await ipfs.FileSystem.ReadAllTextAsync(filename);
```

## Features

Each IPFS feature has it's own interface.

| Feature | Purpose |
| ------- | ------- |
| [Bitswap](xref:Ipfs.CoreApi.IBitswapApi) | Block trading between peers |
| [Block](xref:Ipfs.CoreApi.IBlockApi) | Manages the blocks |
| [Bootstrap](xref:Ipfs.CoreApi.IBootstrapApi) | Trusted peers |
| [Config](xref:Ipfs.CoreApi.IConfigApi) | Manages the configuration of the local peer |
| [Dag](xref:Ipfs.CoreApi.IDagApi) | Manages the IPLD (linked data) Directed Acrylic Graph |
| [Dht](xref:Ipfs.CoreApi.IDhtApi) | Manages the Distributed Hash Table |
| [Dns](xref:Ipfs.CoreApi.IDnsApi) | DNS mapping to IPFS |
| [Misc](xref:Ipfs.CoreApi.IGenericApi) | Some miscellaneous methods |
| [FileSystem](filesystem.md) | Manages the files/directories in IPFS |
| [Key](xref:Ipfs.CoreApi.IKeyApi) | Manages the cryptographic keys |
| [Name](xref:Ipfs.CoreApi.INameApi) | Manages the Interplanetary Name Space (IPNS) |
| [Object](xref:Ipfs.CoreApi.IObjectApi) | Manages the IPFS Directed Acrylic Graph |
| [Pin](xref:Ipfs.CoreApi.IPinApi) | Manage objects that are locally stored and permanent |
| [PubSub](pubsub.md) | Publish and subscribe topic messages |
| [Swarm](xref:Ipfs.CoreApi.ISwarmApi) | Manages the swarm of peers |
| [Stats](xref:Ipfs.CoreApi.IStatsApi) | Statistics on IPFS components |

