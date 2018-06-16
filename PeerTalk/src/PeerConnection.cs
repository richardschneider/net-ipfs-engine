using Common.Logging;
using Ipfs;
using PeerTalk.Protocols;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PeerTalk
{
    /// <summary>
    ///   A connection between two peers.
    /// </summary>
    /// <remarks>
    ///   A connection is used to exchange messages between peers.
    /// </remarks>
    public class PeerConnection : IDisposable
    {
        static ILog log = LogManager.GetLogger(typeof(PeerConnection));

        StatsStream stream;

        /// <summary>
        ///   The local peer.
        /// </summary>
        public Peer LocalPeer { get; set; }

        /// <summary>
        ///   The remote peer.
        /// </summary>
        public Peer RemotePeer { get; set; }

        /// <summary>
        ///   The local peer's end point.
        /// </summary>
        public MultiAddress LocalAddress { get; set; }

        /// <summary>
        ///   The remote peer's end point.
        /// </summary>
        public MultiAddress RemoteAddress { get; set; }

        /// <summary>
        ///   The duplex stream between the two peers.
        /// </summary>
        public Stream Stream
        {
            get { return stream; }
            set { stream = new StatsStream(value); }
        }

        /// <summary>
        ///   Signals that the security for the connection is established.
        /// </summary>
        /// <remarks>
        ///   This can be awaited.
        /// </remarks>
        public TaskCompletionSource<bool> SecurityEstablished { get; }  = new TaskCompletionSource<bool>();

        /// <summary>
        ///   When the connection was last used.
        /// </summary>
        public DateTime LastUsed => stream.LastUsed;

        /// <summary>
        ///   Number of bytes read over the connection.
        /// </summary>
        public long BytesRead => stream.BytesRead;

        /// <summary>
        ///   Number of bytes written over the connection.
        /// </summary>
        public long BytesWritten => stream.BytesWritten;

        /// <summary>
        ///  Establish the connection with the remote node.
        /// </summary>
        /// <param name="cancel"></param>
        /// <returns></returns>
        /// <remarks>
        ///   This should be called when the local peer wants a connection with
        ///   the remote peer.
        /// </remarks>
        public async Task<Peer> InitiateAsync(CancellationToken cancel = default(CancellationToken))
        {
            await EstablishProtocolAsync("/multistream/", cancel);
            await EstablishProtocolAsync("/plaintext/", cancel);
            //await EstablishProtocolAsync("/multistream/", cancel);
            //await EstablishProtocolAsync("/ipfs/id/", cancel);

            ReadMessages(cancel);
            return null;
        }

        /// <summary>
        ///   TODO:
        /// </summary>
        /// <param name="name"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public async Task EstablishProtocolAsync(string name, CancellationToken cancel)
        {
            var protocols = ProtocolRegistry.Protocols.Keys
                .Where(k => k.StartsWith(name))
                .Select(k => VersionedName.Parse(k))
                .OrderByDescending(vn => vn)
                .Select(vn => vn.ToString());
            foreach (var protocol in protocols)
            {
                await Message.WriteAsync(protocol, Stream, cancel);
                var result = await Message.ReadStringAsync(Stream, cancel);
                if (result == protocol)
                {
                    await ProtocolRegistry.Protocols[protocol]().ProcessResponseAsync(this, cancel);
                    return;
                }
            }
            if (protocols.Count() == 0)
            {
                throw new Exception($"Protocol '{name}' is not registered.");
            }
            throw new Exception($"Remote does not support protocol '{name}'.");
        }

        /// <summary>
        ///   Starts reading messages from the remote peer.
        /// </summary>
        public async void ReadMessages(CancellationToken cancel)
        {
            log.Debug($"start reading messsages from {RemoteAddress}");

            // TODO: Only a subset of protocols are allowed until
            // the remote is authenticated.
            IPeerProtocol protocol = new Multistream1();
            try
            {
                while (!cancel.IsCancellationRequested && stream != null)
                {
                    // TODO: ProcessRequestAsync => ProcessMessageAsync
                    await protocol.ProcessRequestAsync(this, cancel);
                }
            }
            catch (EndOfStreamException)
            {
                // eat it.
            }
            catch (Exception e)
            {
                if (!cancel.IsCancellationRequested && stream != null)
                {
                    log.Error("reading message failed", e);
                }
            }

            log.Debug($"stop reading messsages from {RemoteAddress}");
        }

        /// <summary>
        ///   Accept a connection from the remote peer.
        /// </summary>
        /// <param name="cancel"></param>
        /// <returns></returns>
        /// <remarks>
        ///   This should be called when a remote peer is connecting to the
        ///   local peer.
        /// </remarks>
        public async Task RespondAsync(CancellationToken cancel = default(CancellationToken))
        {
            var ms = new Multistream1();

            // Establish connection security.
            await ms.ProcessRequestAsync(this, cancel);

            // Establish multiplexer
            await ms.ProcessRequestAsync(this, cancel);

            // TODO: Get remote identity and return the Peer
            //await EstablishProtocolAsync("/ipfs/id/", cancel);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        /// <summary>
        ///  TODO
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (stream != null)
                    {
                        try
                        {
                            stream.Dispose();
                            log.Debug($"Closed connection to {RemoteAddress}");
                            // TODO: Does swarm need to know this?
                        }
                        finally
                        {
                            stream = null;
                        }
                    }
                }

                // free unmanaged resources (unmanaged objects) and override a finalizer below.
                // set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~PeerConnection() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

       /// <summary>
       /// 
       /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
