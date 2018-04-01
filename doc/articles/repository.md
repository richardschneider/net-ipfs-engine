# Repository

The repository is a local persistent store for IPFS data. By default, it is located at `$HOME/.csipfs`.  If not present, the [IPFS Engine](xref:Ipfs.Engine.IpfsEngine) will create it with the factory defaults.

To change its name and/or location use the [environment variables](envvars.md) or the [Repository Options](xref:Ipfs.Engine.RepositoryOptions).
