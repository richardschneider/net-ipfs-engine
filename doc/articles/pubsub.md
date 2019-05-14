# IPFS PubSub system

The publish/subscribe system allows a `message` to be sent to a group of peers that 
are subsctibed to a `topic` via the [PubAub API](xref:Ipfs.CoreApi.IPubSubApi).
The `topic` is just a name that indicates a group of related messages. 
The [message](xref:Ipfs.IPublishedMessage) contains the author, topic(s), 
sequence number and content.



### Publishing

[PublishAsync](xref:Ipfs.CoreApi.IPubSubApi.PublishAsync*) sends a
message to a group of peers that are subscribed to a topic.

The following, sends a "hello world" to all peers subscribed to "nz".

```csharp
await ipfs.PubSub.PublishAsync("nz", "tēnā koutou");
```

### Subscribing

[SubscribeAsync](xref:Ipfs.CoreApi.IPubSubApi.SubscribeAsync*) indicates interest
in messages for the specified topic. The `handler` is invoked when a [unique
message](pubsub/dupmsg.md) is received.

```csharp
var cs = new CancellationTokenSource();
await ipfs.PubSub.SubscribeAsync("nz", msg =>
{
    // do something with msg.DataBytes
}, cs.Token);
```

To unsubscribe, simply cancel the subscribe

```csharp
cs.Cancel();
```

### Implementation

The peer talk [notification service](xref:PeerTalk.PubSub.NotificationService)
with a [floodsub router](xref:PeerTalk.PubSub.FloodRouter) is currently
used.  In the future a [gossip router](https://github.com/richardschneider/peer-talk/issues/25) will be used.

See also
- [PubSub interface for libp2p](https://github.com/libp2p/specs/tree/master/pubsub)
- [In the beginning was floodsub](https://github.com/libp2p/specs/tree/master/pubsub/gossipsub#in-the-beginning-was-floodsub)
- [And then there is gossipsub](https://github.com/libp2p/specs/tree/master/pubsub/gossipsub#the-gossipsub-protocol)
- [Proximity Aware Epidemic PubSub for libp2p](https://github.com/libp2p/specs/blob/master/pubsub/gossipsub/episub.md)
