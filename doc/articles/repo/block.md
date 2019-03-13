# Block

A block is typically a file (or portion of a file) that is content addressable by IPFS, e.g. 
a [content ID](xref:Ipfs.Cid).  It is managed with the [BlockApi](xref:Ipfs.CoreApi.IBlockApi).

A locally cached or pinned block can be found in the repository's `blocks` folder.  To support case insensitive file 
systems the block's file name is the [base-32](xref:Ipfs.Base32) encoding of the 
block's content ID's multihash value.

Most blocks are encoded in the [standard format](../fs/format.md), however other formats, such as 
[chunked](../fs/format.md#chunked-format) and 
[raw](../fs/raw.md) are also availble.