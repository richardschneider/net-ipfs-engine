# Message

Messages are exchanged between peers to request some action.  Each message consists of
- A [Varint](xref:Ipfs.Varint) length prefix
- The payload
- A terminating newline (0x0a)

The `length prefix` is the payload size + 1 (for the newline)

## Example

The wire representation of the 'foo' message
```
   0x04 - the length prefix
   0x66 0x6f 0x6f - the payload
   0x0a - the terminating newline
```