# Raw Leaves

The [raw leaves option](xref:Ipfs.CoreApi.AddFileOptions.RawLeaves) specifies that 
the file's data blocks are not [encapsulated](format.md) 
with a Merkle DAG but simply contain the file's data.

The [Cid.ContentType](xref:Ipfs.Cid.ContentType) is set to `raw`.

```csharp
var options = new AddFileOptions
{
    RawLeaves = true
};
var node = await ipfs.FileSystem.AddTextAsync("hello world", options);

// zb2rhj7crUKTQYRGCRATFaQ6YFLTde2YzdqbbhAASkL9uRDXn
// base58btc cidv1 raw sha2-256 QmaozNR7DZHQK1ZcU9p7QdrshMvXqWK6gpu5rmrkPdT3L4

```
