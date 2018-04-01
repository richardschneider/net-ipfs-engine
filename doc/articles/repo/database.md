# Database

A [SQLite database](https://sqlite.org/), named `ipfs.db`, stores most of the repository information.  You can use [DB Browser](http://sqlitebrowser.org/) to examine and modify the information.

| Table | Usage | Description |
| ----- | ----- | ----------- |
| Configs | [Config API](xref:Ipfs.Engine.IpfsEngine.Config)| The configuration information |
| EncryptedKeys | [Key API](xref:Ipfs.Engine.IpfsEngine.Key) | An encrypted key |
| Keys | [Key API](xref:Ipfs.Engine.IpfsEngine.Key) | Details on a keys |
| Pins | [Pin API](xref:Ipfs.Engine.IpfsEngine.Pin) | A [Block](block.md) that is pinned to the local repository |
| _EFMigrationsHistory |  | Current version of the database |

## Migration

From time to time the database schema changes.  `_EFMigrationsHistory` records the current version of the database.  At initialisation, the database is upgraded/downgraded based on the version of IPFS Engine.