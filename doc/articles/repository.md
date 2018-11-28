# Repository

The repository is a local persistent store for IPFS data. By default, it is located 
at `$HOME/.csipfs`.  If not present, the [IPFS Engine](xref:Ipfs.Engine.IpfsEngine) 
will create it with the factory defaults.

To change its name and/or location use the [environment variables](envvars.md) 
or the [Repository Options](xref:Ipfs.Engine.RepositoryOptions).

# Contents

| File/Folder | Usage | Description |
| ----------- | ----- | ----------- |
| config      | [Config API](xref:Ipfs.Engine.IpfsEngine.Config)| The configuration information |
| blocks      | [Block API](xref:Ipfs.Engine.IpfsEngine.Block) | A [Block](repo/block.md) of data |
| keys        | [Key API](xref:Ipfs.Engine.IpfsEngine.Key) | Cryptographic [keys](repo/key.md) |
| pins        | [Pin API](xref:Ipfs.Engine.IpfsEngine.Pin) | A [Block](repo/block.md) that is pinned to the local repository |
