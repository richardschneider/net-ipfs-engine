# Duplicate Message

The message [sequence number](xref:Ipfs.IPublishedMessage.SequenceNumber) is a monotonically 
increasing number that is unique among 
[messages](xref:Ipfs.IPublishedMessage) originating from a peer.
No two messages from the same peer have the same sequence number.

However, messages from different peers can have the same sequence number, 
so this number alone cannot be used to uniquely identify a message.
A peer id is unique, so the `unique message ID` is the concatenation of the
message [author id](xref:Ipfs.IPublishedMessage.Sender) and [sequence number](xref:Ipfs.IPublishedMessage.SequenceNumber)
fields.

Maintaining a list of all message IDs seen by the peer is not scalable.
A [timed cached](xref:PeerTalk.MessageTracker) is used to detect duplicate messages seen in the last
the 10 minutes.

