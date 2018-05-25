# Block

A block is typically a file (or portion of a file) that is content addressable by IPFS, e.g. 
a [content ID](xref:Ipfs.Cid).  It is managed with the [BlockApi](xref:Ipfs.CoreApi.IBlockApi).

Blocks are stored in the local file system and not in the [database](database.md) for performance reasons. 
Each block can be found in the repository's `blocks` folder.  To support case sensitive file 
systems the block's file name is the [z-base-32](xref:Ipfs.Base32z) encoding of the 
block's content ID's multihash value