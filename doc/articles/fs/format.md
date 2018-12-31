### Standard Format

Here is the [Merkle DAG](https://github.com/ipfs/go-ipfs/blob/0cb22ccf359e05fb5b55a9bf2f9c515bf7d4dba7/merkledag/pb/merkledag.proto#L31-L39) 
and [UnixFS Data](https://github.com/ipfs/go-ipfs/blob/0cb22ccf359e05fb5b55a9bf2f9c515bf7d4dba7/unixfs/pb/unixfs.proto#L3-L20) 
of a file containing the "hello world" string.

```json
{
  "Links": [],
  "Data": "\u0008\u0002\u0012\u000bhello world\u0018\u000b"
}
```

`Data` is the protobuf encoding of the UnixFS Data.

```json
{
  "Type": 2,
  "Data": "aGVsbG8gd29ybGQ=",
  "FileSize": 11,
  "BlockSizes": null,
  "HashType": null,
  "Fanout": null
}
```
### Chunked Format

When the file's data exceeds the [chunking size](xref:Ipfs.CoreApi.AddFileOptions.ChunkSize), multiple [blocks](xref:Ipfs.CoreApi.IBlockApi) 
are generated.  The returned CID points to a block that has `Merkle.Links`. Each link 
contains a chunk of the file.

The following uses a chunking size of 6.  A primary and two secondary blocks are created for "hello world".

#### Primary Block

```json
{
  "Links": [
    {"Name": "", "Hash": "QmPhmNbdBMtSQczNc4hnsMxRf5L4vfkU8jRTXDSHj8trSV", "Size": 14},
	{"Name": "", "Hash": "QmNyJpQkU1cEkBwMDhDNFstr42q55mqG5GE5Mgwug4xyGk", "Size": 13}
	],
  "Data":"\u0008\u0002\u0018\u000b \u0006 \u0005"
}
{
  "Type": 2, 
  "Data": null,
  "FileSize": 11,
  "BlockSizes": [6,5],
  "HashType":null,
  "Fanout":null
}
```

#### First Link

```json
{
  "Links": [],
  "Data": "\u0008\u0002\u0012\u0006hello \u0018\u0006"
}
```

#### Second Link

```json
{
  "Links": [],
  "Data": "\u0008\u0002\u0012\u0005world\u0018\u0005"
}
```

