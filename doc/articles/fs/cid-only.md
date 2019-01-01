### Getting a CID

Normally, you get the CID by [adding](xref:Ipfs.CoreApi.IFileSystemApi.AddAsync*) the file to IPFS.  You can avoid adding it 
to IPFS by using the [OnlyHash option](xref:Ipfs.CoreApi.AddFileOptions.OnlyHash).

```csharp
var options = new AddFileOptions { OnlyHash = true };
var fsn = await ipfs.FileSystem.AddTextAsync("hello world", options);
Console.WriteLine((string)fsn.Id)

// Qmf412jQZiuVUtdgnB36FXFX7xg5V6KEbSJ4dpQuhkLyfD
```

