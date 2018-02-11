# Handshake

This documents the handshake message exchange when a peer (local) connects to another peer (remote). 

It was traced with WireShark on Feb 11 2018 using jsipfs v0.27.07 by doing

```
jsipfs daemon
jsipfs jsipfs swarm connect /ip4/127.0.0.1/tcp/4002/ipfs/QmXFX2P5ammdmXQgfqGkfswtEVFsZUJ5KeHRXQYCTdiTAb
```

The daemon's ID
```
> jsipfs id
{
  "id": "QmXFX2P5ammdmXQgfqGkfswtEVFsZUJ5KeHRXQYCTdiTAb",
  "publicKey": "CAASpgIwggEiMA0GCSqGSIb3DQEBAQUAA4IBDwAwggEKAoIBAQCfBYU9c0n28u02N/XCJY8yIsRqRVO5Zw+6kDHCremt2flHT4AaWnwGLAG9YyQJbRTvWN9nW2LK7Pv3uoIlvUSTnZEP0SXB5oZeqtxUdi6tuvcyqTIfsUSanLQucYITq8Qw3IMBzk+KpWNm98g9A/Xy30MkUS8mrBIO9pHmIZa55fvclDkTvLxjnGWA2avaBfJvHgMSTu0D2CQcmJrvwyKMhLCSIbQewZd2V7vc6gtxbRovKlrIwDTmDBXbfjbLljOuzg2yBLyYxXlozO9blpttbnOpU4kTspUVJXglmjsv7YSIJS3UKt3544l/srHbqlwC5CgOgjlwNfYPadO8kmBfAgMBAAE=",
  "addresses": [
    "/ip4/127.0.0.1/tcp/4002/ipfs/QmXFX2P5ammdmXQgfqGkfswtEVFsZUJ5KeHRXQYCTdiTAb",
    "/ip4/127.0.0.1/tcp/4003/ws/ipfs/QmXFX2P5ammdmXQgfqGkfswtEVFsZUJ5KeHRXQYCTdiTAb",
    "/ip4/169.254.140.225/tcp/4002/ipfs/QmXFX2P5ammdmXQgfqGkfswtEVFsZUJ5KeHRXQYCTdiTAb",
    "/ip4/192.168.178.21/tcp/4002/ipfs/QmXFX2P5ammdmXQgfqGkfswtEVFsZUJ5KeHRXQYCTdiTAb"
  ],
  "agentVersion": "js-ipfs/0.27.7",
  "protocolVersion": "9000"
}```


| Local | Remote |
| ===== | ====== |
| a | b |