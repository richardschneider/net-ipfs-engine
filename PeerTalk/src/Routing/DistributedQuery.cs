using Common.Logging;
using Ipfs;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PeerTalk.Routing
{
    /// <summary>
    ///   A query that is sent to multiple peers.
    /// </summary>
    /// <typeparam name="T">
    ///  The type of answer returned by a peer.
    /// </typeparam>
    public class DistributedQuery<T> where T : class
    {
        static ILog log = LogManager.GetLogger("PeerTalk.Routing.DistributedQuery");
        static int nextQueryId = 1;

        CancellationTokenSource runningQuery;
        List<Peer> visited = new List<Peer>();
        DhtMessage queryMessage;

        /// <summary>
        ///   Raised when an answer is obtained.
        /// </summary>
        public event EventHandler<T> AnswerObtained;

        /// <summary>
        ///   The unique identifier of the query.
        /// </summary>
        public int Id { get; } = nextQueryId++;

        /// <summary>
        ///   The received answers for the query.
        /// </summary>
        public List<T> Answers { get; } = new List<T>();

        /// <summary>
        ///   The number of answers needed.
        /// </summary>
        /// <remarks>
        ///   When the numbers <see cref="Answers"/> recaches this limit
        ///   the <see cref="Run">running query</see> will stop.
        /// </remarks>
        public int AnswersNeeded { get; set; } = 1;

        /// <summary>
        ///   The maximum number of concurrent queries to perform.
        /// </summary>
        public int ConcurrencyLevel { get; set; } = 3;

        /// <summary>
        ///   The distributed hash table.
        /// </summary>
        public Dht1 Dht { get; set; }

        /// <summary>
        ///   The type of query to perform.
        /// </summary>
        public MessageType QueryType { get; set; }

        /// <summary>
        ///   The key to find.
        /// </summary>
        public MultiHash QueryKey { get; set; }

        /// <summary>
        ///   Starts the distributed query.
        /// </summary>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation.
        /// </returns>
        public async Task Run(CancellationToken cancel)
        {
            log.Debug($"Q{Id} run {QueryType} {QueryKey}");

            runningQuery = CancellationTokenSource.CreateLinkedTokenSource(cancel);
            queryMessage = new DhtMessage
            {
                Type = QueryType,
                Key = QueryKey?.ToArray(),
            };

            var tasks = Enumerable
                .Range(1, ConcurrencyLevel)
                .Select(i => { var id = i; return Ask(id); });
            await Task.WhenAll(tasks);
            log.Debug($"Q{Id} found {Answers.Count} answers, visited {visited.Count} peers");

            cancel.ThrowIfCancellationRequested();
        }

        /// <summary>
        ///   Ask the next peer the question.
        /// </summary>
        async Task Ask(int taskId)
        {
            int pass = 0;
            while (!runningQuery.IsCancellationRequested)
            {
                ++pass;

                // Get the nearest peer that has not been visited.
                var peer = Dht.RoutingTable
                    .NearestPeers(QueryKey)
                    .Where(p => !visited.Contains(p))
                    .FirstOrDefault();
                if (peer == null)
                    return;
                visited.Add(peer);

                // Ask the nearest peer.
                log.Debug($"Q{Id}.{taskId}.{pass} ask {peer}");
                try
                {
                    using (var stream = await Dht.Swarm.DialAsync(peer, Dht.ToString(), runningQuery.Token))
                    {
                        // Send the KAD query and get a response.
                        ProtoBuf.Serializer.SerializeWithLengthPrefix(stream, queryMessage, PrefixStyle.Base128);
                        await stream.FlushAsync(runningQuery.Token);
                        var response = await ProtoBufHelper.ReadMessageAsync<DhtMessage>(stream, runningQuery.Token);

                        // Process answer
                        ProcessProviders(response.ProviderPeers);
                        ProcessCloserPeers(response.CloserPeers);
                    }
                }
                catch (Exception e)
                {
                    log.Warn($"Q{Id}.{taskId}.{pass} ask failed '{e.Message}'");
                    // eat it
                }
            }
        }

        void ProcessProviders(DhtPeerMessage[] providers)
        {
            if (providers == null)
                return;

            foreach (var provider in providers)
            {
                if (provider.TryToPeer(out Peer p))
                {
                    p = Dht.Swarm.RegisterPeer(p);
                    if (QueryType == MessageType.GetProviders)
                    {
                        // Only unique answers
                        var answer = p as T;
                        if (!Answers.Contains(answer))
                        {
                            AddAnswer(answer);
                        }
                    }
                }
            }
        }

        void ProcessCloserPeers(DhtPeerMessage[] closerPeers)
        {
            if (closerPeers == null)
                return;
            foreach (var closer in closerPeers)
            {
                if (closer.TryToPeer(out Peer p))
                {
                    p = Dht.Swarm.RegisterPeer(p);
                    if (QueryType == MessageType.FindNode && QueryKey == p.Id)
                    {
                        AddAnswer(p as T);
                    }
                }
            }
        }

        void AddAnswer(T answer)
        {
            if (answer == null)
                return;

            Answers.Add(answer);
            if (Answers.Count >= AnswersNeeded && !runningQuery.IsCancellationRequested)
            {
                runningQuery.Cancel(false);
            }

            AnswerObtained?.Invoke(this, answer);
        }
    }
}
