# Repository

The repository is a local persistent store for IPFS data. By default, it is located 
at `$HOME/.csipfs`.  If not present, the [IPFS Engine](xref:Ipfs.Engine.IpfsEngine) 
will create it with the factory defaults.

To change its name and/or location use the [environment variables](envvars.md) 
or the [Repository Options](xref:Ipfs.Engine.RepositoryOptions).

## Creating

The repository will be automatically created if it does not already exist.  

At creation time 
a [cryptograhic key](key.md) named `self` is created and is used to uniquely identify this node 
in the IPFS network.  The [keychain options](xref:Ipfs.Engine.Cryptography.KeyChainOptions) are used to control the type of key that 
is generated; by default it is RSA with 2048 bits.

## Contents

| File/Folder | Usage | Description |
| ----------- | ----- | ----------- |
| config      | [Config API](xref:Ipfs.Engine.IpfsEngine.Config)| The [configuration](repo/config.md) information |
| blocks      | [Block API](xref:Ipfs.Engine.IpfsEngine.Block) | A [Block](repo/block.md) of data |
| keys        | [Key API](xref:Ipfs.Engine.IpfsEngine.Key) | Cryptographic [keys](repo/key.md) |
| pins        | [Pin API](xref:Ipfs.Engine.IpfsEngine.Pin) | A [Block](repo/block.md) that is pinned to the local repository |


