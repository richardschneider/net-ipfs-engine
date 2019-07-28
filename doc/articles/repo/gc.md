# Garbage Collection

[Garbage collection](xref:Ipfs.CoreApi.IBlockRepositoryApi.RemoveGarbageAsync*) is used to reclaim hard disk space from the
repository. It enumerates the local set of
[blocks](block.md) and remove ones that are not [pinned](pin.md).