# Wrapping

The [wrap option](xref:Ipfs.CoreApi.AddFileOptions.Wrap) specifies that 
the a directory is created for the file.

```csharp
var path = "hello.txt";
File.WriteAllText("hello.txt", "hello world");
var options = new AddFileOptions
{
    Wrap = true
};
var node = await ipfs.FileSystem.AddFileAsync(path, options);

// QmNxvA5bwvPGgMXbmtyhxA1cKFdvQXnsGnZLCGor3AzYxJ

```
## Format

Two blocks are created, a directory object and a file object.  The file object 
is described in [standard format](format.md).  The directory object looks 
like this.

```json
{
  "Links": [
    {"Name": "hello.txt", "Hash": "Qmf412jQZiuVUtdgnB36FXFX7xg5V6KEbSJ4dpQuhkLyfD", "Size":19}
  ],
  "Data": "\u0008\u0001"
}
{
  "Type": 1,
}
```
