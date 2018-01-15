# Repository

The repository is a persistent store for IPFS data. By default, it is located at `$HOME/.csipfs`.  If not present, the [IPFS Engine](xref:Ipfs.Engine.IpfsEngine) will create it with the factory defaults.

A [SQLite database](https://sqlite.org/), named `ipfs.db`, stores most of the information.  You can use [DB Browser](http://sqlitebrowser.org/) to examine and modify the information.
