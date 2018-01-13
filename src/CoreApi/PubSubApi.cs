using System;
using System.Collections.Generic;
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

        public Task Publish(string topic, string message, CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task Subscribe(string topic, Action<IPublishedMessage> handler, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<string>> SubscribedTopicsAsync(CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }
    }
}
