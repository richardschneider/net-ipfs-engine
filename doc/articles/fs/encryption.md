# Encryption

The [protection key option](xref:Ipfs.CoreApi.AddFileOptions.ProtectionKey) specifies that 
the file's data blocks are encrypted using the specified [key name](../key.md).

Each data block maps to a [RFC652 - Cryptographic Message Syntax (CMS)](https://tools.ietf.org/html/rfc5652)
of type [Enveloped-data](https://tools.ietf.org/html/rfc5652#section-6) which is DER encoded 
and has the following features:

- The data block is encrypted with a random IV and key using [aes-256-cbc](https://en.wikipedia.org/wiki/Advanced_Encryption_Standard)
- The recipient is a key transport (ktri) with the Subject Key ID equal to the protection key's public ID
- The protection key is used to obtain the `aes key` to decrypt the data block.

The [Cid.ContentType](xref:Ipfs.Cid.ContentType) is set to `cms`.

```csharp
var options = new AddFileOptions
{
    ProtectionKey = "me"
};
var node = await ipfs.FileSystem.AddTextAsync("hello world", options);
```

## Reading

The standard [read file methods](../filesystem.md#reading-a-file) are used to decrypt a file. 
If the protection key is not held by the local peer, then a `KeyNotFoundException` is thrown.


