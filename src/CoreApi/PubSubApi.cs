using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ipfs.CoreApi;

namespace Ipfs.Engine.CoreApi
{
    class PubSubApi : IPubSubApi
    {
        IpfsEngine ipfs;

        public PubSubApi(IpfsEngine ipfs)
        {
            this.ipfs = ipfs;
        }

        public Task<IEnumerable<Peer>> PeersAsync(string topic = null, CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task PublishAsync(string topic, string message, CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task PublishAsync(string topic, byte[] message, CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task PublishAsync(string topic, Stream message, CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task SubscribeAsync(string topic, Action<IPublishedMessage> handler, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<string>> SubscribedTopicsAsync(CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }
    }
}
