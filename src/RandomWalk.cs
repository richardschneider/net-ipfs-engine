﻿using Common.Logging;
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
    ///   This is a background task that runs every <see cref="Period"/>.
    /// </remarks>
    public class RandomWalk : IService
    {
        static ILog log = LogManager.GetLogger(typeof(RandomWalk));
        Random rng = new Random();
        Thread thread;
        CancellationTokenSource cancel;

        /// <summary>
        ///   The Distributed Hash Table to query.
        /// </summary>
        public IDhtApi Dht { get; set; }

        /// <summary>
        ///   How often the query should be run.
        /// </summary>
        /// <value>
        ///   The default is 20 minutes.
        /// </value>
        public TimeSpan Period = TimeSpan.FromMinutes(20);

        /// <summary>
        ///   How long a query should be run.
        /// </summary>
        /// <value>
        ///   The default is 5 minutes.
        /// </value>
        public TimeSpan QueryTime = TimeSpan.FromMinutes(5);

        /// <summary>
        ///   Start a background process that will run a random
        ///   walk every <see cref="Period"/>.
        /// </summary>
        public Task StartAsync()
        {
            if (thread != null)
            {
                throw new Exception("Already started.");
            }

            thread = new Thread(Runner)
            {
                IsBackground = true
            };
            cancel = new CancellationTokenSource();
            thread.Start();
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
            thread = null;

            log.Debug("stopped");

            return Task.CompletedTask;
        }

        /// <summary>
        ///   The background process.
        /// </summary>
        void Runner()
        {
            while (true)
            {
                try
                {
                    RunQuery();
                }
                catch (Exception)
                {
                    // eat all exceptions
                }
                Thread.Sleep(Period);
            }
        }

        void RunQuery()
        {
            log.Debug("Running a query");

            // Get a random peer id.
            byte[] x = new byte[32];
            rng.NextBytes(x);
            var id = MultiHash.ComputeHash(x);

            // Run the query for a while.
            using (var timeout = new CancellationTokenSource(QueryTime))
            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, cancel.Token))
            {
                var _ = Dht.FindPeerAsync(id, cts.Token).ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

    }
}
