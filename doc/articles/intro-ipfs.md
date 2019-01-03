# Accessing IPFS

IPFS is a distributed peer to peer system.  There is no central server!  The **IPFS engine** allows your 
program to be a peer on the network.

The [engine](xref:Ipfs.Engine.IpfsEngine) provides a simple way for your program to access the [IPFS Core API](core-api.md).
The engine should be used as a shared object in your program.  It is thread safe (re-entrant) and conserves resources when only one instance is used.

```csharp
public class Program
{
  const string passphrase = "this is not a secure pass phrase";
  static readonly IpfsClient ipfs = new IpfsClient(new IpfsEngine(passphrase.ToCharArray()));

  public async Task Main(string[] args) 
  {
    // Set the repository
	var path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData); 
    ipfs.Options.Repository.Folder = Path.Combine(path, "myapp-ipfs");
	
	// Start the engine.
    await ipfs.StartAsync();

	// Get our peer details.
	var peer = await ipfs.IdAsync();
  }
}
```
