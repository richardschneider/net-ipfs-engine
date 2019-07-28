# Pinning

The IPFS engine treats the [blocks](block.md) it stores like a cache, 
meaning that there is no guarantee that the data will 
continue to be stored; see [garbage collection](gc.md). 

The [Pin API](xref:Ipfs.CoreApi.IPinApi) is used to indicate that the data 
is important and mustn’t be thrown away.

## Pinning Services

To ensure that your important data is retained, you may want
to use a pinning service. Such a service normally trades 
money for the service of guaranteeing they’ll keep your data
pinned. 

Some cases where this might be important to you:

- You don’t have a lot of disk space, but you want to ensure some data sticks around.
- Your computer is a laptop, phone, or tablet that will have intermittent connectivity to the network, but you want to be able to access your data on IPFS from anywhere at any time, even when the device you added it from is offline.
- You want a backup that ensures your data is always available from another computer on the network in case you accidentally delete or garbage-collect on your own computer.