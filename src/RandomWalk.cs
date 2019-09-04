using Common.Logging;
using Ipfs.CoreApi;
using PeerTalk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ipfs.Engine
{
    /// <summary>
    ///   Periodically queries the DHT to discover new peers.
    /// </summary>
    /// <remarks>
    ///   A backgroud task is created to query the DHT.  It is designed
    ///   to run often at startup and then less often at time increases.
    /// </remarks>
    public class RandomWalk : IService
    {
        static ILog log = LogManager.GetLogger(typeof(RandomWalk));
        CancellationTokenSource cancel;

        /// <summary>
        ///   The Distributed Hash Table to query.
        /// </summary>
        public IDhtApi Dht { get; set; }

        /// <summary>
        ///   The time to wait until running the query.
        /// </summary>
        public TimeSpan Delay = TimeSpan.FromSeconds(5);

        /// <summary>
        ///   The time to add to the <see cref="Delay"/>.
        /// </summary>
        public TimeSpan DelayIncrement = TimeSpan.FromSeconds(10);

        /// <summary>
        ///   The maximum <see cref="Delay"/>.
        /// </summary>
        public TimeSpan DelayMax = TimeSpan.FromMinutes(9);

        /// <summary>
        ///   Start a background process that will run a random
        ///   walk every <see cref="Delay"/>.
        /// </summary>
        public Task StartAsync()
        {
            if (cancel != null)
            {
                throw new Exception("Already started.");
            }
            cancel = new CancellationTokenSource();
            _ = RunnerAsync(cancel.Token);

            log.Debug("started");
            return Task.CompletedTask;
        }

        /// <summary>
        ///   Stop the background process.
        /// </summary>
        public Task StopAsync()
        {
            cancel?.Cancel();
            cancel?.Dispose();
            cancel = null;

            log.Debug("stopped");
            return Task.CompletedTask;
        }

        /// <summary>
        ///   The background process.
        /// </summary>
        async Task RunnerAsync(CancellationToken cancellation)
        {
            while (!cancellation.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(Delay, cancellation);
                    await RunQueryAsync(cancellation).ConfigureAwait(false);
                    log.Debug("query finished");
                    Delay += DelayIncrement;
                    if (Delay > DelayMax)
                    {
                        Delay = DelayMax;
                    }
                }
                catch (TaskCanceledException)
                {
                    // eat it.
                }
                catch (Exception e)
                {
                    log.Error("run query failed", e);
                    // eat all exceptions
                }
            }
        }

        async Task RunQueryAsync(CancellationToken cancel = default(CancellationToken))
        {
            // Tests may not set a DHT.
            if (Dht == null)
            {
                return;
            }
            log.Debug("Running a query");

            // Get a random peer id.
            var x = new byte[32];
            var rng = new Random();
            rng.NextBytes(x);
            var id = MultiHash.ComputeHash(x);

            await Dht.FindPeerAsync(id, cancel).ConfigureAwait(false);
        }

    }
}
